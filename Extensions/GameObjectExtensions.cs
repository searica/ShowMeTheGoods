using UnityEngine;

namespace ShowMeTheGoods.Extensions;
internal static class GameObjectExtensions
{

    public static bool IsRootPrefab(this GameObject go)
    {
        return !go.transform.parent;
    }
}
