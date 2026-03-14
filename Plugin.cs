using Microsoft.VisualBasic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using SPTarkov.Server.Core.Utils;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine.Assertions;
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
    public override Range SptVersion { get; init; } = new("4.0.11");
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

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 2)]
public class rtt2trader(
    ModHelper modHelper,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    CustomItemService customItemService,
    AddCustomTraderHelper addCustomTraderHelper,
    WTTServerCommonLib.WTTServerCommonLib wttCommon
) : IOnLoad
{

    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
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
        addCustomTraderHelper.AddTraderToLocales(traderBase_ch, "Chimera", "An old friend. He was dead, previously... but now he's not...?");
        addCustomTraderHelper.AddTraderToLocales(traderBase_st, "Storyteller", "The voice in your head.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_vr, "Voron", "All will be revealed according to Its plan, according to It.");
        addCustomTraderHelper.AddTraderToLocales(traderBase_nd, "Needle", "He's got the stuff, if you've got the cash...");
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

        //Quest import using WTT COMMON LIB AND Item Import
        var assembly = Assembly.GetExecutingAssembly();

        await wttCommon.CustomQuestService.CreateCustomQuests(assembly);
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly);



        // Save the data we loaded above into the trader we've made
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_xm.Id, assort_xm);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_ch.Id, assort_ch);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_st.Id, assort_st);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_vr.Id, assort_vr);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_nd.Id, assort_nd);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_pr.Id, assort_pr);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_at.Id, assort_at);
        addCustomTraderHelper.OverwriteTraderAssort(traderBase_ws.Id, assort_ws);

        // Send back a success to the server to say our trader is good to go
        await Task.CompletedTask;
    }
}
