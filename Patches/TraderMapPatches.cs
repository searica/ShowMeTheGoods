using HarmonyLib;
using Logging;
using ShowMeTheGoods.Core;

namespace ShowMeTheGoods.Patches;

[HarmonyPatch]
internal static class TraderMapPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), nameof(Player.UseItem))]
    private static void UseTradeRouteMap(Player __instance, ItemDrop.ItemData item, bool fromInventoryGui)
    {
        Log.LogInfo("Plyer.UseItem postfix!");
        if (!__instance || !fromInventoryGui || item is null || item.m_shared is null || !item.m_dropPrefab)
        {
            return;
        }     
        
        if (!item.m_dropPrefab.TryGetComponent(out TradeRouteMap tradeRouteMap))
        {
            return;
        }

        Log.LogInfo($"Trade Route Map Item: {item.m_shared.m_name}");
        Log.LogInfo($"From inventory: {fromInventoryGui}");

        tradeRouteMap.FindClosestTrader(out ZoneSystem.LocationInstance locationInstance);

    }
}
