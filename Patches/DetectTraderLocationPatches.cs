using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine.SceneManagement;
using SoftReferenceableAssets;
using System;
using System.IO;
using UnityEngine;
using Jotunn.Managers;
using Logging;
using ShowMeTheGoods.Extensions;


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
                var assetID = AssetManager.Instance.GetAssetID<GameObject>(prefab.name);
                Log.LogInfo($"Found Trader: {prefab.name}, AssetID: {assetID}");
                TraderPrefabNameToAssetID.Add(prefab.name, assetID);
            }
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

