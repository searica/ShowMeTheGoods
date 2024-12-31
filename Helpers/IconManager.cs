// Ignore Spelling: MVBP

using Jotunn.Managers;
using Logging;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShowMeTheGoods.Helpers;

internal class IconManager : MonoBehaviour
{
    private static GameObject _gameObject;
    private static IconManager _instance;

    /// <summary>
    ///     The singleton instance of this manager.
    /// </summary>
    internal static IconManager Instance => CreateInstance();

    private static IconManager CreateInstance()
    {
        if (!_gameObject)
        {
            _gameObject = new GameObject();
            DontDestroyOnLoad(_gameObject);
        }

        if (!_instance)
        {
            _instance = _gameObject.AddComponent<IconManager>();
        }

        return _instance;
    }

    /// <summary>
    ///     Hide .ctor to prevent other instances from being created
    /// </summary>
    private IconManager() { }

    public void GeneratePrefabItemDropIcons(IEnumerable<GameObject> prefabs)
    {
        StartCoroutine(RenderCoroutine(prefabs));
    }

    private IEnumerator RenderCoroutine(IEnumerable<GameObject> gameObjects)
    {
        foreach (var gameObject in gameObjects)
        {
            if (!gameObject)
            {
                Log.LogWarning($"Null prefab, cannot render icon");
                continue;
            }

            if (!gameObject.TryGetComponent(out ItemDrop itemDrop))
            {
                Log.LogWarning($"Missing ItemDrop, cannot render icon");
                continue;
            }

            Sprite result = GenerateObjectIcon(gameObject);

            // returning WaitForEndOfFrame seems to fix the lighting bug in the icons
            yield return new WaitForEndOfFrame();

            if (result is null)
            {
                Log.LogWarning($"Failed to generate icon for {gameObject.name}!");
                continue;
            }
            itemDrop.m_itemData.m_shared.m_icons = new Sprite[] { result };
        }
    }

    private static Sprite GenerateObjectIcon(GameObject obj)
    {
        var request = new RenderManager.RenderRequest(obj)
        {
            Rotation = RenderManager.IsometricRotation,
            UseCache = true
        };

        return RenderManager.Instance.Render(request);
    }
}
