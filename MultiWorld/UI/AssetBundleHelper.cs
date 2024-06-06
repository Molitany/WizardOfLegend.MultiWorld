using UnityEngine;

namespace MultiWorld.UI;

internal class AssetBundleHelper
{
    public static AssetBundle localAssetBundle { get; private set; }
    internal static void LoadBundle()
    {
        localAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(MultiWorldPlugin.Instance.Info.Location), "connectbundle"));
        if (localAssetBundle == null)
        {
            Debug.LogError("Failed to load AssetBundle!");
            return;
        }
    }
    internal static GameObject LoadPrefab(string name)
    {
        return localAssetBundle?.LoadAsset<GameObject>(name);
    }
}