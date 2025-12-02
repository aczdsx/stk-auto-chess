using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CookApps.TeamBattle.UIManagements;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace CookApps.Editor
{
    [InitializeOnLoad]
    internal static class UILayerConstantsGenerator
    {
        [MenuItem("Tools/CookApps/Generate UI Layer Constants", priority = 2100)]
        private static void RunFromMenu()
        {
            // Generate();
            // Debug.Log("CookApps: Regenerated UI Layer Address constants.");
        }

        private const string GeneratedFolderName = "Generated_UILayer";
        private const string NamespaceDefault = "CookApps.TeamBattle.UIManagements";
        private const string OutputFileName = "UILayerConstants.cs";

        private static readonly HashSet<AddressableAssetSettings> SubscribedSettings = new HashSet<AddressableAssetSettings>();

        static UILayerConstantsGenerator()
        {
            EditorApplication.update += Initialize;
        }

        private static void Initialize()
        {
            EditorApplication.update -= Initialize;
            TrySubscribe(AddressableAssetSettingsDefaultObject.Settings);
            Generate();
        }

        private static void TrySubscribe(AddressableAssetSettings settings)
        {
            if (settings == null) return;
            if (SubscribedSettings.Contains(settings)) return;

            settings.OnModification -= HandleSettingsModification;
            settings.OnModification += HandleSettingsModification;
            SubscribedSettings.Add(settings);
        }

        private static void HandleSettingsModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evt, object _)
        {
            switch (evt)
            {
                case AddressableAssetSettings.ModificationEvent.EntryAdded:
                case AddressableAssetSettings.ModificationEvent.EntryCreated:
                case AddressableAssetSettings.ModificationEvent.EntryModified:
                case AddressableAssetSettings.ModificationEvent.EntryMoved:
                case AddressableAssetSettings.ModificationEvent.EntryRemoved:
                case AddressableAssetSettings.ModificationEvent.GroupAdded:
                case AddressableAssetSettings.ModificationEvent.GroupRemoved:
                case AddressableAssetSettings.ModificationEvent.GroupRenamed:
                    TrySubscribe(settings);
                    Generate();
                    break;
            }
        }

        private static void Generate()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            TrySubscribe(settings);

            var outputPath = ResolveGenerationTarget();

            CollectAssetAddresses(settings, out var uiLayerAddresses, out var sceneAddresses);
            var generationInfo = new GenerationInfo(outputPath, NamespaceDefault);
            WriteConstants(uiLayerAddresses, sceneAddresses, generationInfo);
        }

        private static string ResolveGenerationTarget()
        {
            var runtimeFolder = LocateRuntimeFolderPath();
            var generatedFolder = CombineAssetPaths(runtimeFolder, GeneratedFolderName);
            var outputPath = CombineAssetPaths(generatedFolder, OutputFileName);
            return outputPath;
        }

        private static string LocateRuntimeFolderPath()
        {
            var assets = AssetDatabase.FindAssets("t: folder, UIManagements");
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Debug.Log(path);
                if (path.Contains("Scripts_Libs"))
                {
                    return path;
                }
            }
            
            return "Assets";
        }

        private static string NormalizeAssetPath(string value)
        {
            return (value ?? string.Empty).Replace('\\', '/');
        }

        private static string CombineAssetPaths(string left, string right)
        {
            left = NormalizeAssetPath(left).TrimEnd('/');
            right = NormalizeAssetPath(right).Trim('/');
            if (string.IsNullOrEmpty(left)) return right;
            if (string.IsNullOrEmpty(right)) return left;
            return $"{left}/{right}";
        }

        private static void CollectAssetAddresses(AddressableAssetSettings settings, out List<AssetAddressInfo> uiLayerAddresses, out List<AssetAddressInfo> sceneAddresses)
        {
            var collectedEntries = new List<AddressableAssetEntry>();
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                group.GatherAllAssets(collectedEntries, true, true, false);
            }

            uiLayerAddresses = new List<AssetAddressInfo>();
            sceneAddresses = new List<AssetAddressInfo>();
            var seenAssetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in collectedEntries)
            {
                if (entry == null) continue;
                if (string.IsNullOrEmpty(entry.AssetPath)) continue;
                if (string.IsNullOrEmpty(entry.address)) continue;
                if (!seenAssetPaths.Add(entry.AssetPath)) continue;

                if (entry.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                    if (prefab == null) continue;
                    var uiLayerComponent = prefab.GetComponent<UILayer>();
                    if (uiLayerComponent == null) continue;

                    var className = uiLayerComponent.GetType().Name;
                    var fieldName = BuildFieldName(className, usedFieldNames);
                    uiLayerAddresses.Add(new AssetAddressInfo(fieldName, entry.address));
                    continue;
                }

                if (entry.AssetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.AssetPath);
                    if (sceneAsset == null) continue;

                    var sceneName = sceneAsset.name;
                    var fieldName = BuildFieldName(sceneName, usedFieldNames);
                    sceneAddresses.Add(new AssetAddressInfo(fieldName, entry.address));
                }
            }
        }

        private static string BuildFieldName(string prefabName, HashSet<string> usedNames)
        {
            if (string.IsNullOrEmpty(prefabName)) prefabName = "Prefab";

            var builder = new StringBuilder(prefabName.Length);
            foreach (var c in prefabName)
            {
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            var candidate = builder.ToString();
            if (candidate.Length == 0)
            {
                candidate = "Prefab";
            }

            if (!char.IsLetter(candidate[0]) && candidate[0] != '_')
            {
                candidate = "_" + candidate;
            }

            if (usedNames.Contains(candidate))
            {
                var index = 1;
                var baseName = candidate;
                do
                {
                    candidate = $"{baseName}_{index++}";
                } while (usedNames.Contains(candidate));
            }

            usedNames.Add(candidate);
            return candidate;
        }

        private static void WriteConstants(IReadOnlyList<AssetAddressInfo> uiLayerAddresses, IReadOnlyList<AssetAddressInfo> sceneAddresses, GenerationInfo info)
        {
            var folder = Path.GetDirectoryName(info.OutputPath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// 이 파일은 UILayerAddressConstantsGenerator에 의해 자동 생성됩니다.");
            builder.AppendLine("// 수동으로 수정하지 마세요.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine();
            builder.Append("namespace ");
            builder.Append(info.Namespace);
            builder.AppendLine();
            builder.AppendLine("{");
            builder.AppendLine("    internal static class UILayerConstants");
            builder.AppendLine("    {");
            builder.AppendLine("        public static string GetUILayerAddress(string uiLayerName)");
            builder.AppendLine("        {");
            builder.AppendLine("            return uiLayerName switch");
            builder.AppendLine("            {");
            foreach (var entry in uiLayerAddresses)
            {
                builder.Append("                \"");
                builder.Append(entry.FieldName);
                builder.Append("\" => \"");
                builder.Append(entry.Address);
                builder.AppendLine("\",");
            }
            builder.AppendLine("                _ => string.Empty");
            builder.AppendLine("            };");
            builder.AppendLine("        }");

            builder.AppendLine();
            
            builder.AppendLine("        public static string GetSceneAddress(string sceneName)");
            builder.AppendLine("        {");
            builder.AppendLine("            return sceneName switch");
            builder.AppendLine("            {");
            foreach (var entry in sceneAddresses)
            {
                builder.Append("                \"");
                builder.Append(entry.FieldName);
                builder.Append("\" => \"");
                builder.Append(entry.Address);
                builder.AppendLine("\",");
            }
            builder.AppendLine("                _ => string.Empty");
            builder.AppendLine("            };");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var newContent = builder.ToString();
            if (File.Exists(info.OutputPath))
            {
                var existing = File.ReadAllText(info.OutputPath);
                if (existing == newContent) return;
            }

            File.WriteAllText(info.OutputPath, newContent, new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        private static string SanitizeNamespace(string value)
        {
            const string DefaultNamespace = "CookApps.UIManagements";

            if (string.IsNullOrWhiteSpace(value)) return DefaultNamespace;

            var segments = value.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return DefaultNamespace;

            var builder = new StringBuilder(value.Length);
            foreach (var segment in segments)
            {
                var sanitizedSegment = SanitizeNamespaceSegment(segment);
                if (sanitizedSegment.Length == 0) continue;
                if (builder.Length > 0) builder.Append('.');
                builder.Append(sanitizedSegment);
            }

            return builder.Length == 0 ? DefaultNamespace : builder.ToString();

            static string SanitizeNamespaceSegment(string segment)
            {
                if (string.IsNullOrEmpty(segment)) return string.Empty;

                var sb = new StringBuilder(segment.Length);
                var first = segment[0];
                sb.Append(char.IsLetter(first) || first == '_' ? first : '_');

                for (var i = 1; i < segment.Length; i++)
                {
                    var c = segment[i];
                    sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
                }

                return sb.ToString();
            }
        }

        private readonly struct AssetAddressInfo
        {
            public AssetAddressInfo(string fieldName, string address)
            {
                FieldName = fieldName;
                Address = address;
            }

            public string FieldName { get; }
            public string Address { get; }
        }

        private readonly struct GenerationInfo
        {
            public GenerationInfo(string outputPath, string namespaceName)
            {
                OutputPath = outputPath;
                Namespace = SanitizeNamespace(namespaceName);
            }

            public string OutputPath { get; }
            public string Namespace { get; }
        }
    }
}
