using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class PrefabAddressableSync
{
    [MenuItem("Tools/Addressable/Sync Prefab Name to Addressable Name")]
    public static void SyncPrefabNameToAddressable()
    {
        // 폴더 선택
        string folderPath = EditorUtility.OpenFolderPanel("프리팹 폴더 선택", "Assets", "");
        
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("폴더가 선택되지 않았습니다.");
            return;
        }
        
        // Assets 경로로 변환
        if (!folderPath.Contains("Assets"))
        {
            Debug.LogError("Assets 폴더 내의 경로를 선택해주세요.");
            return;
        }
        
        string assetPath = folderPath.Substring(folderPath.IndexOf("Assets"));
        
        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            Debug.LogError($"유효하지 않은 폴더 경로입니다: {assetPath}");
            return;
        }
        
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (addressableSettings == null)
        {
            Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
            return;
        }
        
        // 프리팹 파일 찾기
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });
        
        int changedCount = 0;
        int checkedCount = 0;
        
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
                continue;
            
            checkedCount++;
            
            // 프리팹 이름 (확장자 제외)
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            
            // Addressable Entry 찾기
            AddressableAssetEntry entry = addressableSettings.FindAssetEntry(guid);
            
            if (entry == null)
            {
                Debug.Log($"Addressable에 등록되지 않음: {prefabName} ({prefabPath})");
                continue;
            }
            
            // 현재 Addressable 이름
            string currentAddress = entry.address;
            
            // 이름이 다른 경우 변경
            if (currentAddress != prefabName)
            {
                entry.SetAddress(prefabName, false);
                changedCount++;
                
                Debug.Log($"변경됨: {prefabPath}\n  이전: {currentAddress}\n  변경: {prefabName}");
            }
        }
        
        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"완료: {checkedCount}개 프리팹 확인, {changedCount}개 Addressable 이름 변경됨");
        }
        else
        {
            Debug.Log($"완료: {checkedCount}개 프리팹 확인, 변경된 항목 없음");
        }
    }
    
    [MenuItem("Tools/Addressable/Sync Prefab Name to Addressable Name (Custom Path)")]
    public static void SyncPrefabNameToAddressableCustomPath()
    {
        // 기본 경로 설정 (필요에 따라 수정)
        string defaultPath = "Assets/_Project/Prefabs";
        
        string folderPath = EditorUtility.OpenFolderPanel("프리팹 폴더 선택", defaultPath, "");
        
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("폴더가 선택되지 않았습니다.");
            return;
        }
        
        // Assets 경로로 변환
        if (!folderPath.Contains("Assets"))
        {
            Debug.LogError("Assets 폴더 내의 경로를 선택해주세요.");
            return;
        }
        
        string assetPath = folderPath.Substring(folderPath.IndexOf("Assets"));
        
        if (!AssetDatabase.IsValidFolder(assetPath))
        {
            Debug.LogError($"유효하지 않은 폴더 경로입니다: {assetPath}");
            return;
        }
        
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (addressableSettings == null)
        {
            Debug.LogError("Addressable Settings를 찾을 수 없습니다.");
            return;
        }
        
        // 프리팹 파일 찾기 (하위 폴더 포함)
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });
        
        int changedCount = 0;
        int checkedCount = 0;
        int notRegisteredCount = 0;
        
        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefab == null)
                continue;
            
            checkedCount++;
            
            // 프리팹 이름 (확장자 제외)
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
            
            // Addressable Entry 찾기
            AddressableAssetEntry entry = addressableSettings.FindAssetEntry(guid);
            
            if (entry == null)
            {
                notRegisteredCount++;
                continue;
            }
            
            // 현재 Addressable 이름
            string currentAddress = entry.address;
            
            // 이름이 다른 경우 변경
            if (currentAddress != prefabName)
            {
                entry.SetAddress(prefabName, false);
                changedCount++;
                
                Debug.Log($"변경됨: {prefabPath}\n  이전: {currentAddress}\n  변경: {prefabName}");
            }
        }
        
        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
        }
        
        Debug.Log($"완료:\n  확인: {checkedCount}개\n  변경: {changedCount}개\n  미등록: {notRegisteredCount}개");
    }
}

