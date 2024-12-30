using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoftReferenceableAssets;
using Logging;

namespace ShowMeTheGoods.Extensions;
internal static class AssetBundleLoaderExtensions
{
    private static readonly Dictionary<BundleLoader, HashSet<AssetID>> _bundleLoaderToAssetIDsMap = []; 
    private static void ComputeBundleLoaderToAssetIDDepeneciesMap()
    {
        foreach (AssetLoader assetLoader in AssetBundleLoader.Instance.m_assetLoaders)
        {
            var bundleLoader = AssetBundleLoader.Instance.GetBundleLoaderFromAssetID(assetLoader.m_assetID);
            if (!_bundleLoaderToAssetIDsMap.TryGetValue(bundleLoader, out HashSet<AssetID> assetIDs))
            {
                assetIDs = [];
                _bundleLoaderToAssetIDsMap[bundleLoader] = assetIDs;
            }
            assetIDs.Add(assetLoader.m_assetID);
        }
    }

    private static Dictionary<BundleLoader, HashSet<AssetID>> BundleLoaderToAssetIDsMap
    {
        get
        {
            if (_bundleLoaderToAssetIDsMap.Count == 0)
            {
                ComputeBundleLoaderToAssetIDDepeneciesMap();
            }
            return _bundleLoaderToAssetIDsMap;
        }
    }

    internal static BundleLoader GetBundleLoaderFromAssetID(this AssetBundleLoader assetBundleLoader, AssetID assetID)
    {
        var assetLoader = assetBundleLoader.GetLoader(assetID);
        return assetBundleLoader.m_bundleLoaders[assetLoader.m_bundleLoaderIndex];
    }

    internal static HashSet<AssetID> GetDependenciesFromAssetID(this AssetBundleLoader assetBundleLoader, AssetID assetID)
    {
        BundleLoader bundleLoader = assetBundleLoader.GetBundleLoaderFromAssetID(assetID);
        List<AssetID> dependencies = [];
        foreach (int i in bundleLoader.m_bundleLoaderIndicesOfThisAndDependencies)
        {
            BundleLoader loader = assetBundleLoader.m_bundleLoaders[i];
            if (BundleLoaderToAssetIDsMap.TryGetValue(loader, out HashSet<AssetID> assetIds))
            {
                dependencies.AddRange(assetIds);
            }            
        }
        return dependencies.ToHashSet();
    }
}
