using System;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor
{
    [CreateAssetMenu(fileName = "ProjectFolderSettings", menuName = "RabbitDog/Project Folder Settings", order = 0)]
    public sealed class ProjectFolderSettings : ScriptableObject
    {
        [SerializeField] private DefaultAsset rootFolder;
        [SerializeField] private DefaultAsset dataFolder;

        public DefaultAsset RootFolder => rootFolder;
        public DefaultAsset DataFolder => dataFolder;
        public string RootFolderPath
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(rootFolder);
                bool isFolder = AssetDatabase.IsValidFolder(path);
                Debug.Assert(isFolder);
                return path;
            }
        }
        public string DataFolderPath
        {
            get
            {
                string path = AssetDatabase.GetAssetPath(dataFolder);
                bool isFolder = AssetDatabase.IsValidFolder(path);
                Debug.Assert(isFolder);
                return path;
            }
        }
    }

    internal static class ProjectFolderSettingsProvider
    {
        private const string SettingsSearchFilter = "t:ProjectFolderSettings";
        private const string DefaultAssetPath = "Assets/Settings/ProjectFolderSettings.asset";

        private static ProjectFolderSettings cachedSettings;

        internal static ProjectFolderSettings GetOrCreateSettings()
        {
            if (cachedSettings != null) return cachedSettings;

            cachedSettings = LoadExistingSettings();
            if (cachedSettings != null) return cachedSettings;

            EnsureFolderHierarchy(System.IO.Path.GetDirectoryName(DefaultAssetPath)?.Replace('\\', '/') ?? "Assets");
            cachedSettings = ScriptableObject.CreateInstance<ProjectFolderSettings>();
            AssetDatabase.CreateAsset(cachedSettings, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"RabbitDog: Created default ProjectFolderSettings at '{DefaultAssetPath}'.");
            return cachedSettings;
        }

        internal static void EnsureFolderHierarchy(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            var segments = folderPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return;

            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }
                current = next;
            }
        }

        internal static string BuildChildPath(string parent, string child)
        {
            parent = (parent ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            child = (child ?? string.Empty).Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(parent)) return child;
            if (string.IsNullOrEmpty(child)) return parent;
            return $"{parent}/{child}";
        }

        private static ProjectFolderSettings LoadExistingSettings()
        {
            var guids = AssetDatabase.FindAssets(SettingsSearchFilter);
            if (guids == null || guids.Length == 0) return null;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<ProjectFolderSettings>(path);
                if (settings != null) return settings;
            }

            return null;
        }
    }
}
