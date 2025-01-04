using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Jotunn.Extensions;
using Configs;
using Logging;
using ShowMeTheGoods.Core;

namespace ShowMeTheGoods;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.ServerMustHaveMod, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal sealed class ShowMeTheGoods : BaseUnityPlugin
{
    public const string PluginName = "ShowMeTheGoods";
    internal const string Author = "Searica";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.2.2";

    internal static ShowMeTheGoods Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;

    // Global settings
    internal const string GlobalSection = "Global";
    internal ConfigEntry<int> MapCost;
    
    public void Awake()
    {
        Instance = this;
        ConfigFile = Config;
        Log.Init(Logger);

        Config.DisableSaveOnConfigSet();
        SetUpConfigEntries();
        Config.Save();
        Config.SaveOnConfigSet = true;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;

        ConfigFileWatcher = new(Config);  // check for changes to the config file
        
        // add custom TradeRouteMap
        PrefabManager.OnVanillaPrefabsAvailable += AddCustomItems;
    }

    internal void SetUpConfigEntries()
    {
        MapCost = Config.BindConfigInOrder(
            GlobalSection,
            "Map Cost",
            2000,
            "Cost of the map in coins.",
            synced: true,
            acceptableValues: new AcceptableValueRange<int>(1, 10000)
        );
        MapCost.SettingChanged += (obj, e) =>
        {
            TradeRouteMapManager.UpdateTradeItemCost();
        };
    }

    private void AddCustomItems()
    {
        Log.LogInfo("Adding TradeRouteMap", Log.InfoLevel.Medium);
        ItemManager.Instance.AddItem(TradeRouteMapManager.GetTradeRouteMapCustomItem());
        // Only want this to run once, Jotunn has the item cached for the game session
        PrefabManager.OnVanillaPrefabsAvailable -= AddCustomItems;
    }

    public void OnDestroy()
    {
        Config.Save();
    }

}
