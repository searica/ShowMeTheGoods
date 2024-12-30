using System;
using System.Collections.Generic;
using System.Diagnostics;
using SoftReferenceableAssets;
using UnityEngine;
using UnityEngine.SceneManagement;
using HarmonyLib;
using Jotunn.Managers;
using Logging;
using ShowMeTheGoods.Extensions;


namespace ShowMeTheGoods.Core;

[HarmonyPatch]
internal static class TraderLocatonManager
{
    private static bool HasInit = false;
    private static readonly Dictionary<string, bool> IsTraderLocationMap = [];
    private static readonly Dictionary<string, AssetID> TraderPrefabNameToAssetID = [];


    /// <summary>
    ///     Scan locations to see which depend on the trader prefab assets
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static void InitTraderLocations(ZoneSystem __instance)
    {
        // If loading into game world and prefabs have not been added
        if (SceneManager.GetActiveScene().name != "main" || !__instance || !ZNetScene.instance)
        {
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab || !prefab.IsRootPrefab() || TraderPrefabNameToAssetID.ContainsKey(prefab.name))
            {
                continue;
            }

            if (prefab.GetComponent<Trader>())
            {
                AssetID assetID = AssetManager.Instance.GetAssetID<GameObject>(prefab.name);
                Log.LogInfo($"Found Trader: {prefab.name}, AssetID: {assetID}");
                TraderPrefabNameToAssetID.Add(prefab.name, assetID);
            }
        }

        foreach (ZoneSystem.ZoneLocation zoneLocation in __instance.m_locations)
        {
            if (!zoneLocation.m_enable || IsTraderLocationMap.ContainsKey(zoneLocation.m_prefabName))
            {
                continue;
            }
            IsTraderLocationMap[zoneLocation.m_prefabName] = false;

            HashSet<AssetID> dependencies = AssetBundleLoader.Instance.GetDependenciesFromAssetID(zoneLocation.m_prefab.m_assetID);
            foreach (KeyValuePair<string, AssetID> item in TraderPrefabNameToAssetID)
            {
                if (dependencies.Contains(item.Value))
                {
                    IsTraderLocationMap[zoneLocation.m_prefabName] = true;
                    Log.LogInfo($"Location: {zoneLocation.m_prefabName} has Trader: {item.Key}");
                    break;
                }
            }
        }
        stopwatch.Stop();
        Log.LogInfo($"Time to search for trader locations: {stopwatch.ElapsedMilliseconds} ms");
        HasInit = true;
    }

    private static void IsValid()
    {
        if (!HasInit)
        {
            throw new Exception("InitTraderLocations should run before other method calls!");
        }
    }


    public static bool IsTraderLocation(ZoneSystem.LocationInstance locationInstance)
    {
        return IsTraderLocation(locationInstance.m_location);
    }

    public static bool IsTraderLocation(ZoneSystem.ZoneLocation zoneLocation)
    {
        IsValid();
        if (!IsTraderLocationMap.TryGetValue(zoneLocation.m_prefabName, out bool result))
        {
            result = false;
        }
        return result;
    }

    public static bool HasBeenDiscovered(ZoneSystem.ZoneLocation zoneLocation)
    {
        // find if any of the existing trader locations have been had any instances of them be discovered
        return false;
    }
}
