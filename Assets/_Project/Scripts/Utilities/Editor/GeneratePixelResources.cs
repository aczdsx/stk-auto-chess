#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookApps.BattleSystem;
using Cysharp.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class GeneratePixelResources : Editor
{
    private const int DEAD_FRAME_COUNT = 10;

    [MenuItem("Tools/Pixel Resources/Generate Pixel Resources For All Subfolders")]
    private static void CreateAnimationsForAllSubfolders()
    {
        CreateAnimationsForAllSubfolders(false);
    }
    
    [MenuItem("Tools/Pixel Resources/Force Generate Pixel Resources For All Subfolders")]
    public static void ForceCreateAnimationsForAllSubfolders()
    {
        CreateAnimationsForAllSubfolders(true);
    }

    private static void CreateAnimationsForAllSubfolders(bool isForce)
    {
        string[] groupFolderPaths = Directory.GetDirectories(ResourcePath.SD_PATH);

        // show progress bar
        int totalFolders = groupFolderPaths.Length;
        EditorUtility.DisplayProgressBar("Generating Pixel Resources", "Generating...", 0f);
        for (var i = 0; i < groupFolderPaths.Length; i++)
        {
            var groupFolderPath = groupFolderPaths[i];
            var subFolders = Directory.GetDirectories(groupFolderPath);
            foreach (var subFolder in subFolders)
            {
                string folderName = new DirectoryInfo(subFolder).Name;
                if (int.TryParse(folderName, out _))
                {
                    string generateResourcesPath = Path.Combine(subFolder, "GenerateResources");
                    if (AssetDatabase.IsValidFolder(generateResourcesPath))
                    {
                        if (!isForce)
                            continue;
                        
                        AssetDatabase.DeleteAsset(generateResourcesPath);
                    }

                    CreateAnimationsFromPath(subFolder);
                }
            }
            
            EditorUtility.DisplayProgressBar("Generating Pixel Resources", "Generating...", (float)(i + 1) / totalFolders);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.ClearProgressBar();
    }

    [MenuItem("Assets/Generate Pixel Resources", true)]
    private static bool ValidateCreateAnimation()
    {
        if (Selection.objects.Length == 0)
            return false;

        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (!AssetDatabase.IsValidFolder(path))
                return false;
        }

        return true;
    }


    [MenuItem("Assets/Generate Pixel Resources")]
    private static void CreateAnimations()
    {
        foreach (var selectedObject in Selection.objects)
        {
            string parentFolderPath = AssetDatabase.GetAssetPath(selectedObject);

            if (!AssetDatabase.IsValidFolder(parentFolderPath))
                continue;

            string generateResourcesPath = Path.Combine(parentFolderPath, "GenerateResources");

            if (AssetDatabase.IsValidFolder(generateResourcesPath))
            {
                AssetDatabase.DeleteAsset(generateResourcesPath);
            }

            CreateAnimationsFromPath(parentFolderPath);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateAnimationsFromPath(string parentFolderPath)
    {
        string parentFolderName = new DirectoryInfo(parentFolderPath).Name;
        string generateResourcesPath = Path.Combine(parentFolderPath, "GenerateResources");

        if (!AssetDatabase.IsValidFolder(generateResourcesPath))
        {
            AssetDatabase.CreateFolder(parentFolderPath, "GenerateResources");
        }

        string[] subFolderPaths = Directory.GetDirectories(parentFolderPath);
        List<Sprite> allSprites = new List<Sprite>();
        Sprite viewSprite = null;

        foreach (string subFolderPath in subFolderPaths)
        {
            string[] subSubFolderPaths = Directory.GetDirectories(subFolderPath);
            foreach (string subSubFolderPath in subSubFolderPaths)
            {
                List<Sprite> sprite = LoadSpritesFromFolder(subSubFolderPath);
                allSprites.AddRange(sprite);
                GenerateExtraFiles(subSubFolderPath, generateResourcesPath, parentFolderName,
                    new DirectoryInfo(subFolderPath).Name);

                if (subSubFolderPath.EndsWith(Path.Combine("Front", "IDLE")) && sprite.Count > 0) {
                    viewSprite = sprite[0];
                }
            }
        }

        CreateSingleSpriteAtlas(generateResourcesPath, allSprites, parentFolderName);

        var overrideController = AddAnimationsToBaseController(generateResourcesPath, parentFolderName);

        AddInGameCharacterPrefab(generateResourcesPath, parentFolderName, overrideController, viewSprite);
        AddUICharacterPrefab(generateResourcesPath, parentFolderName, viewSprite);

        AddToAddressableGroup(generateResourcesPath);

        Debug.Log("[Success]");
    }

    private static void AddToAddressableGroup(string folderPath)
    {
        // 경로에서 addressable_group과 address_key 추출
        // folderPath: Assets/_Project/Addressables/Remote/SD/{addressable_group}/{address_key}/GenerateResources
        string normalizedPath = folderPath.Replace("\\", "/");
        string[] pathParts = normalizedPath.Split('/');

        if (pathParts.Length < 3)
        {
            Debug.LogError("[Addressable] Invalid folder structure. Path is too short.");
            return;
        }

        string groupName = ZString.Format("SD_{0}", pathParts[^3]);
        string addressKey = pathParts[^2];
        string address = ZString.Format("{0}/{1}", groupName, addressKey);

        AddressableImportHelper.AddToAddressableGroup(normalizedPath, groupName, address);
    }

    private static List<Sprite> LoadSpritesFromFolder(string folderPath)
    {
        string[] filePaths = Directory.GetFiles(folderPath, "*.png");

        // 파일명을 숫자로 변환하는 함수
        int ExtractNumberFromFileName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string numberStr = new string(fileName.Where(char.IsDigit).ToArray());
            return int.TryParse(numberStr, out int number) ? number : int.MaxValue;
        }

        // 파일 경로를 숫자로 정렬
        var sortedFilePaths = filePaths.OrderBy(ExtractNumberFromFileName).ToList();

        List<Sprite> sprites = sortedFilePaths
            .Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
            .ToList();

        return sprites;
    }



    private static void GenerateExtraFiles(string folderPath, string animationFolderPath,
        string parentFolderName, string subFolderName)
    {
        string folderName = new DirectoryInfo(folderPath).Name;
        List<Sprite> sprites = LoadSpritesFromFolder(folderPath);

        if (sprites.Count == 0)
        {
            Debug.Log("[Fail] No Sprite");
            return;
        }

        // 애니메이션 클립 생성
        AnimationClip animationClip = new AnimationClip();
        animationClip.frameRate = sprites.Count > 12 ? sprites.Count : 12f;
        if (folderName == "IDLE" || folderName == "MOVE")
        {
            animationClip.wrapMode = WrapMode.Loop;
            var clipSettings = AnimationUtility.GetAnimationClipSettings(animationClip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(animationClip, clipSettings);
        }
        else
            animationClip.wrapMode = WrapMode.Once;

        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(SpriteRenderer);
        curveBinding.path = "";
        curveBinding.propertyName = "m_Sprite";

        int frameCount = folderName == "DEAD" ? sprites.Count * DEAD_FRAME_COUNT : sprites.Count;
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i / animationClip.frameRate;
            keyFrames[i].value = sprites[i / (folderName == "DEAD" ? DEAD_FRAME_COUNT : 1)];
        }

        AnimationUtility.SetObjectReferenceCurve(animationClip, curveBinding, keyFrames);

        for (int i = 0; i < sprites.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i / animationClip.frameRate;
            keyFrames[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(animationClip, curveBinding, keyFrames);

        List<AnimationEvent> animationEvents = new List<AnimationEvent>();

        AnimationEvent startEvent = new AnimationEvent();
        startEvent.functionName = "InvokeAnimationEvent";
        startEvent.time = 0f;
        startEvent.intParameter = (int)AnimationEventKey.Start;
        animationEvents.Add(startEvent);

        AnimationEvent endEvent = new AnimationEvent();
        endEvent.functionName = "InvokeAnimationEvent";
        endEvent.time = keyFrames.Length / animationClip.frameRate;
        endEvent.intParameter = (int)AnimationEventKey.End;
        animationEvents.Add(endEvent);

        if (folderName == "ATK")
        {
            AnimationEvent attackEvent = new AnimationEvent();
            attackEvent.functionName = "InvokeAnimationEvent";
            attackEvent.intParameter = (int)AnimationEventKey.Execute1Per1;
            attackEvent.time = 0.3f;
            animationEvents.Add(attackEvent);
        }

        if (folderName == "SKL")
        {
            AnimationEvent attackEvent = new AnimationEvent();
            attackEvent.functionName = "InvokeAnimationEvent";
            attackEvent.intParameter = (int)AnimationEventKey.Execute1Per1;
            attackEvent.time = 0.5f;
            animationEvents.Add(attackEvent);
        }

        // 애니메이션 이벤트를 클립에 설정
        AnimationUtility.SetAnimationEvents(animationClip, animationEvents.ToArray());

        // 애니메이션 클립 저장
        string savePath = Path.Combine(animationFolderPath, $"{subFolderName}_{folderName}.anim");
        savePath = savePath.Replace("\\", "/");

        AssetDatabase.CreateAsset(animationClip, savePath);
    }

    private static AnimatorOverrideController AddAnimationsToBaseController(string parentFolderPath, string parentFolderName)
    {
        string[] animationClipPaths = Directory.GetFiles(parentFolderPath, "*.anim");
        AnimationClip[] animationClips = animationClipPaths
            .Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path)).ToArray();

        // Check if animation clips are loaded properly
        if (animationClips == null || animationClips.Length == 0)
        {
            Debug.LogError("[Fail] Animation Clips are not loaded properly");
            return null;
        }

        string overrideControllerPath = Path.Combine(parentFolderPath, $"{parentFolderName}_AnimationController.controller");
        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

        string baseControllerName = "BaseCharacterAnimController";
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController " + baseControllerName, new []{ ResourcePath.SD_PATH });
        if (guids.Length == 0)
        {
            Debug.LogError("[Fail] Base Anim Controller not found");
            return null;
        }

        string baseControllerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);

        if (baseController == null)
        {
            Debug.LogError("[Fail] Base Anim Controller not loaded");
            return null;
        }

        overrideController.runtimeAnimatorController = baseController;

        foreach (AnimationClip clip in animationClips)
        {
            if (overrideController[clip.name] != null)
            {
                overrideController[clip.name] = clip;
            }
            else
            {
                Debug.LogError($"[Fail] Clip {clip.name} not added to the overrideController");
            }
        }

        Debug.Log("[Success] AddAnimationsToBaseController");
        return overrideController;
    }

    private static void AddInGameCharacterPrefab(string parentFolderPath, string parentFolderName, AnimatorOverrideController controller, Sprite sprite)
    {
        string basePrefabName = "BasePrefab_InGame";
        string[] guids = AssetDatabase.FindAssets($"t:Prefab {basePrefabName}", new [] { ResourcePath.SD_PATH });
        if (guids.Length == 0)
        {
            Debug.Log("[Fail] Find BasePrefab_InGame");
            return;
        }

        string basePrefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        GameObject objSource = (GameObject)PrefabUtility.InstantiatePrefab(source);

        Transform childTransform = objSource.transform.GetChild(0).GetChild(0).GetChild(0);

        SpriteRenderer spriteRenderer = childTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
        else
        {
            Debug.Log("[Fail] SpriteRenderer");
            return;
        }

        Animator animator = childTransform.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
        }
        else
        {
            Debug.Log("[Fail] Animator");
            return;
        }

        string prefabFullPath = Path.Combine(parentFolderPath, $"InGame_{parentFolderName}.prefab");
        prefabFullPath = prefabFullPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(objSource, prefabFullPath);

        DestroyImmediate(objSource);

        Debug.Log("[Success] InGame Prefab Created");
    }

    private static void AddUICharacterPrefab(string parentFolderPath, string parentFolderName, Sprite sprite)
    {
        string basePrefabName = "BasePrefab_UI";
        string[] guids = AssetDatabase.FindAssets($"t:Prefab {basePrefabName}", new [] { ResourcePath.SD_PATH });
        if (guids.Length == 0)
        {
            Debug.Log("[Fail] Find BasePrefab_UI");
            return;
        }

        string basePrefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        GameObject objSource = (GameObject)PrefabUtility.InstantiatePrefab(source);

        Transform image = objSource.transform.Find("img");
        image.GetComponent<Image>().sprite = sprite;

        string prefabFullPath = Path.Combine(parentFolderPath, $"UI_{parentFolderName}.prefab");
        prefabFullPath = prefabFullPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(objSource, prefabFullPath);

        DestroyImmediate(objSource);

        Debug.Log("[Success] UI Prefab Created");
    }

    private static void CreateSingleSpriteAtlas(string parentFolderPath, List<Sprite> allSprites,
        string parentFolderName)
    {
        if (allSprites.Count == 0)
        {
            Debug.Log("[Fail] Sprite Atlas");
            return;
        }

        SpriteAtlas spriteAtlas = new SpriteAtlas();

        spriteAtlas.SetIncludeInBuild(true);
        spriteAtlas.SetIsVariant(false);

        // Packing settings
        SpriteAtlasPackingSettings packingSettings = spriteAtlas.GetPackingSettings();
        packingSettings.enableRotation = false;
        packingSettings.enableTightPacking = false;
        packingSettings.padding = 2;
        spriteAtlas.SetPackingSettings(packingSettings);

        SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings
        {
            sRGB = true,
            generateMipMaps = false,
            readable = false,
            filterMode = FilterMode.Point
        };
        spriteAtlas.SetTextureSettings(textureSettings);

        spriteAtlas.Add(allSprites.ToArray());

        TextureImporterPlatformSettings defaultSettings = new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            overridden = true,
            maxTextureSize = 2048,
            format = TextureImporterFormat.Automatic,
            textureCompression = TextureImporterCompression.CompressedHQ,
            crunchedCompression = false
        };
        spriteAtlas.SetPlatformSettings(defaultSettings);

        string savePath = Path.Combine(parentFolderPath, $"{parentFolderName}.spriteatlas");
        savePath = savePath.Replace("\\", "/");
        AssetDatabase.CreateAsset(spriteAtlas, savePath);

        // Packing atlases
        // SpriteAtlasUtility.PackAtlases(new[] {spriteAtlas}, EditorUserBuildSettings.activeBuildTarget);

        Debug.Log("[Success] Sprite Atlas Created");
    }
}
#endif