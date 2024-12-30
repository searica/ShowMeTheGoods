using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Jotunn.Extensions;
using Configs;
using Logging;
using Jotunn.Configs;
using UnityEngine;
using Jotunn.Entities;
using ShowMeTheGoods.Core;

// To begin using: rename the solution and project, then find and replace all instances of "ShowMeTheGoods"
// Next: rename the main plugin as desired.

// If using Jotunn then the following files should be removed from Configs:
// - ConfigManagerWatcher
// - ConfigurationManagerAttributes
// - SaveInvokeEvents
// - ConfigFileExtensions should be editted to only include `DisableSaveOnConfigSet`

// If not using Jotunn
// - Remove the decorators on the MainPlugin
// - Swap from using SynchronizationManager.OnConfigurationWindowClosed to using ConfigManagerWatcher.OnConfigurationWindowClosed
// - Remove calls to SynchronizationManager.OnConfigurationSynchronized
// - Adjust using statements as needed
// - Remove nuget Jotunn package via manage nuget packages
// - Uncomment the line: <Import Project="$(JotunnProps)" Condition="Exists('$(JotunnProps)')" /> in the csproj file

namespace ShowMeTheGoods;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal sealed class ShowMeTheGoods : BaseUnityPlugin
{
    public const string PluginName = "ShowMeTheGoods";
    internal const string Author = "Searica";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.1.0";

    internal static ShowMeTheGoods Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;


    // Global settings
    internal const string GlobalSection = "Global";
    internal ConfigEntry<int> MapCost;
    internal CustomItem traderMap;

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

        // Re-initialization after reloading config and don't save since file was just reloaded
        ConfigFileWatcher = new(Config);
        ConfigFileWatcher.OnConfigFileReloaded += () =>
        {
            // do stuff
        };

        SynchronizationManager.OnConfigurationSynchronized += (obj, e) =>
        {
            // do stuff
        };

        SynchronizationManager.OnConfigurationWindowClosed += () =>
        {
            // do stuff
        };

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

    }

    private void AddCustomItems()
    {
        Log.LogInfo("Adding custom item");
        // Create and add a custom item based on SwordBlackmetal
        ItemConfig traderMapConfig = new()
        {
            Name = "Trade Route Map",
            Description = "Map that helps to find possible trader locations.",
            CraftingStation = CraftingStations.None,
            Enabled = false, // make not craftable
            StackSize = 1,
            Weight = 1f
        };

        // Start setting up a customized prefab to modify the appearance of the item
        GameObject mapPrefab = PrefabManager.Instance.CreateClonedPrefab("ShowMeTheGoods_TradeRouteMap", "DeerHide");
        mapPrefab.AddComponent<TradeRouteMap>();

        // I need to customize a prefab here and use that in the constructor for traderMap
        // Change the texture or material of some kind of cloth?
        traderMap = new(mapPrefab, true, traderMapConfig);

        ItemManager.Instance.AddItem(traderMap);

        // You want that to run only once, Jotunn has the item cached for the game session
        PrefabManager.OnVanillaPrefabsAvailable -= AddCustomItems;
    }

    // will need to patch Humanoid.UseItem to add trader locating functionality

    public void OnDestroy()
    {
        Config.Save();
    }

}
