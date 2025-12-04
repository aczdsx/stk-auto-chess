/*
* Copyright (c) CookApps.
*/

using CookApps.NetLite.Editor.Asset;
using CookApps.Package.Window.Editor;
using UnityEditor;

namespace CookApps.NetLite.Editor.Window
{
    internal class PackageWindow
    {
        [InitializeOnLoadMethod]
        private static void AddToPackageWindow()
        {
            CookAppsPackageWindow.Add("NetLite", NetLiteAsset.AssetPath, OnInitialize);
        }

        private static void OnInitialize()
        {
            NetLiteAsset.GetOrCreateAsset();
        }
    }
}
