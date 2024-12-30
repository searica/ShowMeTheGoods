using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logging;
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
    private const float PinDuration = 10f; // seconds
    private Minimap.PinData TraderPinData   ;
    private const float DefaultRadius = 100f;
    private const string DefaultPinName = "Trader?";

    public void Awake()
    {
    }

    public void PinClosestUndiscoveredTraderLocation()
    {
        if (!CanReadMap()) { return; }
        if (this.FindClosestTraderLocation(out ZoneSystem.LocationInstance locationInstance))
        {
            Log.LogInfo($"Closest Trader Location: {locationInstance.m_location.m_prefabName}");
        }

        if (!Player.m_localPlayer || !Minimap.instance || !InventoryGui.instance)
        {
            return;
        }

        // Create Pin and modify size
        Vector3 pinPos = locationInstance.m_position;  // want to add random noise to this
        TraderPinData = Minimap.instance.AddPin(pinPos, Minimap.PinType.EventArea, DefaultPinName, save: false, isChecked: false);
        TraderPinData.m_worldSize = DefaultRadius; // want to add random noise to this

        // Show Pin on the map
        InventoryGui.instance.Hide();  // force close to prevent getting locked from input
        Sprite sprite = Minimap.instance.GetSprite(Minimap.PinType.EventArea);
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"$msg_pin_added: {DefaultPinName}", 0, sprite);
        Minimap.instance.ShowPointOnMap(pinPos);

        if (!this.gameObject.activeInHierarchy)
        {
            this.gameObject.SetActive(true);
        }
        Log.LogInfo($"GamObject active: {this.gameObject.activeInHierarchy}");
        Log.LogInfo($"TraderRouteMap active: {this.isActiveAndEnabled}");

        // Remove Pin after PinDuration elapses
        StartCoroutine(RemovePinDataAfter(PinDuration));
    }

    internal bool FindClosestTraderLocation(out ZoneSystem.LocationInstance closest)
    {
        closest = default;
        bool result = false;
   
        Vector3 point = Player.m_localPlayer.transform.position;
        float minDistance = float.MaxValue;
        foreach (ZoneSystem.LocationInstance locationInstance in ZoneSystem.instance.m_locationInstances.Values)
        {
            float distance = Vector3.Distance(locationInstance.m_position, point);
            if (TraderLocatonManager.IsTraderLocation(locationInstance.m_location) && distance < minDistance)
            {
                minDistance = distance;
                closest = locationInstance;
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
        if (TraderPinData is null)
        {
            return true;
        }

        if (Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Cannot read trade route map again yet.");
        }
        return false;
    }

    /// <summary>
    ///     Coroutine to wait until UpdatPInTime has elapsed and
    ///     then remove the PinData.
    /// </summary>
    /// <returns></returns>
    IEnumerator<object> RemovePinDataAfter(float seconds)
    {
        Log.LogInfo($"Will remove pin in {seconds} seconds");
        yield return new WaitForSeconds(seconds);
              
        if (TraderPinData != null && Minimap.instance)
        {
            Minimap.instance.RemovePin(TraderPinData);
        }
        yield return null;
    }
}
