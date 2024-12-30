using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShowMeTheGoods.Extensions;
using UnityEngine;


namespace ShowMeTheGoods.Core;

/// <summary>
///     Handler for updating minimap pins tracking trader locations.
/// </summary>
internal class TradeRouteMap : MonoBehaviour
{
    /// <summary>
    ///  maybe make this static to prevent just spamming multiple maps
    /// </summary>
    private DateTime lastReadTime;
    //private const long cooldown = 5 * TimeSpan.TicksPerMinute;
    private const long cooldown = 5 * TimeSpan.TicksPerSecond;

    public void Awake()
    {
        lastReadTime = DateTime.MinValue;
    }

    public bool FindClosestTrader(out ZoneSystem.LocationInstance closest)
    {
        closest = default;
        bool result = false;

        if (!CanReadMap())
        {
            return result;
        }
    
        Vector3 point = Player.m_localPlayer.transform.position;
        float minDistance = float.MaxValue;
        foreach (ZoneSystem.LocationInstance value in ZoneSystem.instance.m_locationInstances.Values)
        {
            float distance = Vector3.Distance(value.m_position, point);
            if (value.m_location.IsTraderLocation() && distance < minDistance)
            {
                minDistance = distance;
                closest = value;
                result = true;
            }
        }
        return result;
    }

    /// <summary>
    ///     Check if cooldown since last read has elapsed.
    /// </summary>
    /// <returns></returns>
    private bool CanReadMap()
    {
        DateTime now = DateTime.Now;
        long deltaTime = now.Ticks - lastReadTime.Ticks;
        if (deltaTime < cooldown)
        {
            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Cannot read trade route map again yet.");
            }
            return false;
        }
        lastReadTime = now;
        return true;
    }

}
