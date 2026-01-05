using CookApps.TeamBattle.Utility;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace CookApps.Editor
{
    [InitializeOnLoad]
    internal static class SafeAreaSettingsInstaller
    {
        private const string DefaultGroupName = "Data";

        static SafeAreaSettingsInstaller()
        {
            EditorApplication.update += TryInstallOnce;
        }

        private static void TryInstallOnce()
        {
            EditorApplication.update -= TryInstallOnce;

            var projectFolders = ProjectFolderSettingsProvider.GetOrCreateSettings();
            var assetPath = ProjectFolderSettingsProvider.BuildChildPath(projectFolders.DataFolderPath, "SafeAreaSettings.asset");

            var so = AssetDatabase.LoadAssetAtPath<SafeAreaSettings>(assetPath);
            if (so == null)
            {
                EnsureFolders(projectFolders);
                so = ScriptableObject.CreateInstance<SafeAreaSettings>();
                so.left = 1f;
                so.right = 1f;
                so.top = 1f;
                so.bottom = 1f;
                AssetDatabase.CreateAsset(so, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"CookApps: Created default SafeAreaSettings asset at '{assetPath}'.");
            }

            TryMarkAddressable(so);
        }

        private static void EnsureFolders(ProjectFolderSettings settings)
        {
            ProjectFolderSettingsProvider.EnsureFolderHierarchy(settings.RootFolderPath);
            ProjectFolderSettingsProvider.EnsureFolderHierarchy(settings.DataFolderPath);
        }

        private static void TryMarkAddressable(SafeAreaSettings so)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("CookApps: Addressables not configured. SafeAreaSettings will not be addressable.");
                return;
            }

            var path = AssetDatabase.GetAssetPath(so);
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;

            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                var group = settings.FindGroup(DefaultGroupName);
                if (group == null)
                {
                    group = settings.CreateGroup(DefaultGroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                }

                entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = SafeAreaSettings.DefaultAddress;
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
                AssetDatabase.SaveAssets();
                Debug.Log($"CookApps: Marked SafeAreaSettings as Addressable at '{entry.address}'.");
            }
            else
            {
                if (entry.address != SafeAreaSettings.DefaultAddress)
                {
                    entry.address = SafeAreaSettings.DefaultAddress;
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"CookApps: Updated SafeAreaSettings address to '{entry.address}'.");
                }
            }
        }
    }
}

