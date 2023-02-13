using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.U2D.Animation.Sample
{
    public class LoadSwapDLC : MonoBehaviour
    {
        const string k_AssetBundleName = "2DAnimationSampleAssetBundles";
        public SwapFullSkin[] swapFullSkin;

#if ASSETBUNDLE_ENABLED
        AssetBundle m_Bundle;
#endif
        
        public void LoadAssetBundle()
        {
#if ASSETBUNDLE_ENABLED
            if (m_Bundle)
                return;
            
            var assetBundlePath = Path.Combine(Application.streamingAssetsPath, k_AssetBundleName);
            m_Bundle = AssetBundle.LoadFromFile(Path.Combine(assetBundlePath, k_AssetBundleName));
            if (m_Bundle == null)
            {
                Debug.LogWarning("AssetBundle not found");
                return;
            }
            var manifest = m_Bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (manifest == null)
            {
                Debug.LogWarning("Unable to load manifest");
                return;
            }
            foreach (var assetBundleName in manifest.GetAllAssetBundles())
            {
                var subBundle = AssetBundle.LoadFromFile(Path.Combine(assetBundlePath, assetBundleName));
                var assets = subBundle.LoadAllAssets();
                foreach (var asset in assets)
                {
                    if (asset is UnityEngine.U2D.Animation.SpriteLibraryAsset)
                    {
                        var sla = (UnityEngine.U2D.Animation.SpriteLibraryAsset)asset;
                        foreach (var sfs in swapFullSkin)
                        {
                            var list = sfs.spriteLibraries.ToList();
                            list.Add(sla);
                            sfs.spriteLibraries = list.ToArray();
                        }
                        
                    }
                }
            }
            foreach (var sfs in swapFullSkin)
            {
                sfs.UpdateSelectionChoice();
            }
#endif 
        }

#if UNITY_EDITOR
        [ContextMenu("Build Asset Bundles")]
        void BuildBundles()
        {
            BuildAssetBundles();
        }

        public static void BuildAssetBundles()
        {
#if ASSETBUNDLE_ENABLED            
            string assetBundleDirectory = Path.Combine(Application.streamingAssetsPath, "2DAnimationSampleAssetBundles");
            if (!Directory.Exists(assetBundleDirectory))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }
            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
#endif  // ASSETBUNDLE_ENABLED
        }
#endif // UNITY_EDITOR
    }
}