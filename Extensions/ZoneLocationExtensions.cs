using System.Collections.Generic;
using UnityEngine;

namespace ShowMeTheGoods.Extensions;
internal static class ZoneLocationExtensions
{
    private static readonly Dictionary<string, bool> IsTraderLocationMap = [];


    /// <summary>
    ///     Checks if this is the name of a trader location using the cache of trader location names.
    /// </summary>
    /// <param name="locationPrefabName"></param>
    /// <returns></returns>
    public static bool IsCachedTraderLocation(string locationPrefabName)
    {
        if (!IsTraderLocationMap.TryGetValue(locationPrefabName, out bool result))
        {
            result = false;
        }
        return result;
    }

    /// <summary>
    ///     Checks if a ZoneLocation is a trader location and chaches the results.
    /// </summary>
    /// <param name="zoneLocation"></param>
    /// <returns></returns>
    public static bool IsTraderLocation(this ZoneSystem.ZoneLocation zoneLocation, bool release = true)
    {
        if (!zoneLocation.m_enable)
        {
            return false;
        }

        if (!IsTraderLocationMap.TryGetValue(zoneLocation.m_prefabName, out bool result)) 
        {
            result = zoneLocation.CheckIfIsTraderLocation(release);
            IsTraderLocationMap[zoneLocation.m_prefabName] = result;
        }
        return result;       
    }


    /// <summary>
    ///     Loads and inspect location to detemine if it has a trader in it.
    /// </summary>
    /// <param name="zoneLocation"></param>
    /// <param name="release">Whether to release the loaded asset after checking. Useful when inserting code via a transpiler.</param>
    /// <returns></returns>
    private static bool CheckIfIsTraderLocation(this ZoneSystem.ZoneLocation zoneLocation, bool release = true)
    {
        bool isTraderLocation = false;
        if (!zoneLocation.m_prefab.IsLoaded)
        {
            zoneLocation.m_prefab.Load();
        }
        GameObject root = zoneLocation.m_prefab.Asset;

        if (root.GetComponent<Trader>())
        {
            isTraderLocation = true;
        }
        else
        {
            // breadth first search for a trader component in children
            Queue<Transform> queue = [];
            Transform transform = root.transform;
            while (true)
            {
                int childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    queue.Enqueue(transform.GetChild(i));
                }
                if (queue.Count <= 0)
                {
                    break;
                }
                transform = queue.Dequeue();
                if (transform.GetComponent<Trader>())
                {
                    isTraderLocation = true;
                    break;
                }
            }
        }
        if (release)
        {
            zoneLocation.m_prefab.Release();
        }
        return isTraderLocation;
    }
}
