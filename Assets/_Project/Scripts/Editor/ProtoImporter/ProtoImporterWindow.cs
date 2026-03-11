#if UNITY_EDITOR
using CookApps.Package.Window.Editor;
using UnityEditor;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtoImporterWindow
    {
        [InitializeOnLoadMethod]
        private static void RegisterTab()
        {
            CookAppsPackageWindow.Add(
                "Proto Importer",
                ProtoImporterSetting.AssetPath,
                () => ProtoImporterSetting.GetOrCreateAsset());
        }
    }
}
#endif
