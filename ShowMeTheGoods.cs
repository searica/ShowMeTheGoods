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
    public const string PluginVersion = "0.3.1";

    internal static ShowMeTheGoods Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;

    // Global settings
    internal const string MainConfigSection = PluginName;
    internal ConfigEntry<int> MapCost;
    private bool AddedCustomItems = false;
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
        Log.Verbosity = Config.BindConfigInOrder(
            MainConfigSection,
            "Verbosity",
            Log.InfoLevel.Low,
            "Low will log basic information about the mod. Medium will log information that " +
            "is useful for troubleshooting. High will log a lot of information, do not set " +
            "it to this without good reason as it will slow Down your game.",
            synced: false,
            sectionOrder: false
        );

        MapCost = Config.BindConfigInOrder(
            MainConfigSection,
            "Map Cost",
            2000,
            "Cost of the map in coins.",
            synced: true,
            acceptableValues: new AcceptableValueRange<int>(1, 10000),
            sectionOrder: false
        );

        MapCost.SettingChanged += (obj, e) =>
        {
            if (AddedCustomItems)
            {
                TradeRouteMapManager.UpdateTradeItemCost();
            }
            
        };
    }

    private void AddCustomItems()
    {
        Log.LogInfo("Adding TradeRouteMap", Log.InfoLevel.Medium);
        ItemManager.Instance.AddItem(TradeRouteMapManager.GetTradeRouteMapCustomItem());
        // Only want this to run once, Jotunn has the item cached for the game session
        PrefabManager.OnVanillaPrefabsAvailable -= AddCustomItems;
        AddedCustomItems = true;
    }

    public void OnDestroy()
    {
        Config.Save();
    }

}
