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
}
#endif