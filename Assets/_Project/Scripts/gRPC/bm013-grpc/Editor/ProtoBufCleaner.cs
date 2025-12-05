#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace CookApps.Editor
{
    internal static class ProtoBufCleaner
    {
        [InitializeOnLoadMethod]
        private static void EnsureSettingsAsset()
        {
            // 에디터 시작 시 설정 파일이 없으면 생성
            ProtoBufCleanupSetting.LoadOrCreate();
        }

        [MenuItem("Tools/ProtoBufCleaner/불필요한 Proto 파일 삭제")]
        public static void CleanViaMenu()
        {
            CleanInternal(forceLog: true);
        }

        [MenuItem("Tools/ProtoBufCleaner/설정 열기")]
        public static void OpenSettingAsset()
        {
            var asset = ProtoBufCleanupSetting.LoadOrCreate();
            if (asset == null)
            {
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            EditorUtility.FocusProjectWindow();
        }

        private static void CleanInternal(bool forceLog = false)
        {
            var settings = ProtoBufCleanupSetting.LoadOrCreate();
            if (settings == null)
            {
                return;
            }

            var targetAssetPath = (settings.targetDir ?? string.Empty).Replace("\\", "/").TrimEnd('/');
            if (string.IsNullOrEmpty(targetAssetPath))
            {
                if (forceLog)
                {
                    Debug.LogWarning("[ProtoBufCleaner] targetDir가 비어 있어 정리를 건너뜁니다.");
                }
                return;
            }

            var targetFullPath = Path.GetFullPath(targetAssetPath).Replace("\\", "/");
            if (!Directory.Exists(targetFullPath))
            {
                if (forceLog)
                {
                    Debug.LogWarning($"[ProtoBufCleaner] 경로를 찾을 수 없어 정리를 건너뜁니다: {targetFullPath}");
                }
                return;
            }

            var removeMatchers = BuildRemoveMatchers(settings.removeFileNames);
            if (removeMatchers.Count == 0)
            {
                if (forceLog)
                {
                    Debug.Log("[ProtoBufCleaner] 삭제 대상 파일명이 설정되어 있지 않아 정리를 건너뜁니다.");
                }
                return;
            }

            bool deleted = false;
            foreach (var file in Directory.GetFiles(targetFullPath, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (!ShouldRemove(fileName, removeMatchers))
                {
                    continue;
                }

                var assetPath = ToAssetPath(file, targetFullPath, targetAssetPath);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (AssetDatabase.DeleteAsset(assetPath))
                {
                    deleted = true;
                }
            }

            if (deleted)
            {
                AssetDatabase.Refresh();
                Debug.Log("[ProtoBufCleaner] 불필요한 생성 파일을 삭제했습니다.");
            }
            else if (forceLog)
            {
                Debug.Log("[ProtoBufCleaner] 삭제할 대상이 없습니다.");
            }
        }

        private static bool ShouldRemove(string fileName, List<Regex> matchers)
        {
            if (string.IsNullOrEmpty(fileName) || matchers == null || matchers.Count == 0)
            {
                return false;
            }

            return matchers.Any(regex => regex.IsMatch(fileName));
        }

        private static List<Regex> BuildRemoveMatchers(IEnumerable<string> names)
        {
            var matchers = new List<Regex>();
            if (names == null)
            {
                return matchers;
            }

            foreach (var raw in names)
            {
                var trimmed = raw?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                bool hasWildcard = trimmed.IndexOfAny(new[] { '*', '?' }) >= 0;
                if (hasWildcard)
                {
                    AddWildcardMatcher(matchers, trimmed);
                    AddWildcardMatcher(matchers, $"{trimmed}.meta");
                }
                else
                {
                    var baseName = Path.GetFileNameWithoutExtension(trimmed);
                    if (string.IsNullOrWhiteSpace(baseName))
                    {
                        continue;
                    }

                    AddExactMatcher(matchers, $"{baseName}.cs");
                    AddExactMatcher(matchers, $"{baseName}.cs.meta");
                }
            }

            return matchers;
        }

        private static void AddExactMatcher(List<Regex> matchers, string fileName)
        {
            matchers.Add(new Regex($"^{Regex.Escape(fileName)}$", RegexOptions.IgnoreCase));
        }

        private static void AddWildcardMatcher(List<Regex> matchers, string wildcard)
        {
            string pattern = "^" + Regex.Escape(wildcard)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            matchers.Add(new Regex(pattern, RegexOptions.IgnoreCase));
        }

        private static string ToAssetPath(string absolutePath, string targetFullPath, string targetAssetPath)
        {
            var normalizedFile = Path.GetFullPath(absolutePath).Replace("\\", "/");
            if (!normalizedFile.StartsWith(targetFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var relative = normalizedFile.Substring(targetFullPath.Length).TrimStart('/');
            return $"{targetAssetPath}/{relative}".Replace("\\", "/");
        }

        private class ProtoBufCleanerPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets,
                string[] deletedAssets,
                string[] movedAssets,
                string[] movedFromAssetPaths)
            {
                var settings = AssetDatabase.LoadAssetAtPath<ProtoBufCleanupSetting>(ProtoBufCleanupSetting.AssetPath);
                if (settings == null)
                {
                    return;
                }

                var targetAssetPath = (settings.targetDir ?? string.Empty).Replace("\\", "/").TrimEnd('/');
                if (string.IsNullOrEmpty(targetAssetPath))
                {
                    return;
                }

                // 생성 대상 폴더에 새로운 자산이 들어왔을 때만 정리 실행
                bool touched = importedAssets.Any(a => a.StartsWith(targetAssetPath, StringComparison.OrdinalIgnoreCase))
                               || movedAssets.Any(a => a.StartsWith(targetAssetPath, StringComparison.OrdinalIgnoreCase));
                if (touched)
                {
                    CleanInternal();
                }
            }
        }
    }
}
#endif
