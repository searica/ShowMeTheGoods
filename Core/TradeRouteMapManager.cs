using UnityEngine;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Logging;
using ShowMeTheGoods.Helpers;

namespace ShowMeTheGoods.Core;

[HarmonyPatch]
internal static class TradeRouteMapManager
{
    private const string TraderMapPrefabName = "ShowMeTheGoods_TradeRouteMap";
    private static CustomItem TradeRouteMapCustomItem;
    private static Trader.TradeItem TradeRouteMapTradeItem;

    /// <summary>
    ///     Get or create TradeRouteMap CustomItem
    /// </summary>
    /// <returns></returns>
    public static CustomItem GetTradeRouteMapCustomItem()
    {
        if (TradeRouteMapCustomItem is not null)
        {
            return TradeRouteMapCustomItem;
        }

        // Create and add a custom item based on SwordBlackmetal
        ItemConfig traderMapConfig = new()
        {
            Name = "Trade Route Map",
            Description = "Map that helps to find possible trader locations.",
            CraftingStation = CraftingStations.None,
            Enabled = false, // make not craftable
            StackSize = 1,
            Weight = 1f
        };

        // Start setting up a customized prefab to modify the appearance of the item
        GameObject mapPrefab = PrefabManager.Instance.CreateClonedPrefab(TraderMapPrefabName, "Iron");
        mapPrefab.transform.localRotation = Quaternion.identity;
        mapPrefab.AddComponent<TradeRouteMap>();

        Transform modelPrefab = mapPrefab.transform.Find("model");
        if (modelPrefab)  // Customize model size
        {
            modelPrefab.transform.localPosition = Vector3.zero;
            modelPrefab.transform.localRotation = Quaternion.identity;
            modelPrefab.transform.localScale = new Vector3(1.1f, 0.1f, 0.7f);
        }

        try  // Customize material
        {
            GameObject hildirMapTable = PrefabManager.Instance.GetPrefab("hildir_maptable");
            MeshRenderer meshRenderer = modelPrefab.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = hildirMapTable.GetComponentInChildren<MeshRenderer>().sharedMaterial;
        }
        catch
        {
            Log.LogWarning($"Failed to customize material of {TraderMapPrefabName}!");
        }

        try  // Customize item type
        {
            ItemDrop itemDrop = mapPrefab.GetComponent<ItemDrop>();
            itemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.None;
            itemDrop.m_itemData.m_shared.m_teleportable = true;
            IconManager.Instance.GeneratePrefabItemDropIcons(new GameObject[] { mapPrefab });
        }
        catch
        {
            Log.LogWarning($"Failed to customize item type of {TraderMapPrefabName}!");
        }


        // Create and add customized map
        TradeRouteMapCustomItem = new(mapPrefab, true, traderMapConfig);

        return TradeRouteMapCustomItem;
    }

    /// <summary>
    ///     Get or create TradeRouteMap TradeItem
    /// </summary>
    /// <returns></returns>
    public static Trader.TradeItem GetTradeRouteMapTradeItem()
    {
        if (TradeRouteMapTradeItem is not null)
        {
            return TradeRouteMapTradeItem;
        }

        var tradeMap = GetTradeRouteMapCustomItem();

        TradeRouteMapTradeItem = new Trader.TradeItem()
        {
            m_prefab = tradeMap.ItemDrop,
            m_stack = 1,
            m_price = ShowMeTheGoods.Instance.MapCost.Value
        };

        return TradeRouteMapTradeItem;
    }


    /// <summary>
    ///     Update trade route map cost.
    /// </summary>
    public static void UpdateTradeItemCost()
    { 
        Trader.TradeItem tradeItem = GetTradeRouteMapTradeItem();
        tradeItem.m_price = ShowMeTheGoods.Instance.MapCost.Value;
    }


    /// <summary>
    ///     Add trade rout map to trader inventories when they start up.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Trader), nameof(Trader.Start))]
    private static void AddTradeRouteMapToTraderItems(Trader __instance)
    {
        if (!__instance)
        {
            return;
        }
        __instance.m_items.Add(GetTradeRouteMapTradeItem());
    }
}
