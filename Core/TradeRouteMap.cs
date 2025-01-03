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
#if DEBUG
    private const float PinDuration = 10f; // seconds
    private const float DefaultRadius = 100f;
    private const float RadiusVariance = 10f;
    private const float RadialVariance = 0.9f;  // as a fraction of the variable radius
#else
    private const float PinDuration = 300f; // 
    private const float DefaultRadius = 1500f;
    private const float RadiusVariance = 300f;
    private const float RadialVariance = 0.9f;  // as a fraction of the variable radius
#endif
    private const string DefaultPinName = "Trader?";
    private Minimap.PinData LocationPinData;

    /// <summary>
    ///     Check if cooldown since last read has elapsed.
    /// </summary>
    /// <returns></returns>
    public bool CanReadMap()
    {
        if (LocationPinData is null && ZoneSystem.instance && ZoneSystem.instance.LocationsGenerated)
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

        // Get random radius of pin area.
        float areaRadius = DefaultRadius + Random.Range(-1f * RadiusVariance, RadiusVariance);

        // Compute variance in pin position via radial coordiantes to ensure the actual
        // trader location could be anywhere within the pin area.
        float posRadialVariance = Random.Range(0f, areaRadius * RadialVariance);
        float posThetaVariance = Random.Range(0f, 2f * Mathf.PI);
        float posXVariance = posRadialVariance * Mathf.Cos(posThetaVariance);
        float posZVariance = posRadialVariance * Mathf.Sin(posThetaVariance);

        // Create and modify pin
        Vector3 pinPos = new(
            locationInstance.m_position.x + posXVariance,
            locationInstance.m_position.y,
            locationInstance.m_position.z + posZVariance
        );
        LocationPinData = Minimap.instance.AddPin(pinPos, Minimap.PinType.EventArea, DefaultPinName, save: false, isChecked: false);
        LocationPinData.m_worldSize = areaRadius * 2f;

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
        Log.LogDebug($"Will remove pin in {seconds} seconds");
        yield return new WaitForSeconds(seconds);
              
        if (LocationPinData is not null)
        {
            if (Minimap.instance)
            {
                Minimap.instance.RemovePin(LocationPinData);
            }
            LocationPinData = null;
        }
        yield return null;
    }
}
