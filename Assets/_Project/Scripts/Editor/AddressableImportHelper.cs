#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public static class AddressableImportHelper
{
    /// <summary>
    /// 에셋 또는 폴더를 지정된 주소와 레이블로 Addressable 그룹에 추가
    /// </summary>
    /// <param name="assetPath">추가할 에셋 또는 폴더의 경로</param>
    /// <param name="groupName">Addressable 그룹 이름</param>
    /// <param name="addressKey">에셋의 주소 키</param>
    /// <param name="labels">엔트리에 적용할 레이블 (선택사항)</param>
    /// <returns>성공 시 true, 실패 시 false</returns>
    public static bool AddToAddressableGroup(string assetPath, string groupName, string addressKey, params string[] labels)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableHelper] Settings not found. Please initialize Addressables.");
            return false;
        }

        // 경로 정규화
        string normalizedPath = assetPath.Replace("\\", "/");

        // 에셋 GUID 가져오기
        string assetGuid = AssetDatabase.AssetPathToGUID(normalizedPath);
        if (string.IsNullOrEmpty(assetGuid))
        {
            Debug.LogError($"[AddressableHelper] Failed to get GUID for asset: {normalizedPath}");
            return false;
        }

        // 그룹 찾기 또는 생성
        AddressableAssetGroup targetGroup = settings.FindGroup(groupName);
        if (targetGroup == null)
        {
            Debug.Log($"[AddressableHelper] Group '{groupName}' not found. Creating new group.");
            targetGroup = settings.CreateGroup(groupName, false, false, true, settings.DefaultGroup.Schemas);
        }

        // 엔트리 생성 또는 이동
        var entry = settings.CreateOrMoveEntry(assetGuid, targetGroup, false, false);
        if (entry == null)
        {
            Debug.LogError($"[AddressableHelper] Failed to create entry for asset: {normalizedPath}");
            return false;
        }

        // 주소 설정
        entry.address = addressKey;

        // 레이블 추가
        if (labels != null && labels.Length > 0)
        {
            foreach (string label in labels)
            {
                if (!string.IsNullOrEmpty(label))
                {
                    // 설정에 레이블이 없으면 추가
                    if (!settings.GetLabels().Contains(label))
                    {
                        settings.AddLabel(label);
                    }

                    // 엔트리에 레이블 설정
                    entry.SetLabel(label, true);
                }
            }
        }

        Debug.Log($"[AddressableHelper] Added '{normalizedPath}' to group '{groupName}' with address '{addressKey}'");
        return true;
    }

    /// <summary>
    /// 여러 에셋을 지정된 주소와 레이블로 Addressable 그룹에 추가
    /// </summary>
    /// <param name="assetPaths">추가할 에셋 경로 리스트</param>
    /// <param name="groupName">Addressable 그룹 이름</param>
    /// <param name="addressKeys">주소 키 리스트 (assetPaths와 길이가 같아야 함)</param>
    /// <param name="labels">모든 엔트리에 적용할 레이블 (선택사항)</param>
    /// <returns>성공적으로 추가된 에셋 개수</returns>
    public static int AddMultipleToAddressableGroup(List<string> assetPaths, string groupName, List<string> addressKeys, params string[] labels)
    {
        if (assetPaths == null || addressKeys == null || assetPaths.Count != addressKeys.Count)
        {
            Debug.LogError("[AddressableHelper] Invalid input: assetPaths and addressKeys must be non-null and have the same length");
            return 0;
        }

        int successCount = 0;
        for (int i = 0; i < assetPaths.Count; i++)
        {
            if (AddToAddressableGroup(assetPaths[i], groupName, addressKeys[i], labels))
            {
                successCount++;
            }
        }

        Debug.Log($"[AddressableHelper] Successfully added {successCount}/{assetPaths.Count} assets to group '{groupName}'");
        return successCount;
    }

    /// <summary>
    /// Addressables에서 에셋 제거
    /// </summary>
    /// <param name="assetPath">제거할 에셋의 경로</param>
    /// <returns>성공 시 true, 실패 시 false</returns>
    public static bool RemoveFromAddressables(string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AddressableHelper] Settings not found.");
            return false;
        }

        string normalizedPath = assetPath.Replace("\\", "/");
        string assetGuid = AssetDatabase.AssetPathToGUID(normalizedPath);

        if (string.IsNullOrEmpty(assetGuid))
        {
            Debug.LogError($"[AddressableHelper] Failed to get GUID for asset: {normalizedPath}");
            return false;
        }

        var entry = settings.FindAssetEntry(assetGuid);
        if (entry != null)
        {
            settings.RemoveAssetEntry(assetGuid);
            Debug.Log($"[AddressableHelper] Removed '{normalizedPath}' from Addressables");
            return true;
        }

        Debug.LogWarning($"[AddressableHelper] Asset not found in Addressables: {normalizedPath}");
        return false;
    }

    /// <summary>
    /// 에셋이 이미 Addressables에 포함되어 있는지 확인
    /// </summary>
    /// <param name="assetPath">확인할 에셋의 경로</param>
    /// <returns>Addressables에 포함되어 있으면 true, 아니면 false</returns>
    public static bool IsAssetAddressable(string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return false;

        string normalizedPath = assetPath.Replace("\\", "/");
        string assetGuid = AssetDatabase.AssetPathToGUID(normalizedPath);

        if (string.IsNullOrEmpty(assetGuid))
            return false;

        var entry = settings.FindAssetEntry(assetGuid);
        return entry != null;
    }

    /// <summary>
    /// 에셋의 Addressable 엔트리 가져오기
    /// </summary>
    /// <param name="assetPath">에셋의 경로</param>
    /// <returns>찾으면 AddressableAssetEntry 반환, 없으면 null</returns>
    public static AddressableAssetEntry GetAddressableEntry(string assetPath)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
            return null;

        string normalizedPath = assetPath.Replace("\\", "/");
        string assetGuid = AssetDatabase.AssetPathToGUID(normalizedPath);

        if (string.IsNullOrEmpty(assetGuid))
            return null;

        return settings.FindAssetEntry(assetGuid);
    }
}
#endif