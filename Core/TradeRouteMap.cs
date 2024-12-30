using System.Collections.Generic;
using UnityEngine;
using Logging;

namespace ShowMeTheGoods.Core;

/// <summary>
///     Component to attach to items to identify them as a trade route map.
///     Also used by TraderLocationManager to control minimap pins for each player.
/// </summary>
internal class TradeRouteMap : MonoBehaviour
{
    private const float PinDuration = 10f; // seconds
    private Minimap.PinData LocationPinData;
    private const float DefaultRadius = 1500f;
    private const float RadiusVariance = 300f;
    private const float PositionVariance = RadiusVariance / 2f;
    private const string DefaultPinName = "Trader?";

    /// <summary>
    ///     Check if cooldown since last read has elapsed.
    /// </summary>
    /// <returns></returns>
    public bool CanReadMap()
    {
        if (LocationPinData is null)
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
    ///     Should always call CanReadMap to check if you should add a location pin.
    /// </summary>
    /// <param name="locationInstance"></param>
    public void AddLocationPin(ZoneSystem.LocationInstance locationInstance)
    {
        if (!Player.m_localPlayer || !Minimap.instance || !InventoryGui.instance)
        {
            return;
        }

        if (LocationPinData is not null)
        {
            Minimap.instance.RemovePin(LocationPinData);
        }

        // Create pin with variable position and size
        Vector3 pinPos = new( 
            locationInstance.m_position.x + Random.Range(-1f*PositionVariance, PositionVariance),
            locationInstance.m_position.y,
            locationInstance.m_position.z + Random.Range(-1f*PositionVariance, PositionVariance)
        );
        LocationPinData = Minimap.instance.AddPin(pinPos, Minimap.PinType.EventArea, DefaultPinName, save: false, isChecked: false);
        LocationPinData.m_worldSize = DefaultRadius + Random.Range(-1f*RadiusVariance, RadiusVariance); // want to add random noise to this

        // Show Pin on the map
        InventoryGui.instance.Hide();  // force close to prevent getting locked from input
        Sprite sprite = Minimap.instance.GetSprite(Minimap.PinType.EventArea);
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"$msg_pin_added: {DefaultPinName}", 0, sprite);
        Minimap.instance.ShowPointOnMap(pinPos);

        // Remove Pin after PinDuration elapses
        StartCoroutine(RemovePinDataAfter(PinDuration));
    }
 
    /// <summary>
    ///     Coroutine to wait until UpdatPInTime has elapsed and
    ///     then remove the PinData.
    /// </summary>
    /// <returns></returns>
    private IEnumerator<object> RemovePinDataAfter(float seconds)
    {
        Log.LogInfo($"Will remove pin in {seconds} seconds");
        yield return new WaitForSeconds(seconds);
              
        if (LocationPinData != null && Minimap.instance)
        {
            Minimap.instance.RemovePin(LocationPinData);
            LocationPinData = null;
        }
        yield return null;
    }
}
