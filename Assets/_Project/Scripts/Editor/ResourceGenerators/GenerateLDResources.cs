#if UNITY_EDITOR
using System.IO;
using CookApps.AutoBattler;
using Cysharp.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

public class GenerateLDResources
{
    private static readonly string BASE_PREFAB_PATH = $"{ResourcePath.LD_PATH}/Template/BasePrefab_UI.prefab";
    private static readonly string BASE_NANINOVEL_PREFAB_PATH = $"{ResourcePath.LD_PATH}/Template/BasePrefab_NaniNovel.prefab";
    private const string LOCK_SUFFIX = "_lock";

    /// <summary>
    /// 폴더가 LD 리소스 생성에서 제외되어야 하는지 확인합니다.
    /// 폴더명 끝에 "_lock" 접미사가 있으면 제외됩니다. (대소문자 구분 없음)
    ///
    /// 사용법: 아트 작업자가 수동 작업이 필요한 폴더의 이름을 변경
    /// 예시: "10101001" → "10101001_lock" (또는 "10101001_Lock", "10101001_LOCK" 등)
    /// </summary>
    private static bool IsExcludedPath(string path)
    {
        string folderName = new DirectoryInfo(path).Name;
        return folderName.EndsWith(LOCK_SUFFIX, System.StringComparison.OrdinalIgnoreCase);
    }

    public static void CreateAllLDResources()
    {
        if (!AssetDatabase.IsValidFolder(ResourcePath.LD_PATH))
        {
            Debug.LogError($"[Fail] LD Characters folder not found: {ResourcePath.LD_PATH}");
            return;
        }

        string[] groupFolderPaths = Directory.GetDirectories(ResourcePath.LD_PATH);

        int totalFolders = groupFolderPaths.Length;
        EditorUtility.DisplayProgressBar("Generating LD Resources", "Generating...", 0f);

        AssetDatabase.StartAssetEditing();
        try
        {
            for (var i = 0; i < groupFolderPaths.Length; i++)
            {
                var groupFolderPath = groupFolderPaths[i];
                string[] subFolderPaths = Directory.GetDirectories(groupFolderPath);
                string groupName = new DirectoryInfo(groupFolderPath).Name;
                EditorUtility.DisplayProgressBar("Generating LD Resources", $"Processing {groupName}...", (float)(i + 1) / totalFolders);

                for (var j = 0; j < subFolderPaths.Length; j++)
                {
                    var subFolderPath = subFolderPaths[j];
                    string folderName = new DirectoryInfo(subFolderPath).Name;

                    // Only process folders with numeric names
                    if (!int.TryParse(folderName, out _))
                        continue;

                    // 제외 경로 체크
                    if (IsExcludedPath(subFolderPath))
                        continue;

                    CreateLDResourceFromPath(subFolderPath);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.ClearProgressBar();
        Debug.Log("[Success] LD Resources generation completed");
    }

    public static void CreateLDResourceFromPath(string idFolderPath)
    {
        string id = new DirectoryInfo(idFolderPath).Name;
        // Create GenerateResources folder
        string generateResourcesPath = Path.Combine(idFolderPath, "GenerateResources");
        if (!AssetDatabase.IsValidFolder(generateResourcesPath))
        {
            AssetDatabase.CreateFolder(idFolderPath, "GenerateResources");
        }

        // Find Spine and Illust resources
        string spineFolderPath = Path.Combine(idFolderPath, "Spine");
        string illustFolderPath = Path.Combine(idFolderPath, "Illust");

        SkeletonDataAsset skeletonDataAsset = FindSkeletonDataAsset(spineFolderPath);
        Sprite illustSprite = FindIllustSprite(illustFolderPath);

        // Check if we have any resources
        if (skeletonDataAsset == null && illustSprite == null)
        {
            Debug.LogWarning($"[Skip] {id} - No Spine or Illust resources found");
            return;
        }

        // Create UI Prefab
        CreateUIPrefab(generateResourcesPath, id, skeletonDataAsset, illustSprite);

        // Create NaniNovel Prefab
        if (illustSprite != null)
        {
            CreateNaniNovelPrefab(generateResourcesPath, id, illustSprite);
        }

        // Add Spine folder to Addressable
        if (AssetDatabase.IsValidFolder(spineFolderPath))
        {
            AddFolderToAddressableGroup(spineFolderPath, id, "Spine");
        }

        // Add Illust folder to Addressable
        if (AssetDatabase.IsValidFolder(illustFolderPath))
        {
            AddFolderToAddressableGroup(illustFolderPath, id, "Illust");
        }
    }

    private static SkeletonDataAsset FindSkeletonDataAsset(string spineFolderPath)
    {
        if (!AssetDatabase.IsValidFolder(spineFolderPath))
            return null;

        // Find all SkeletonDataAsset files in Spine folder
        string[] guids = AssetDatabase.FindAssets("t:SkeletonDataAsset", new[] { spineFolderPath });

        if (guids.Length > 0)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(assetPath);
        }

        return null;
    }

    private static Sprite FindIllustSprite(string illustFolderPath)
    {
        if (!AssetDatabase.IsValidFolder(illustFolderPath))
            return null;

        // Find PNG files in Illust folder
        string[] pngFiles = Directory.GetFiles(illustFolderPath, "*.png");

        if (pngFiles.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(pngFiles[0]);
        }

        return null;
    }

    private static void CreateUIPrefab(string generateResourcesPath, string id, SkeletonDataAsset skeletonDataAsset, Sprite illustSprite)
    {
        // Load base prefab
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_PREFAB_PATH);
        if (basePrefab == null)
        {
            Debug.LogError($"[Fail] BasePrefab_UI not found at {BASE_PREFAB_PATH}");
            return;
        }

        // Instantiate base prefab
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);

        // Find child objects
        Transform imgTransform = prefabInstance.transform.Find("Image");
        Transform skeletonGraphicTransform = prefabInstance.transform.Find("SkeletonGraphic");

        if (skeletonDataAsset != null)
        {
            // Use Spine resource
            if (skeletonGraphicTransform != null)
            {
                SkeletonGraphic skeletonGraphic = skeletonGraphicTransform.GetComponent<SkeletonGraphic>();
                if (skeletonGraphic != null)
                {
                    skeletonGraphic.skeletonDataAsset = skeletonDataAsset;
                    skeletonGraphic.Initialize(true);
                    Debug.Log($"[Success] {id} - Spine resource assigned");
                }
            }

            // Remove Image object
            if (imgTransform != null)
            {
                Object.DestroyImmediate(imgTransform.gameObject);
            }
        }
        else if (illustSprite != null)
        {
            // Use Illust resource
            if (imgTransform != null)
            {
                Image image = imgTransform.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = illustSprite;
                    Debug.Log($"[Success] {id} - Illust resource assigned");
                }
            }

            // Remove SkeletonGraphic object
            if (skeletonGraphicTransform != null)
            {
                Object.DestroyImmediate(skeletonGraphicTransform.gameObject);
            }

            // CharacterIllust에 Image 연결 및 pivot 적용
            CharacterIllust characterIllust = prefabInstance.GetComponent<CharacterIllust>();
            if (characterIllust != null && imgTransform != null)
            {
                Image image = imgTransform.GetComponent<Image>();
                if (image != null)
                {
                    // SerializedObject를 사용하여 private 필드에 접근
                    SerializedObject serializedObject = new SerializedObject(characterIllust);
                    SerializedProperty pivotImageProperty = serializedObject.FindProperty("_pivotReferenceImage");
                    pivotImageProperty.objectReferenceValue = image;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    characterIllust.ApplySpritePivot();
                }
            }
        }

        // Save as prefab
        string prefabPath = Path.Combine(generateResourcesPath, $"UI_{id}.prefab");
        prefabPath = prefabPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);

        // Cleanup
        Object.DestroyImmediate(prefabInstance);

        Debug.Log($"[Success] {id} - UI Prefab created at {prefabPath}");

        // Add to Addressable
        AddToAddressableGroup(prefabPath);
    }

    private static void CreateNaniNovelPrefab(string generateResourcesPath, string id, Sprite illustSprite)
    {
        // Load base prefab
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASE_NANINOVEL_PREFAB_PATH);
        if (basePrefab == null)
        {
            Debug.LogError($"[Fail] BasePrefab_NaniNovel not found at {BASE_NANINOVEL_PREFAB_PATH}");
            return;
        }

        // Instantiate base prefab
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);

        // Get SpriteRenderer from root object
        SpriteRenderer spriteRenderer = prefabInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = illustSprite;
            Debug.Log($"[Success] {id} - NaniNovel Illust resource assigned");
        }
        else
        {
            Debug.LogError($"[Fail] {id} - SpriteRenderer not found on BasePrefab_NaniNovel");
            Object.DestroyImmediate(prefabInstance);
            return;
        }

        // Save as prefab
        string prefabPath = Path.Combine(generateResourcesPath, $"Naninovel_{id}.prefab");
        prefabPath = prefabPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);

        // Cleanup
        Object.DestroyImmediate(prefabInstance);

        Debug.Log($"[Success] {id} - NaniNovel Prefab created at {prefabPath}");

        // Add to Addressable with naninovel label
        AddNaniNovelToAddressableGroup(prefabPath);
    }

    private static void AddToAddressableGroup(string prefabPath)
    {
        // 경로에서 addressable_group과 address_key 추출
        // prefabPath: Assets/_Project/Addressables/Remote/LD/{addressable_group}/{address_key}/GenerateResources/UI_{id}.prefab
        string normalizedPath = prefabPath.Replace("\\", "/");
        string[] pathParts = normalizedPath.Split('/');

        if (pathParts.Length < 4)
        {
            Debug.LogError("[Addressable] Invalid folder structure. Path is too short.");
            return;
        }

        string groupName = ZString.Format("LD_{0}", pathParts[^4]);
        string addressKey = pathParts[^3];
        string address = ZString.Format("{0}/UI_{1}", groupName, addressKey);

        AddressableImportHelper.AddToAddressableGroup(normalizedPath, groupName, address);
    }

    private static void AddFolderToAddressableGroup(string folderPath, string id, string folderType)
    {
        // 경로에서 addressable_group 추출
        // folderPath: Assets/_Project/Addressables/Remote/LD/{addressable_group}/{id}/{folderType}
        string normalizedPath = folderPath.Replace("\\", "/");
        string[] pathParts = normalizedPath.Split('/');

        if (pathParts.Length < 3)
        {
            Debug.LogError($"[Addressable] Invalid folder structure for {folderType}. Path is too short.");
            return;
        }

        string groupName = ZString.Format("LD_{0}", pathParts[^3]);
        string address = ZString.Format("{0}/{1}/{2}", groupName, id, folderType);

        AddressableImportHelper.AddToAddressableGroup(normalizedPath, groupName, address);
    }

    private static void AddNaniNovelToAddressableGroup(string prefabPath)
    {
        // 경로에서 addressable_group과 address_key 추출
        // prefabPath: Assets/_Project/Addressables/Remote/LD/{addressable_group}/{address_key}/GenerateResources/{id}_NaniNovel.prefab
        string normalizedPath = prefabPath.Replace("\\", "/");
        string[] pathParts = normalizedPath.Split('/');

        if (pathParts.Length < 4)
        {
            Debug.LogError("[Addressable] Invalid folder structure for NaniNovel. Path is too short.");
            return;
        }

        string groupName = "Naninovel_Character";
        string addressKey = pathParts[^3];
        string address = ZString.Format("Naninovel/Characters/{0}_{1}", pathParts[^4], addressKey);

        AddressableImportHelper.AddToAddressableGroup(normalizedPath, groupName, address, "Naninovel");
    }
}
#endif
