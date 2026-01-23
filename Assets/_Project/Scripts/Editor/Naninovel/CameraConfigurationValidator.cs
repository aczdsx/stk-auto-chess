#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Naninovel;
using System.Linq;

public static class CameraConfigurationValidator
{
    [MenuItem("Tools/Naninovel/Validate CameraConfiguration")]
    public static void ValidateCameraConfiguration()
    {
        Debug.Log("=== CameraConfiguration Validation ===");
        
        // Load CameraConfiguration asset
        var configPath = "Assets/NaninovelData/Resources/Naninovel/Configuration/CameraConfiguration.asset";
        var config = AssetDatabase.LoadAssetAtPath<CameraConfiguration>(configPath);
        
        if (config == null)
        {
            Debug.LogError($"❌ CameraConfiguration not found at: {configPath}");
            return;
        }
        
        Debug.Log($"✓ CameraConfiguration loaded from: {configPath}");
        
        // Check CustomCameraPrefab
        bool mainCameraValid = ValidatePrefab(
            config.CustomCameraPrefab,
            "CustomCameraPrefab",
            "MainCamera"
        );
        
        // Check CustomUICameraPrefab
        bool uiCameraValid = ValidatePrefab(
            config.CustomUICameraPrefab,
            "CustomUICameraPrefab",
            "UICamera"
        );
        
        // Check UseUICamera setting
        if (config.UseUICamera && !uiCameraValid)
        {
            Debug.LogWarning("⚠ UseUICamera is enabled but CustomUICameraPrefab is invalid!");
        }
        
        // Summary
        Debug.Log("=== Validation Summary ===");
        if (mainCameraValid && uiCameraValid)
        {
            Debug.Log("✓ All camera prefabs are valid!");
        }
        else
        {
            Debug.LogError("❌ Some camera prefabs have issues. Check details above.");
        }
    }
    
    private static bool ValidatePrefab(Camera prefab, string fieldName, string expectedName)
    {
        Debug.Log($"\n--- Checking {fieldName} ---");
        
        if (prefab == null)
        {
            Debug.LogError($"❌ {fieldName} is NULL!");
            return false;
        }
        
        Debug.Log($"✓ {fieldName} reference exists: {prefab.name}");
        
        // Check if prefab is a valid Camera component
        if (!prefab)
        {
            Debug.LogError($"❌ {fieldName} is not a valid Camera component!");
            return false;
        }
        
        // Get the asset path
        var prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError($"❌ {fieldName} asset path not found!");
            return false;
        }
        
        Debug.Log($"✓ Asset path: {prefabPath}");
        
        // Check if it's in Addressables
        #if ADDRESSABLES_AVAILABLE
        var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(prefabPath));
            if (entry != null)
            {
                Debug.LogWarning($"⚠ {fieldName} is an Addressable asset!");
                Debug.LogWarning($"  Address: {entry.address}");
                Debug.LogWarning($"  Group: {entry.parentGroup?.Name ?? "Unknown"}");
                Debug.LogWarning($"  Labels: [{string.Join(", ", entry.labels)}]");
                
                // Check if it's properly labeled for Naninovel
                if (!entry.labels.Contains("Naninovel"))
                {
                    Debug.LogWarning($"  ⚠ Missing 'Naninovel' label - this might cause loading issues!");
                }
            }
            else
            {
                Debug.Log($"✓ {fieldName} is NOT an Addressable (direct reference)");
            }
        }
        #endif
        
        // Check camera settings
        Debug.Log($"  Camera Settings:");
        Debug.Log($"    - Orthographic: {prefab.orthographic}");
        Debug.Log($"    - Depth: {prefab.depth}");
        Debug.Log($"    - Clear Flags: {prefab.clearFlags}");
        Debug.Log($"    - Culling Mask: {prefab.cullingMask}");
        
        // Validate camera name matches expected
        if (!prefab.name.Contains(expectedName))
        {
            Debug.LogWarning($"  ⚠ Camera name '{prefab.name}' doesn't match expected '{expectedName}'");
        }
        
        return true;
    }
    
    [MenuItem("Tools/Naninovel/Find Camera Prefabs")]
    public static void FindCameraPrefabs()
    {
        Debug.Log("=== Searching for Camera Prefabs ===");
        
        // Search for MainCamera prefab
        var mainCameraGuids = AssetDatabase.FindAssets("MainCamera t:Prefab");
        Debug.Log($"\nFound {mainCameraGuids.Length} MainCamera prefab(s):");
        foreach (var guid in mainCameraGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var camera = prefab?.GetComponent<Camera>();
            Debug.Log($"  - {path}");
            Debug.Log($"    GUID: {guid}");
            Debug.Log($"    Has Camera: {camera != null}");
            if (camera != null)
            {
                Debug.Log($"    Orthographic: {camera.orthographic}, Depth: {camera.depth}");
            }
        }
        
        // Search for UICamera prefab
        var uiCameraGuids = AssetDatabase.FindAssets("UICamera t:Prefab");
        Debug.Log($"\nFound {uiCameraGuids.Length} UICamera prefab(s):");
        foreach (var guid in uiCameraGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var camera = prefab?.GetComponent<Camera>();
            Debug.Log($"  - {path}");
            Debug.Log($"    GUID: {guid}");
            Debug.Log($"    Has Camera: {camera != null}");
            if (camera != null)
            {
                Debug.Log($"    Orthographic: {camera.orthographic}, Depth: {camera.depth}");
            }
        }
    }
}
#endif

