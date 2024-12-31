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
    private readonly HashSet<string> TraderLocationNames = [];
    private readonly Dictionary<string, bool> IsTraderLocationMap = [];
    private readonly Dictionary<string, bool> IsTraderLocationUniqueMap = [];
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
    ///     Initialize this manager by scanning assets and bundles to determine which locations
    ///     depend on one the Trader's AssetIDs
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
                Log.LogInfo($"Found Trader: {prefab.name}, AssetID: {assetID}", Log.InfoLevel.Medium);
                Instance.TraderPrefabNameToAssetID.Add(prefab.name, assetID);
            }
        }

        foreach (ZoneSystem.ZoneLocation zoneLocation in __instance.m_locations)
        {
            if (!zoneLocation.m_enable || 
                Instance.IsTraderLocationMap.ContainsKey(zoneLocation.m_prefabName) || 
                Instance.TraderLocationNames.Contains(zoneLocation.m_prefabName))
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
                    Instance.TraderLocationNames.Add(zoneLocation.m_prefabName);
                    Instance.IsTraderLocationUniqueMap[zoneLocation.m_prefabName] = zoneLocation.m_unique;
                    string isUniqueText = zoneLocation.m_unique ? "is Unique" : "is Not Unique";
                    Log.LogInfo($"Location: {zoneLocation.m_prefabName} has Trader: {item.Key} and {isUniqueText}", Log.InfoLevel.Medium);
                    break;
                }
            }
        }
        stopwatch.Stop();
        Log.LogInfo($"Time to search for trader locations: {stopwatch.ElapsedMilliseconds} ms");

        if (!ZNet.instance.IsServer())
        {
            ZRoutedRpc.instance.Register<ZPackage>("TraderLocations", TraderLocationManager.Instance.RPC_TraderLocations);
        }
    }


    /// <summary>
    ///     Get and send trader locations whenever sending location icons.
    ///     This only runs on the server which should keep all 
    ///     location instances loaded.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="peer"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SendLocationIcons))]
    private static void SendTraderLocations(ZoneSystem __instance, long peer)
    {
        if (!ZNet.instance.IsServer())
        {
            return;
        }
        ZPackage zPackage = new ZPackage();
        Dictionary<string, List<ZoneSystem.LocationInstance>> traderLocations = TraderLocationManager.Instance.GetTraderLocationInstances();
        zPackage.Write(traderLocations.Keys.Count);
        foreach (KeyValuePair<string, List<ZoneSystem.LocationInstance>> item in traderLocations)
        {
            zPackage.Write(item.Key);
            zPackage.Write(item.Value.Count);
            foreach (ZoneSystem.LocationInstance location in item.Value)
            {
                zPackage.Write(location.m_location.m_prefabName);
                zPackage.Write(location.m_position.x);
                zPackage.Write(location.m_position.y);
                zPackage.Write(location.m_position.z);
                zPackage.Write(location.m_placed);
            }
        }
        ZRoutedRpc.instance.InvokeRoutedRPC(peer, "TraderLocations", zPackage);
    }

    /// <summary>
    ///     Recieve and register trader locations sent from SendTraderLocations patch as Location Instances.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="pkg"></param>
    private void RPC_TraderLocations(long sender, ZPackage pkg)
    {
        Log.LogInfo("Client recived trader locations.");
        int numUniqueTraderLocations = pkg.ReadInt();
        for (int i = 0; i < numUniqueTraderLocations; i++)
        {
            string locationName = pkg.ReadString();
            int numInstances = pkg.ReadInt();
            Log.LogInfo($"Parsing {numInstances} instances of location: {locationName}");
            for (int j = 0; j < numInstances; j++)
            {
                string text = pkg.ReadString();
                Vector3 zero = Vector3.zero;
                zero.x = pkg.ReadSingle();
                zero.y = pkg.ReadSingle();
                zero.z = pkg.ReadSingle();
                bool generated = pkg.ReadBool();
                ZoneSystem.ZoneLocation location = ZoneSystem.instance.GetLocation(text);
                if (location is not null)
                {
                    ZoneSystem.instance.RegisterLocation(location, zero, generated);
                }
                else
                {
                    Log.LogDebug($"Failed to find instance {j + 1} of location {locationName}");
                }
            }
        }
    }

    /// <summary>
    ///     Patch to detect when a player uses a trade route map from their inventory.
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="item"></param>
    /// <param name="fromInventoryGui"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), nameof(Player.UseItem))]
    private static void UseTradeRouteMap(Player __instance, ItemDrop.ItemData item, bool fromInventoryGui)
    {
        if (!__instance || !fromInventoryGui || item is null || item.m_shared is null || !item.m_dropPrefab)
        {
            return;
        }

        if (item.m_dropPrefab.GetComponent<TradeRouteMap>())
        {
            Instance.PinClosestUndiscoveredTrader();
        }
    }


    /// <summary>
    ///     Pin the closest undiscovereed trader on the map if one exists.
    /// </summary>
    private void PinClosestUndiscoveredTrader()
    {
        if (!Instance.TraderRouteMap.CanReadMap())
        {
            return;
        }

        if (FindClosestUndiscoveredTraderLocation(out ZoneSystem.LocationInstance closest))
        {
            Instance.TraderRouteMap.AddLocationPin(closest);
        }
        else if (Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "You have found all the traders!");
        }
    }


    /// <summary>
    ///     Find closest Trader LocationInstance that has not been discovered and has not
    ///     had a different instance discovered if it is meant to be a unique location.
    /// </summary>
    /// <param name="closest"></param>
    /// <returns></returns>
    private bool FindClosestUndiscoveredTraderLocation(out ZoneSystem.LocationInstance closest)
    {
        Dictionary<string, List<ZoneSystem.LocationInstance>> traderLocationInstances = Instance.GetTraderLocationInstances();

        closest = default;
        bool result = false;
        float minDistance = float.MaxValue;
        foreach (KeyValuePair<string, List<ZoneSystem.LocationInstance>> item in traderLocationInstances)
        {
            bool isUniqueLocation = IsTraderLocationUniqueMap[item.Key];
            bool foundUndiscoveredLocation = Instance.FindClosestUndiscoveredLocationInList(
                isUniqueLocation,
                item.Value,
                out float distance,
                out ZoneSystem.LocationInstance closestLocationInstance
            );
            Log.LogInfo($"{item.Key} has undiscovered location: {foundUndiscoveredLocation}", Log.InfoLevel.Medium);

            if (foundUndiscoveredLocation && distance < minDistance)
            {
                minDistance = distance;
                closest = closestLocationInstance;
                result = true;   
            }
        }
        return result;
    }


    /// <summary>
    ///     Get map of trader location prefab names to location instances.
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, List<ZoneSystem.LocationInstance>> GetTraderLocationInstances()
    {
        Dictionary<string, List<ZoneSystem.LocationInstance>> traderLocationInstances = [];
        foreach (string name in Instance.TraderLocationNames)
        {
            traderLocationInstances.Add(name, []);
        }

        foreach (ZoneSystem.LocationInstance locationInstance in ZoneSystem.instance.m_locationInstances.Values)
        {
            string locationName = locationInstance.m_location.m_prefabName;
            
            if (traderLocationInstances.ContainsKey(locationName))
            {
                traderLocationInstances[locationName].Add(locationInstance);
            }
        }

        foreach (string locationName in Instance.TraderLocationNames)
        {
            int nLocations = traderLocationInstances[locationName].Count;
            Log.LogInfo($"{locationName} has {nLocations} Location Instances", Log.InfoLevel.Medium);
        }

        return traderLocationInstances;
    }


    /// <summary>
    ///     Find closest undiscovered location.
    /// </summary>
    /// <param name="isUniqueLocation">Whether all location instances are meant to be the same unique location.</param>
    /// <param name="locationInstances"></param>
    /// <param name="minDistance"></param>
    /// <param name="closest"></param>
    /// <returns></returns>
    private bool FindClosestUndiscoveredLocationInList(
        bool isUniqueLocation, 
        List<ZoneSystem.LocationInstance> locationInstances, 
        out float minDistance, 
        out ZoneSystem.LocationInstance closest
    )
    {
        closest = default;
        minDistance = float.MaxValue;
        bool result = false;
        Vector3 point = Player.m_localPlayer.transform.position;

        foreach (ZoneSystem.LocationInstance locationInstance in locationInstances)
        {
            if (Instance.HasLocationInstanceBeenDiscovered(locationInstance))
            {
                // end search and say it failed if these locations are unique and have already been discoverd.
                if (isUniqueLocation)
                {
                    Log.LogInfo($"Location {locationInstance.m_location.m_prefabName} is Unique and has already been discovered");
                    return false;
                }
                continue;  // don't check if this instance already discovered
            }

            float distance = Vector3.Distance(locationInstance.m_position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = locationInstance;
                result = true;
            }
        }

        return result;
    }

    /// <summary>
    ///     Checks if the location has been pinned on the map already.
    /// </summary>
    /// <param name="locationInstance"></param>
    /// <returns></returns>
    private bool HasLocationInstanceBeenDiscovered(ZoneSystem.LocationInstance locationInstance)
    {
        return locationInstance.m_location.m_iconAlways || (locationInstance.m_location.m_iconPlaced && locationInstance.m_placed);
    }
}
