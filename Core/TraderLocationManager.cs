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
internal sealed class TraderLocationManager
{
    
    private readonly Dictionary<string, bool> IsTraderLocationMap = [];
    private readonly Dictionary<string, AssetID> TraderPrefabNameToAssetID = [];
    private GameObject gameObject;
    private TradeRouteMap TraderRouteMap;
    public static TraderLocationManager Instance;
    private static bool HasInit = false;

    private static bool IsValid()
    {
        return HasInit;
    }

    /// <summary>
    ///     Scan locations to see which depend on the trader prefab assets
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.Start))]
    private static void InitTraderLocationManager(ZoneSystem __instance)
    {
        // If loading into game world and prefabs have not been added
        if (SceneManager.GetActiveScene().name != "main" || !__instance || !ZNetScene.instance)
        {
            return;
        }
        
        if (!IsValid())
        {
            Instance = new() { gameObject = new("TraderLocationManager") };
            Instance.TraderRouteMap = Instance.gameObject.AddComponent<TradeRouteMap>();
            GameObject.DontDestroyOnLoad(Instance.gameObject);
            HasInit = true;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            if (!prefab || !prefab.IsRootPrefab() || Instance.TraderPrefabNameToAssetID.ContainsKey(prefab.name))
            {
                continue;
            }

            if (prefab.GetComponent<Trader>())
            {
                AssetID assetID = AssetManager.Instance.GetAssetID<GameObject>(prefab.name);
                Log.LogInfo($"Found Trader: {prefab.name}, AssetID: {assetID}");
                Instance.TraderPrefabNameToAssetID.Add(prefab.name, assetID);
            }
        }

        foreach (ZoneSystem.ZoneLocation zoneLocation in __instance.m_locations)
        {
            if (!zoneLocation.m_enable || Instance.IsTraderLocationMap.ContainsKey(zoneLocation.m_prefabName))
            {
                continue;
            }
            Instance.IsTraderLocationMap[zoneLocation.m_prefabName] = false;

            HashSet<AssetID> dependencies = AssetBundleLoader.Instance.GetDependenciesFromAssetID(zoneLocation.m_prefab.m_assetID);
            foreach (KeyValuePair<string, AssetID> item in Instance.TraderPrefabNameToAssetID)
            {
                if (dependencies.Contains(item.Value))
                {
                    Instance.IsTraderLocationMap[zoneLocation.m_prefabName] = true;
                    Log.LogInfo($"Location: {zoneLocation.m_prefabName} has Trader: {item.Key}");
                    break;
                }
            }
        }
        stopwatch.Stop();
        Log.LogInfo($"Time to search for trader locations: {stopwatch.ElapsedMilliseconds} ms");
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), nameof(Player.UseItem))]
    private static void UseTradeRouteMap(Player __instance, ItemDrop.ItemData item, bool fromInventoryGui)
    {
        Log.LogInfo("Plyer.UseItem postfix!");
        if (!__instance || !fromInventoryGui || item is null || item.m_shared is null || !item.m_dropPrefab)
        {
            return;
        }

        if (!item.m_dropPrefab.GetComponent<TradeRouteMap>())
        {
            return;
        }
        Instance.PinClosestUndiscoveredTrader();
    }

    private void PinClosestUndiscoveredTrader()
    {
        if (!Instance.TraderRouteMap.CanReadMap())
        {
            return;
        }

        if (Instance.FindClosestTraderLocation(out ZoneSystem.LocationInstance closest))
        {
            Log.LogInfo($"Closest Trader Location: {closest.m_location.m_prefabName}");
            Instance.TraderRouteMap.AddLocationPin(closest);
        }
    }

    private bool HasBeenDiscovered(ZoneSystem.ZoneLocation zoneLocation)
    {
        // find if any of the existing trader locations have been had any instances of them be discovered
        return false;
    }

    private bool FindClosestTraderLocation(out ZoneSystem.LocationInstance closest)
    {
        closest = default;
        bool result = false;

        Vector3 point = Player.m_localPlayer.transform.position;
        float minDistance = float.MaxValue;
        foreach (ZoneSystem.LocationInstance locationInstance in ZoneSystem.instance.m_locationInstances.Values)
        {
            float distance = Vector3.Distance(locationInstance.m_position, point);
            if (Instance.IsTraderLocation(locationInstance.m_location) && distance < minDistance)
            {
                minDistance = distance;
                closest = locationInstance;
                result = true;
            }
        }
        return result;
    }

    private bool IsTraderLocation(ZoneSystem.LocationInstance locationInstance)
    {
        return IsTraderLocation(locationInstance.m_location);
    }

    private bool IsTraderLocation(ZoneSystem.ZoneLocation zoneLocation)
    {
        if (!IsValid())
        {
            return false;
        }

        if (!IsTraderLocationMap.TryGetValue(zoneLocation.m_prefabName, out bool result))
        {
            result = false;
        }
        return result;
    }
}
