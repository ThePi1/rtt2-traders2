using EFT;
using EFT.Hideout;
using EFT.UI;
using EFT.UI.Chat;
using Microsoft.VisualBasic;
using SPTarkov.Common.Extensions;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using SPTarkov.Server.Core.Utils.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using WTTServerCommonLib.Models;
using static RenderQueue;
using Path = System.IO.Path;
using Range = SemanticVersioning.Range;

namespace rtt2traders;
//[BepInDependency("com.wtt.commonlib")]
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.rtt2traderteam.rtt2traders";
    public override string Name { get; init; } = "RTT2 Traders";
    public override string Author { get; init; } = "RTT 2 Trader Team";
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override Range SptVersion { get; init; } = new("~4.0.0");
    public override string License { get; init; } = "MIT";
    public override bool? IsBundleMod { get; init; } = true;
    public override Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new Range("~2.0.0") }
    };
    public override string? Url { get; init; }
    public override List<string>? Contributors { get; init; }
    public override List<string>? Incompatibilities { get; init; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 4)]
public class rtt2trader(
    ModHelper modHelper,
    ImageRouter imageRouter,
    ConfigServer configServer,
    ISptLogger<rtt2trader> logger,
    TimeUtil timeUtil,
    DatabaseService databaseService,
    AddCustomTraderHelper addCustomTraderHelper,
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{

    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private static readonly string[] TradersToRemove =  [
                                                        "54cb50c76803fa8b248b4571","6617beeaa9cfa777ca915b7c",
                                                        "54cb57776803fa99248b456e","58330581ace78e27b8b10cee",
                                                        "5935c25fb3acc3127c3d8cd9","5a7c2eca46aef81a7ca2145d",
                                                        "5c0647fdd443bc2504c2d371"
                                                        ];
    private static readonly string[] TradersToSkip =    [
                                                        "656f0f98d80a697f855d34b1","579dc571d53a0658a154fbec",
                                                        "638f541a29ffd1183d187f57","69744632183b55cf9702c984",
                                                        "69744632183b55cf9702c987","69744632183b55cf9702c988",
                                                        "69744632183b55cf9702c981","69744632183b55cf9702c983",
                                                        "69744632183b55cf9702c985","69744632183b55cf9702c982",
                                                        "69744632183b55cf9702c986","6864e812f9fe664cb8b8e152",
                                                        "5ac3b934156ae10c4430e83c"
                                                        ];
    private HashSet<string> VanillaItems;
    public async Task OnLoad()
    {
        // A path to the mods files we use below
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        var VanillaItemsList = modHelper.GetJsonDataFromFile<List<string>>(pathToMod, "data/itemTPL.json");
        VanillaItems = new HashSet<string>(VanillaItemsList); //use a hashset instead of list to save on runtime when we compare items.

        // A relative path to the trader icon to show
        var traderImagePath_xm = Path.Combine(pathToMod, "data/xiaoming.jpg");
        var traderImagePath_ch = Path.Combine(pathToMod, "data/chimera.jpg");
        var traderImagePath_st = Path.Combine(pathToMod, "data/storyteller.jpg");
        var traderImagePath_vr = Path.Combine(pathToMod, "data/voron.jpg");
        var traderImagePath_nd = Path.Combine(pathToMod, "data/needle.jpg");
        var traderImagePath_pr = Path.Combine(pathToMod, "data/price.jpg");
        var traderImagePath_at = Path.Combine(pathToMod, "data/atlas.jpg");
        var traderImagePath_ws = Path.Combine(pathToMod, "data/weiss.jpg");



        // The base json containing trader settings we will add to the server
        var traderBase_xm = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/xm-base.json");
        var traderBase_ch = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/ch-base.json");
        var traderBase_st = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/st-base.json");
        var traderBase_vr = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/vr-base.json");
        var traderBase_nd = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/nd-base.json");
        var traderBase_pr = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/pr-base.json");
        var traderBase_at = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/at-base.json");
        var traderBase_ws = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/ws-base.json");

        var dialogue_st = modHelper.GetJsonDataFromFile<Dictionary<string, List<string>?>>(pathToMod, "data/st-dialogue.json");




        // Create a helper class and use it to register our traders image/icon + set its stock refresh time
        imageRouter.AddRoute(traderBase_xm.Avatar.Replace(".jpg", ""), traderImagePath_xm);
        imageRouter.AddRoute(traderBase_ch.Avatar.Replace(".jpg", ""), traderImagePath_ch);
        imageRouter.AddRoute(traderBase_st.Avatar.Replace(".jpg", ""), traderImagePath_st);
        imageRouter.AddRoute(traderBase_vr.Avatar.Replace(".jpg", ""), traderImagePath_vr);
        imageRouter.AddRoute(traderBase_nd.Avatar.Replace(".jpg", ""), traderImagePath_nd);
        imageRouter.AddRoute(traderBase_pr.Avatar.Replace(".jpg", ""), traderImagePath_pr);
        imageRouter.AddRoute(traderBase_at.Avatar.Replace(".jpg", ""), traderImagePath_at);
        imageRouter.AddRoute(traderBase_ws.Avatar.Replace(".jpg", ""), traderImagePath_ws);

        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_xm, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_ch, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_st, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_vr, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_nd, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_pr, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_at, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, traderBase_ws, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));


        // Add our trader to the config file, this lets it be seen by the flea market

        //_ragfairConfig.Traders.TryAdd(traderBase.Id, true);
        // Add our trader (with no items yet) to the server database
        // An 'assort' is the term used to describe the offers a trader sells, it has 3 parts to an assort
        // 1: The item
        // 2: The barter scheme, cost of the item (money or barter)
        // 3: The Loyalty level, what rep level is required to buy the item from trader
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_xm, []);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_ch, []);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_st, dialogue_st);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_vr, []);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_nd, []);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_pr, []);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_at, []);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_ws, []);

        // Add localisation text for our trader to the database so it shows to people playing in different languages
        addCustomTraderHelper.AddTraderToLocales(traderBase_xm, "Xiaoming", "Chinese bureaucrat representing the CCP in Tarkov. He's more clever than you think, but less clever than he thinks.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_ch, "Chimera", "An old friend.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_st, "Storyteller", "The voice in your head.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_vr, "Voron", "All will be revealed...");
        addCustomTraderHelper.AddTraderToLocales(traderBase_nd, "Needle", "Wheel and deal.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_pr, "Price", "He sells cigarettes and cigarette accessories.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_at, "Atlas", "Your friendly, neighborhood, out-of-touch gun enthusiast.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_ws, "Weiss", "Unhinged ex-Terragroup researcher.");

        // Get the assort data from JSON
        var assort_xm = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/xm-assort.json");
        var assort_ch = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/ch-assort.json");
        var assort_st = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/st-assort.json");
        var assort_vr = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/vr-assort.json");
        var assort_nd = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/nd-assort.json");
        var assort_pr = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/pr-assort.json");
        var assort_at = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/at-assort.json");
        var assort_ws = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/ws-assort.json");

        removeQuests();
        hideoutChanges();
        giftChanges();
        achievementChanges();

        //Quest import using WTT COMMON LIB AND Item Import
        var assembly = Assembly.GetExecutingAssembly();

        await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomLootspawnService.CreateCustomLootSpawns(assembly);
        await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly);
        // await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
        
        addPlate(); //adds custom plate to vests
    
        // Save the data we loaded above into the trader we've made
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_xm.Id, assort_xm);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_ch.Id, assort_ch);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_st.Id, assort_st);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_vr.Id, assort_vr);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_nd.Id, assort_nd);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_pr.Id, assort_pr);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_at.Id, assort_at);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_ws.Id, assort_ws);

        editRagman(pathToMod); //does the changes to ragman and preps his new assort for new items
        editTraders(); // begins our trader modifications and deletions

        //This just for fun.
        logger.LogWithColor("RTT2 has completed all steps!",LogTextColor.Blue,LogBackgroundColor.Black);

        // Send back a success to the server to say our trader is good to go
        
        await Task.CompletedTask;
    }

    private void debugListQuests()
    {
        Console.WriteLine("Current quests:");
        var newquests = databaseService.GetTables().Templates.Quests;
        foreach (var quest in newquests.ToList())
        {
            Console.WriteLine(quest.Value.QuestName);
        }
    }
    private void removeQuests()
    {
        var quests = databaseService.GetTables().Templates.Quests; // grabs the quest table
        int removeCounter = 0;
        foreach(var quest in quests.ToList())
        { //This removes all quests minus repeatables to ensure nothing tries to call a removed trader.
            var id = quest.Value.Id;
            //Console.WriteLine(quest.Value.QuestName);
            quests.Remove(id);          
            removeCounter++;
        }
        //Console.WriteLine("RTT2: removed " + removeCounter + " quests.");
    }
    private void addPlate() // Referenced FiveFs Unrestricted Armor Plate mod for understanding of editing already existing database entries.
    {
        var itemTable = databaseService.GetTables().Templates.Items;
        var plateToAdd = "69d44b9ec379dcfd2bf8de40";

        foreach (var item in itemTable)
        {
            var itemId = item.Value.Id;
            var parentId = item.Value.Parent;
            var properties = item.Value.Properties;
            var name = item.Value.Name;

            if (parentId == "5448e54d4bdc2dcc718b4568" || parentId == "5448e5284bdc2dcb718b4567") //Armor || Chest Rigs
            {
                foreach (var slot in properties.Slots)
                {
                    foreach(var filter in slot.Properties.Filters)
                    {
                        if (slot.Name.Equals("front_plate",StringComparison.OrdinalIgnoreCase) || slot.Name.Equals("back_plate",StringComparison.OrdinalIgnoreCase) )
                        {
                            if (!filter.Filter.Contains(plateToAdd))
                            {
                                filter.Filter.Add(plateToAdd);  //this whole nested set of loops is to add our custom LIMP plate to armor/vests
                            }
                        }
                    }
                }
            }
        }
    }

    private void editTraders()
    {
        var traders = databaseService.GetTables().Traders;

        foreach (var trader in traders)
        {
            //logger.Info(trader.Value.Base.Id);
            if(TradersToSkip.Contains(trader.Value.Base.Id.ToString())) //these need to be skipped
            {
                continue; //logger.Info("Found Trader to skip: " + trader.Value.Base.Nickname);
            }
            if(!TradersToRemove.Contains(trader.Value.Base.Id.ToString()))
            {
                //logger.Info("Found modded Trader: " + trader.Value.Base.Nickname);
                moddedTraders(trader.Value.Base.Id);
                //this catches our modded traders so that if a user wants their modded items they are moved to a trader that wont
                //potentiall cause problems.
            }
            else
            {
                //logger.Info("Found trader to Remove: " + trader.Value.Base.Nickname);
                removeTrader(trader.Value.Base.Id); 
                //this else catches the traders we directly want to delete and sends it off to be removed
            }
        }
    }

//still need to add appearance to laptop.
    private void moddedTraders(string trader) // modded trader support to add modded items to Laptop
    {                       //may have to rework again :'( as this would cull any QOL trader mods++
        
        var laptopApparel = databaseService.GetTrader("5ac3b934156ae10c4430e83c").Suits;
        var moddedApparel = databaseService.GetTrader(trader).Suits;

        if (moddedApparel != null)
        {
            foreach(var suit in moddedApparel)
            {   //sets this modded trader's clothes to be locked behind first quest for now / removes LL req.
                suit.Requirements.LoyaltyLevel = 0;
                suit.Tid = "5ac3b934156ae10c4430e83c"; //sets the suits Trader id to laptop and below this for the requirements. fixes red/blue gui bug.
                suit.Requirements.RequiredTid = "5ac3b934156ae10c4430e83c";
                if (suit.Requirements.QuestRequirements.Count() != 0){ //checks if suit has a quest requirement and clears all and adds first quest
                    suit.Requirements.QuestRequirements.Clear(); 
                    suit.Requirements.QuestRequirements.Add("69cc8e901fda896ea59b50f0");
                }
                laptopApparel.Add(suit);
            }

        }

        moddedItemGrabber(trader);

        removeTrader(trader); //sends modded trader off for removal
    }

    private void removeTrader(string trader)
    {

        moddedItemGrabber(trader);

        var traders = databaseService.GetTables().Traders; // grabs trader table
        var ragFairTraders = _ragfairConfig.Traders; // grabs traders ids and their assorts on flea
        var repeatQuestConfigs = configServer.GetConfig<QuestConfig>().RepeatableQuests; // grabs our SPT config that houses repeatable quests

        traders.Remove(trader); // remove the passed trader from database
        ragFairTraders.Remove(trader); //removes the passed trader barters from FLEA


        foreach (var repeatQ in repeatQuestConfigs) 
        {
            foreach (var item in repeatQ.TraderWhitelist.ToList()) // run through traders in whitelist and create copy
            {                                                       // with ToList to make sure we dont lose our pointer upon deletion.
                if (item.TraderId == trader) //if the trader matches what we are removing
                {                                           
                    repeatQ.TraderWhitelist.Remove(item); //remove trader from whitelist
                }                                          //this also allows repeatables to be on only Fence and My Laptop
            }
        }
        //logger.Info(quests.ToString());
    }

    private void editRagman(string modPath)
    {
        var traders = databaseService.GetTrader("5ac3b934156ae10c4430e83c");
        databaseService.GetTables().Locales.Global["en"].AddTransformer(locale =>
            { //due to how Locales load we use a transformer to catch our locale and make the required changes
                if (locale == null) return locale; //if our locale doesnt load do nothing

                locale["5ac3b934156ae10c4430e83c FullName"] = "Lenobo IdeaPod";
                locale["5ac3b934156ae10c4430e83c Description"] = "It's amazing what you can do with a Lenobo IdeaPod™";
                locale["5ac3b934156ae10c4430e83c Nickname"] = "My Laptop";
                locale["5ac3b934156ae10c4430e83c Location"] = "The Hideout";
                locale["5ac3b934156ae10c4430e83c Firstname"] = "Paper Weight";

                return locale; //make our changes to ragman info and send it out.
            });

        traders.Base.Nickname = "My Laptop";//change his base nickname for terminal debugging
        
        var loyaltyLevel = traders.Base.LoyaltyLevels;
        foreach (var loyalty in loyaltyLevel) //removes rep requirements from laptop since no quests
        {
            loyalty.MinStanding = 0;
        }
        

        traders.Base.Avatar = "/files/trader/avatar/laptop.jpg"; //set our new trader image in trader table
        imageRouter.AddRoute("/files/trader/avatar/laptop", "./user/mods/rtt2/data/laptop.jpg"); 
        //this is very similiar to how the modded trader images are routed but we skip the steps of merging the 
        //mod path and the image names by simply sending our chosen key and value.

        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(modPath, "data/laptop-assort.json");
        traders.Assort = assort; //overwrites ragmans assort to our modded assort.

        var laptopApparel = traders.Suits;
        foreach(var suit in laptopApparel.ToList()) //sets all vanilla clothes to be locked behind first quest for now.
        {
            if (suit.Requirements.QuestRequirements.Count() != 0){
                suit.Requirements.QuestRequirements.Clear(); //removes achievement requirements/ may remove at later date seems achievements may still work
                suit.Requirements.QuestRequirements.Add("69cc8e901fda896ea59b50f0");
            }
            suit.Requirements.AchievementRequirements.Clear();
            suit.Requirements.LoyaltyLevel = 1;
        }
    }

    private void hideoutChanges() //sets all LL req. for hideout upgrades to Chimera
    {
        var hideout = databaseService.GetTables().Hideout.Areas;

        foreach (var station in hideout)
        {
            foreach (var stage in station.Stages)
            {
                foreach (var requirement in stage.Value.Requirements)
                {
                    requirement.TraderId = "69744632183b55cf9702c982";
                }
            }
        }
    }

    private void giftChanges()
    {
        var gifts = configServer.GetConfig<GiftsConfig>().Gifts;

        foreach (var gift in gifts)
        {
            gift.Value.Trader = "69744632183b55cf9702c981";
        }   
    }

    private void achievementChanges()
    {
        var achievements = databaseService.GetTemplates().Achievements;
        foreach (var achievement in achievements)
        {
            foreach (var reward in achievement.Rewards)
            {
                if(reward.TraderId != null)
                {
                    reward.TraderId = "69744632183b55cf9702c981";
                }
            }
        }
    }

    private void AddItemAndChildren(List<Item> moddedItem, List<Item> LaptopAssort, string parentId)
    {
        foreach (var item in moddedItem.Where(item => item.ParentId == parentId)) // foreach item where new items parent id = old items parentId if its not next iteration
        {
            if (!LaptopAssort.Any(oldItem => oldItem.Id == item.Id)) // checks if our old item ID is not already being used by our new item in the assort
            {
                LaptopAssort.Add(item);
                AddItemAndChildren(moddedItem, LaptopAssort, item.Id); // recursivly call with our new items id as the next parent
            }
        }
    }

    private void moddedItemGrabber(string trader)
    {
        var assort = databaseService.GetTrader("5ac3b934156ae10c4430e83c").Assort;
        var moddedAssort = databaseService.GetTrader(trader).Assort;
        if (moddedAssort == null)
        { //just checks if a trader assort has not been loaded
            logger.Warning(trader + "assort null, skipping.");
            return;
        }

        foreach(var item in moddedAssort.Items)
        {
            
            if (!VanillaItems.Contains(item.Template)) //if our item is not vanilla continue
            {
                if (!assort.Items.Any(existingitem => existingitem.Id == item.Id)) // we check if the item has already been added if not continue
                {
                    assort.Items.Add(item); // adds item
                    AddItemAndChildren(moddedAssort.Items, assort.Items, item.Id); //sends off to see if it has children

                    foreach(var barter in moddedAssort.BarterScheme)
                    {
                        if(item.Id == barter.Key)
                        {
                            assort.BarterScheme[barter.Key] = barter.Value; //adds barter scheme
                            break;
                        }   
                    }
                    foreach(var loyalty in moddedAssort.LoyalLevelItems)
                    {
                        if(item.Id == loyalty.Key)
                        {
                            assort.LoyalLevelItems[loyalty.Key] = loyalty.Value; //adds loyalty 
                            break;
                        }
                    }
                    continue; //potential else to add entire modded trader assorts.
                }
            }
        } 
    }
}
