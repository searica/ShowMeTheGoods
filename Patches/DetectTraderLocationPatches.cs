using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine.SceneManagement;
using SoftReferenceableAssets;
using ShowMeTheGoods.Extensions;
using Logging;
using System;
using System.IO;


namespace ShowMeTheGoods.Patches;

[HarmonyPatch]
internal static class DetectTraderLocationPatches
{
    private static readonly HashSet<string> TraderLocationNames = ["Vendor_BlackForest", "Hildir_camp", "BogWitch_Camp"];
    private static readonly Dictionary<string, AssetID> TraderPrefabNameToAssetID = [];
    private static readonly HashSet<AssetID> TraderAssetIDS = [];
    /// <summary>
    ///     Simple wrapper function for checking if zonelocation is a trader location
    ///     without releasing the loaded assets. For use in transpiler patch.
    /// </summary>
    /// <param name="zoneLocation"></param>
    private static void IsTraderLocation(ZoneSystem.ZoneLocation zoneLocation)
    {
        zoneLocation.IsTraderLocation(release: false);
    }

    /// <summary>
    ///     Also check after start is down just to see.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static void FindTraderLocationNames(ZoneSystem __instance)
    {
        // If loading into game world and prefabs have not been added
        if (SceneManager.GetActiveScene().name != "main" || !__instance)
        {
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab || !prefab.IsRootPrefab() || TraderPrefabNameToAssetID.ContainsKey(prefab.name)) 
            {  
                continue; 
            }

            if (prefab.GetComponent<Trader>())
            {
                TraderPrefabNameToAssetID.Add(prefab.name, default); 
            }
        }

        foreach (KeyValuePair<string, AssetID> item in Runtime.GetAllAssetPathsInBundleMappedToAssetID())
        {
            if (!item.Key.EndsWith(".prefab", StringComparison.Ordinal))
            {
                continue;
            }
            string prefabName = Path.GetFileNameWithoutExtension(item.Key);
            if (!TraderPrefabNameToAssetID.ContainsKey(prefabName))
            {
                continue;
            }
            TraderPrefabNameToAssetID[prefabName] = item.Value;
            Log.LogInfo($"Path: {item.Key}, AssetID: {item.Value}");
        }
        
        foreach (ZoneSystem.ZoneLocation zoneLocation in __instance.m_locations)
        {
            if (!zoneLocation.m_enable)
            {
                continue;
            }
            HashSet<AssetID> dependencies = AssetBundleLoader.Instance.GetDependenciesFromAssetID(zoneLocation.m_prefab.m_assetID);
            foreach (KeyValuePair<string, AssetID> item in TraderPrefabNameToAssetID)
            {
                if (dependencies.Contains(item.Value))
                {
                    Log.LogInfo($"Location: {zoneLocation.m_prefabName} has Trader: {item.Key}");
                }
            }
        }
        stopwatch.Stop();
        Log.LogInfo($"Time to search for trader locations: {stopwatch.ElapsedMilliseconds} ms");
    }
}

