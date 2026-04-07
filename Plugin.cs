using EFT;
using EFT.UI;
using Microsoft.VisualBasic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using WTTServerCommonLib.Models;
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
    public override Range SptVersion { get; init; } = new("~4");
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

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 10)]
public class rtt2trader(
    ModHelper modHelper,
    ImageRouter imageRouter,
    ConfigServer configServer,
    ISptLogger<rtt2trader> logger,
    TimeUtil timeUtil,
    CustomItemService customItemService,
    DatabaseService databaseService,
    LocaleService localeService,
    AddCustomTraderHelper addCustomTraderHelper,
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{

    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private static readonly string[] TradersToRemove = ["54cb50c76803fa8b248b4571","6617beeaa9cfa777ca915b7c",
                                                        "54cb57776803fa99248b456e","58330581ace78e27b8b10cee",
                                                        "5935c25fb3acc3127c3d8cd9","5a7c2eca46aef81a7ca2145d",
                                                        "5c0647fdd443bc2504c2d371"
                                                        ];
    public async Task OnLoad()
    {
        // A path to the mods files we use below
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        
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
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_xm);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_ch);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_st);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_vr);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_nd);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_pr);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_at);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(traderBase_ws);

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

        removeTrader(pathToMod); //does our vanilla removal before our quests are loaded.

        //Quest import using WTT COMMON LIB AND Item Import
        var assembly = Assembly.GetExecutingAssembly();

        await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);
        await wttCommon.CustomLootspawnService.CreateCustomLootSpawns(assembly);
        await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly);
        // await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly);
        
        addPlate(); //adds 
        


        // Save the data we loaded above into the trader we've made
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_xm.Id, assort_xm);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_ch.Id, assort_ch);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_st.Id, assort_st);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_vr.Id, assort_vr);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_nd.Id, assort_nd);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_pr.Id, assort_pr);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_at.Id, assort_at);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_ws.Id, assort_ws);

        //This just for fun.
        logger.LogWithColor("************************",LogTextColor.Blue,LogBackgroundColor.Black);
        logger.LogWithColor("*  RTT2 has completed  *",LogTextColor.Blue,LogBackgroundColor.Black);
        logger.LogWithColor("*      all steps!      *",LogTextColor.Blue,LogBackgroundColor.Black);
        logger.LogWithColor("************************",LogTextColor.Blue,LogBackgroundColor.Black);

        // Send back a success to the server to say our trader is good to go
        await Task.CompletedTask;
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

    private void removeTrader(string modPath)
    {
        var quests = databaseService.GetTables().Templates.Quests; // grabs the quest table
        var traders = databaseService.GetTables().Traders; // grabs trader table
        var ragFairTraders = configServer.GetConfig<RagfairConfig>().Traders; // grabs traders ids and their assorts on flea
        var repeatQuestConfigs = configServer.GetConfig<QuestConfig>().RepeatableQuests; // grabs our SPT config that houses repeatable quests
        var locale = databaseService.GetLocales().Global.Values; // grabs locale DB to be transformed and applied later.

        foreach(var traderID in TradersToRemove)
        {
            if (traders.ContainsKey(traderID))
            {
                traders.Remove(traderID); // remove traders from database
                ragFairTraders.Remove(traderID); //removes trader barters from FLEA

                
                foreach(var quest in quests)
                { //This removes all quests minus repeatables to ensure nothing tries to call a removed trader.
                    var id = quest.Value.Id;
                    quests.Remove(id);
                }

                foreach (var repeatQ in repeatQuestConfigs) 
                {
                    foreach (var item in repeatQ.TraderWhitelist.ToList()) // run through traders in whitelist and create copy
                    {                                                       // with ToList to make sure we dont lose our pointer upon deletion.
                        if (item.TraderId == traderID) //if the trader matches what we are removing
                        {                                           
                            repeatQ.TraderWhitelist.Remove(item); //remove trader from whitelist
                        }                                          //this also allows repeatables to be on only Fence and My Laptop
                    }
                }

                //logger.Info(quests.ToString());
            }

            if (traders.ContainsKey("5ac3b934156ae10c4430e83c")) // check for ragman so we can modify his information since removing him isnt worth it.
            {
                databaseService.GetTables().Locales.Global["en"].AddTransformer(locale =>
                { //due to how Locales loaded we use a transformer to catch our locale and make the required changes
                    if (locale == null) return locale; //if our locale doesnt load do nothing

                    locale["5ac3b934156ae10c4430e83c FullName"] = "Lenobo IdeaPod";
                    locale["5ac3b934156ae10c4430e83c Description"] = "It's amazing what you can do with a Lenobo IdeaPod™";
                    locale["5ac3b934156ae10c4430e83c Nickname"] = "My Laptop";
                    locale["5ac3b934156ae10c4430e83c Location"] = "The Hideout";
                    locale["5ac3b934156ae10c4430e83c Firstname"] = "Paper Weight";

                    return locale; //make our changes to ragman info and send it out.
                });

                traders["5ac3b934156ae10c4430e83c"].Base.Nickname = "My Laptop";//change his base nickname for terminal debugging


                traders["5ac3b934156ae10c4430e83c"].Base.Avatar = "/files/trader/avatar/laptop.jpg"; //set our new trader image in trader table
                imageRouter.AddRoute("/files/trader/avatar/laptop", "./user/mods/rtt2/data/laptop.jpg"); 
                //this is very similiar to how the modded trader images are routed but we skip the steps of merging the 
                //mod path and the image names by simply sending our chosen key and value.

                var assort = modHelper.GetJsonDataFromFile<TraderAssort>(modPath, "data/laptop-assort.json");
                traders["5ac3b934156ae10c4430e83c"].Assort = assort; //overwrites ragmans assort to our modded assort.
                
                ;
            }
            
        }
        
        
    }

}
