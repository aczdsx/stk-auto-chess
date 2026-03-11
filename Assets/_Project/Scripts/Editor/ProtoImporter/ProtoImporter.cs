#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    internal static class ProtoImporter
    {
        /// <summary>
        /// 설정 에셋 기반으로 API 업데이트를 수행합니다.
        /// clone → C# 생성 → cleanup → AssetDatabase 갱신
        /// </summary>
        public static async void RunFromSetting()
        {
            var setting = ProtoImporterSetting.GetOrCreateAsset();
            if (!setting.IsValid())
                return;

            try
            {
                await System.Threading.Tasks.Task.Yield();

                // 1. Library에 임시 폴더 생성
                string tempDir = Path.GetFullPath("Library/TempProto");
                ProtoFileUtil.EnsureDirectoryExists(tempDir);
                ProtoFileUtil.ClearDirectoryContents(tempDir);

                // 2. IDL 저장소 clone + 필터링 + 평탄화
                var result = await ProtoCloner.RunCloneScript(
                    setting.IdlProjectName, tempDir,
                    setting.IncludeServices, setting.ExcludeServices,
                    new CancellationTokenSource());

                if (!result.Result)
                {
                    Debug.LogError($"[ProtoImporter] git에서 {setting.IdlProjectName} proto 파일 복제에 실패했습니다.");
                    return;
                }

                // 3. C# 코드 생성
                string csDir = Path.GetFullPath(Path.Combine(tempDir, "Generated/CSharp"));
                ProtocRunner.GenerateCSharp(result.ProtoDir, csDir);

                // 4. 생성된 파일을 타겟 디렉토리로 복사
                string targetFullDir = Path.GetFullPath(setting.TargetDir);
                ProtoFileUtil.ClearDirectoryContents(Path.Combine(targetFullDir, "Generated/CSharp"));
                ProtoFileUtil.ClearDirectoryContents(Path.Combine(targetFullDir, "Generated/Proto"));
                ProtoFileUtil.CopyAllFilesToDirectory(tempDir, targetFullDir);

                // 5. Cleanup — 불필요 파일 삭제
                CleanupGeneratedFiles(setting);

                // 6. AssetDatabase 갱신
                AssetDatabase.Refresh();

                Debug.Log("[ProtoImporter] API 업데이트가 완료되었습니다.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 설정의 removeFileNames 기준으로 생성 폴더에서 불필요한 파일을 삭제합니다.
        /// </summary>
        internal static void CleanupGeneratedFiles(ProtoImporterSetting setting)
        {
            if (setting == null || setting.RemoveFileNames == null || setting.RemoveFileNames.Count == 0)
                return;

            string targetAssetPath = (setting.CleanupTargetDir ?? string.Empty).Replace("\\", "/").TrimEnd('/');
            if (string.IsNullOrEmpty(targetAssetPath))
                return;

            string targetFullPath = Path.GetFullPath(targetAssetPath).Replace("\\", "/");
            if (!Directory.Exists(targetFullPath))
                return;

            var matchers = BuildRemoveMatchers(setting.RemoveFileNames);
            if (matchers.Count == 0)
                return;

            bool deleted = false;
            foreach (var file in Directory.GetFiles(targetFullPath, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (!ShouldRemove(fileName, matchers))
                    continue;

                var assetPath = ToAssetPath(file, targetFullPath, targetAssetPath);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                if (AssetDatabase.DeleteAsset(assetPath))
                    deleted = true;
            }

            if (deleted)
            {
                AssetDatabase.Refresh();
                Debug.Log("[ProtoImporter] 불필요한 생성 파일을 삭제했습니다.");
            }
        }

        private static bool ShouldRemove(string fileName, List<Regex> matchers)
        {
            if (string.IsNullOrEmpty(fileName) || matchers == null || matchers.Count == 0)
                return false;
            return matchers.Any(regex => regex.IsMatch(fileName));
        }

        private static List<Regex> BuildRemoveMatchers(IEnumerable<string> names)
        {
            var matchers = new List<Regex>();
            if (names == null)
                return matchers;

            foreach (var raw in names)
            {
                var trimmed = raw?.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

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
                        continue;
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
                return string.Empty;

            var relative = normalizedFile.Substring(targetFullPath.Length).TrimStart('/');
            return $"{targetAssetPath}/{relative}".Replace("\\", "/");
        }

        /// <summary>
        /// AssetPostprocessor — 생성 폴더에 새 파일이 들어오면 자동으로 cleanup 수행
        /// </summary>
        internal class ProtoImporterPostprocessor : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(
                string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
            {
                var setting = AssetDatabase.LoadAssetAtPath<ProtoImporterSetting>(ProtoImporterSetting.AssetPath);
                if (setting == null)
                    return;

                string targetAssetPath = (setting.CleanupTargetDir ?? string.Empty).Replace("\\", "/").TrimEnd('/');
                if (string.IsNullOrEmpty(targetAssetPath))
                    return;

                bool touched = importedAssets.Any(a =>
                        a.StartsWith(targetAssetPath, StringComparison.OrdinalIgnoreCase))
                    || movedAssets.Any(a =>
                        a.StartsWith(targetAssetPath, StringComparison.OrdinalIgnoreCase));
                if (touched)
                    CleanupGeneratedFiles(setting);
            }
        }
    }
}
#endif
