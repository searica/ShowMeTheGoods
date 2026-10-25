using BepInEx.Bootstrap;

namespace ShowMeTheGoods.Core;


internal static class ModCompat
{
 
    public const string MWLGUID = "warpalicious.More_World_Locations_AIO";


    private static bool? _MWLInstalled;



    internal static bool IsMWLInstalled()
    {
        _MWLInstalled ??= Chainloader.PluginInfos.ContainsKey(MWLGUID);
        return _MWLInstalled.Value;
    }

}
