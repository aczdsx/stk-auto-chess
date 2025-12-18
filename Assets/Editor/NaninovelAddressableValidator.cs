#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace CookApps.Editor
{
    /// <summary>
    /// Naninovel Addressables 설정 검증 및 수정 도구
    /// </summary>
    public static class NaninovelAddressableValidator
    {
        [MenuItem("Tools/Naninovel/Validate Addressables")]
        public static void Validate()
        {
            Debug.Log("=== Naninovel Addressables 검증 시작 ===");
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings를 찾을 수 없습니다!");
                return;
            }
            
            var issues = new List<(AddressableAssetEntry entry, string issue)>();
            var naninovelEntries = new List<AddressableAssetEntry>();
            
            // Naninovel 라벨이 있는 모든 항목 수집
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;
                    if (!entry.labels.Contains("Naninovel")) continue;
                    
                    naninovelEntries.Add(entry);
                    
                    // 문제 검사
                    var issue = CheckEntry(entry);
                    if (issue != null)
                    {
                        issues.Add((entry, issue));
                    }
                }
            }
            
            Debug.Log($"\n총 Naninovel 항목: {naninovelEntries.Count}");
            Debug.Log($"문제 발견: {issues.Count}");
            
            if (issues.Count > 0)
            {
                Debug.LogError("\n⚠️ 문제가 있는 항목들:");
                foreach (var (entry, issue) in issues)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    Debug.LogError($"  [{entry.parentGroup.Name}] {issue}");
                    Debug.LogError($"    Address: {entry.address ?? "(empty)"}");
                    Debug.LogError($"    Asset: {assetPath}");
                    Debug.LogError($"    GUID: {entry.guid}");
                }
                
                Debug.LogError("\n💡 해결 방법:");
                Debug.LogError("  1. Tools/Naninovel/Fix Addressables 실행");
                Debug.LogError("  2. 또는 Addressables 창에서 수동으로 Address 수정");
                Debug.LogError("     - 형식: 'Naninovel/Type/Path' (예: Naninovel/Scripts/0-1)");
            }
            else
            {
                Debug.Log("✓ 모든 Naninovel Addressables 항목이 정상입니다!");
            }
        }
        
        [MenuItem("Tools/Naninovel/Fix Addressables")]
        public static void Fix()
        {
            Debug.Log("=== Naninovel Addressables 자동 수정 시작 ===");
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings를 찾을 수 없습니다!");
                return;
            }
            
            int fixedCount = 0;
            int failedCount = 0;
            
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;
                    if (!entry.labels.Contains("Naninovel")) continue;
                    
                    var issue = CheckEntry(entry);
                    if (issue == null) continue; // 문제 없음
                    
                    if (TryFixEntry(entry))
                    {
                        fixedCount++;
                        Debug.Log($"✓ 수정됨: {entry.address}");
                    }
                    else
                    {
                        failedCount++;
                        var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                        Debug.LogWarning($"⚠ 수정 실패: {assetPath}");
                    }
                }
            }
            
            if (fixedCount > 0 || failedCount > 0)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"\n=== 수정 완료 ===");
                Debug.Log($"수정됨: {fixedCount}");
                Debug.Log($"실패: {failedCount}");
            }
            else
            {
                Debug.Log("수정할 항목이 없습니다.");
            }
        }
        
        [MenuItem("Tools/Naninovel/Check Spawn Resources")]
        public static void CheckSpawnResources()
        {
            Debug.Log("=== Spawn 리소스 확인 ===");
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings를 찾을 수 없습니다!");
                return;
            }
            
            var spawnResources = new List<(AddressableAssetEntry entry, string expectedPath)>();
            var wrongAddresses = new List<(AddressableAssetEntry entry, string expectedPath)>();
            
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;
                    if (!entry.labels.Contains("Naninovel")) continue;
                    
                    var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(assetPath)) continue;
                    
                    // cbg_ 프리팹 찾기 (또는 다른 spawn 리소스)
                    if (assetPath.Contains("cbg_") && assetPath.EndsWith(".prefab"))
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                        var expectedPath = $"Naninovel/Spawn/{fileName}";
                        
                        spawnResources.Add((entry, expectedPath));
                        
                        if (entry.address != expectedPath)
                        {
                            wrongAddresses.Add((entry, expectedPath));
                            Debug.LogWarning($"⚠ 잘못된 주소: {fileName}");
                            Debug.LogWarning($"  현재: {entry.address}");
                            Debug.LogWarning($"  예상: {expectedPath}");
                            Debug.LogWarning($"  파일: {assetPath}");
                        }
                        else
                        {
                            Debug.Log($"✓ 올바른 주소: {fileName} -> {entry.address}");
                        }
                    }
                }
            }
            
            Debug.Log($"\n총 Spawn 리소스: {spawnResources.Count}");
            Debug.Log($"잘못된 주소: {wrongAddresses.Count}");
            
            if (wrongAddresses.Count > 0)
            {
                Debug.LogError("\n💡 수정 방법:");
                Debug.LogError("  Tools/Naninovel/Fix Spawn Resources 실행");
            }
        }
        
        [MenuItem("Tools/Naninovel/Fix Spawn Resources")]
        public static void FixSpawnResources()
        {
            Debug.Log("=== Spawn 리소스 주소 수정 시작 ===");
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings를 찾을 수 없습니다!");
                return;
            }
            
            int fixedCount = 0;
            
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                
                foreach (var entry in group.entries)
                {
                    if (entry == null) continue;
                    if (!entry.labels.Contains("Naninovel")) continue;
                    
                    var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                    if (string.IsNullOrEmpty(assetPath)) continue;
                    
                    // cbg_ 프리팹 찾기
                    if (assetPath.Contains("cbg_") && assetPath.EndsWith(".prefab"))
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                        var expectedPath = $"Naninovel/Spawn/{fileName}";
                        
                        if (entry.address != expectedPath)
                        {
                            entry.address = expectedPath;
                            fixedCount++;
                            Debug.Log($"✓ 수정됨: {fileName} -> {expectedPath}");
                        }
                    }
                }
            }
            
            if (fixedCount > 0)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                Debug.Log($"\n=== 수정 완료 ===");
                Debug.Log($"수정된 리소스: {fixedCount}");
            }
            else
            {
                Debug.Log("수정할 리소스가 없습니다.");
            }
        }
        
        private static string CheckEntry(AddressableAssetEntry entry)
        {
            // 빈 Address 체크
            if (string.IsNullOrEmpty(entry.address))
            {
                return "Address가 비어있습니다";
            }
            
            // "Naninovel/"로 시작하는지 체크
            if (!entry.address.StartsWith("Naninovel/"))
            {
                return $"Address가 'Naninovel/'로 시작하지 않습니다: {entry.address}";
            }
            
            // "/"가 포함되어 있는지 체크 (GetAfterFirst가 작동하려면 필요)
            if (!entry.address.Contains("/"))
            {
                return $"Address에 '/'가 없습니다: {entry.address}";
            }
            
            // 최소 3개 파트가 있는지 체크 (Naninovel/Type/Path)
            var parts = entry.address.Split('/');
            if (parts.Length < 3)
            {
                return $"Address 형식이 올바르지 않습니다 (최소 3개 파트 필요): {entry.address}";
            }
            
            return null; // 문제 없음
        }
        
        private static bool TryFixEntry(AddressableAssetEntry entry)
        {
            // Address가 비어있는 경우
            if (string.IsNullOrEmpty(entry.address))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return false;
                }
                
                // Asset 경로에서 Naninovel 경로 추출
                if (assetPath.Contains("Naninovel"))
                {
                    var naninovelIndex = assetPath.IndexOf("Naninovel");
                    var pathAfterNaninovel = assetPath.Substring(naninovelIndex);
                    
                    // "Assets/" 제거
                    if (pathAfterNaninovel.StartsWith("Assets/"))
                    {
                        pathAfterNaninovel = pathAfterNaninovel.Substring(7);
                    }
                    
                    // 확장자 제거
                    pathAfterNaninovel = System.IO.Path.ChangeExtension(pathAfterNaninovel, null);
                    
                    // 경로 구분자 정규화
                    pathAfterNaninovel = pathAfterNaninovel.Replace('\\', '/');
                    
                    entry.address = pathAfterNaninovel;
                    return true;
                }
                
                return false;
            }
            
            // Address가 "Naninovel/"로 시작하지 않는 경우
            if (!entry.address.StartsWith("Naninovel/"))
            {
                entry.address = "Naninovel/" + entry.address.TrimStart('/');
                return true;
            }
            
            return false; // 자동 수정 불가
        }
    }
}
#endif

