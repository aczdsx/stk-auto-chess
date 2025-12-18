#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Naninovel;
using System.IO;

/// <summary>
/// Fixes CameraConfiguration to use non-Addressable prefab references.
/// Naninovel's Engine.Instantiate doesn't support Addressable prefabs directly.
/// </summary>
public static class FixCameraConfigurationAddressables
{
    [MenuItem("Tools/Naninovel/Fix CameraConfiguration Addressables Issue")]
    public static void FixCameraConfiguration()
    {
        Debug.Log("=== Fixing CameraConfiguration Addressables Issue ===");
        
        // Load CameraConfiguration
        var configPath = "Assets/NaninovelData/Resources/Naninovel/Configuration/CameraConfiguration.asset";
        var config = AssetDatabase.LoadAssetAtPath<CameraConfiguration>(configPath);
        
        if (config == null)
        {
            Debug.LogError($"CameraConfiguration not found at: {configPath}");
            return;
        }
        
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found!");
            return;
        }
        
        // Check MainCamera
        if (config.CustomCameraPrefab != null)
        {
            var mainCameraPath = AssetDatabase.GetAssetPath(config.CustomCameraPrefab);
            var mainCameraGuid = AssetDatabase.AssetPathToGUID(mainCameraPath);
            var mainCameraEntry = settings.FindAssetEntry(mainCameraGuid);
            
            if (mainCameraEntry != null)
            {
                Debug.LogWarning($"⚠ CustomCameraPrefab is an Addressable asset!");
                Debug.LogWarning($"  Address: {mainCameraEntry.address}");
                Debug.LogWarning($"  Path: {mainCameraPath}");
                Debug.LogWarning($"\n  SOLUTION: Remove this prefab from Addressables or create a non-Addressable copy.");
                Debug.LogWarning($"  To fix:");
                Debug.LogWarning($"  1. Select the prefab: {mainCameraPath}");
                Debug.LogWarning($"  2. In Addressables window, remove it from the group");
                Debug.LogWarning($"  3. Or create a copy outside Addressables folder");
            }
            else
            {
                Debug.Log($"✓ CustomCameraPrefab is NOT an Addressable (OK)");
            }
        }
        else
        {
            Debug.LogWarning("⚠ CustomCameraPrefab is NULL");
        }
        
        // Check UICamera
        if (config.CustomUICameraPrefab != null)
        {
            var uiCameraPath = AssetDatabase.GetAssetPath(config.CustomUICameraPrefab);
            var uiCameraGuid = AssetDatabase.AssetPathToGUID(uiCameraPath);
            var uiCameraEntry = settings.FindAssetEntry(uiCameraGuid);
            
            if (uiCameraEntry != null)
            {
                Debug.LogWarning($"⚠ CustomUICameraPrefab is an Addressable asset!");
                Debug.LogWarning($"  Address: {uiCameraEntry.address}");
                Debug.LogWarning($"  Path: {uiCameraPath}");
                Debug.LogWarning($"\n  SOLUTION: Remove this prefab from Addressables or create a non-Addressable copy.");
                Debug.LogWarning($"  To fix:");
                Debug.LogWarning($"  1. Select the prefab: {uiCameraPath}");
                Debug.LogWarning($"  2. In Addressables window, remove it from the group");
                Debug.LogWarning($"  3. Or create a copy outside Addressables folder");
            }
            else
            {
                Debug.Log($"✓ CustomUICameraPrefab is NOT an Addressable (OK)");
            }
        }
        else
        {
            Debug.LogWarning("⚠ CustomUICameraPrefab is NULL");
        }
        
        Debug.Log("\n=== Summary ===");
        Debug.Log("If prefabs are Addressables, they need to be removed from Addressables");
        Debug.Log("or copied to a non-Addressables location for Naninovel to use them.");
    }
    
    [MenuItem("Tools/Naninovel/Create Non-Addressable Camera Prefabs")]
    public static void CreateNonAddressableCopies()
    {
        Debug.Log("=== Creating Non-Addressable Camera Prefabs ===");
        
        var configPath = "Assets/NaninovelData/Resources/Naninovel/Configuration/CameraConfiguration.asset";
        var config = AssetDatabase.LoadAssetAtPath<CameraConfiguration>(configPath);
        
        if (config == null)
        {
            Debug.LogError($"CameraConfiguration not found at: {configPath}");
            return;
        }
        
        // Create directory for non-Addressable camera prefabs
        var targetDir = "Assets/NaninovelData/Resources/Naninovel/Camera";
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
            AssetDatabase.Refresh();
        }
        
        bool needsUpdate = false;
        
        // Copy MainCamera if it's an Addressable
        if (config.CustomCameraPrefab != null)
        {
            var sourcePath = AssetDatabase.GetAssetPath(config.CustomCameraPrefab);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var guid = AssetDatabase.AssetPathToGUID(sourcePath);
            var entry = settings?.FindAssetEntry(guid);
            
            if (entry != null)
            {
                var targetPath = Path.Combine(targetDir, "MainCamera.prefab");
                if (!File.Exists(targetPath))
                {
                    AssetDatabase.CopyAsset(sourcePath, targetPath);
                    AssetDatabase.Refresh();
                    
                    var newPrefab = AssetDatabase.LoadAssetAtPath<Camera>(targetPath);
                    if (newPrefab != null)
                    {
                        config.CustomCameraPrefab = newPrefab;
                        EditorUtility.SetDirty(config);
                        Debug.Log($"✓ Created non-Addressable copy: {targetPath}");
                        Debug.Log($"  Updated CameraConfiguration.CustomCameraPrefab");
                        needsUpdate = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"Target already exists: {targetPath}");
                    var existingPrefab = AssetDatabase.LoadAssetAtPath<Camera>(targetPath);
                    if (existingPrefab != null && config.CustomCameraPrefab != existingPrefab)
                    {
                        config.CustomCameraPrefab = existingPrefab;
                        EditorUtility.SetDirty(config);
                        Debug.Log($"✓ Updated CameraConfiguration to use existing non-Addressable prefab");
                        needsUpdate = true;
                    }
                }
            }
        }
        
        // Copy UICamera if it's an Addressable
        if (config.CustomUICameraPrefab != null)
        {
            var sourcePath = AssetDatabase.GetAssetPath(config.CustomUICameraPrefab);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var guid = AssetDatabase.AssetPathToGUID(sourcePath);
            var entry = settings?.FindAssetEntry(guid);
            
            if (entry != null)
            {
                var targetPath = Path.Combine(targetDir, "UICamera.prefab");
                if (!File.Exists(targetPath))
                {
                    AssetDatabase.CopyAsset(sourcePath, targetPath);
                    AssetDatabase.Refresh();
                    
                    var newPrefab = AssetDatabase.LoadAssetAtPath<Camera>(targetPath);
                    if (newPrefab != null)
                    {
                        config.CustomUICameraPrefab = newPrefab;
                        EditorUtility.SetDirty(config);
                        Debug.Log($"✓ Created non-Addressable copy: {targetPath}");
                        Debug.Log($"  Updated CameraConfiguration.CustomUICameraPrefab");
                        needsUpdate = true;
                    }
                }
                else
                {
                    Debug.LogWarning($"Target already exists: {targetPath}");
                    var existingPrefab = AssetDatabase.LoadAssetAtPath<Camera>(targetPath);
                    if (existingPrefab != null && config.CustomUICameraPrefab != existingPrefab)
                    {
                        config.CustomUICameraPrefab = existingPrefab;
                        EditorUtility.SetDirty(config);
                        Debug.Log($"✓ Updated CameraConfiguration to use existing non-Addressable prefab");
                        needsUpdate = true;
                    }
                }
            }
        }
        
        if (needsUpdate)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("\n✓ CameraConfiguration updated! Please verify the settings.");
        }
        else
        {
            Debug.Log("\n✓ No changes needed - camera prefabs are already non-Addressable.");
        }
    }
}
#endif

