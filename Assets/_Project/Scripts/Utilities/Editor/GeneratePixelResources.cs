using System.Collections.Generic;
using System.IO;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Rendering;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class GeneratePixelResources : Editor
{
    private const int DEAD_FRAME_COUNT = 10;
    [MenuItem("Assets/Generate Pixel Resources", true)]
    private static bool ValidateCreateAnimation()
    {
        // 메뉴가 활성화될 조건: 선택한 것이 폴더여야 함
        return Selection.activeObject != null &&
               AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Assets/Generate Pixel Resources")]
    private static void CreateAnimations()
    {
        // 선택된 상위 폴더의 경로
        string parentFolderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string parentFolderName = new DirectoryInfo(parentFolderPath).Name;
        string animationFolderPath = Path.Combine(parentFolderPath, "GenerateResources");

        // Animation 폴더 생성
        if (!AssetDatabase.IsValidFolder(animationFolderPath))
        {
            AssetDatabase.CreateFolder(parentFolderPath, "GenerateResources");
        }

        // 상위 폴더 내의 모든 하위 폴더 검색 (예: Front, Back)
        string[] subFolderPaths = Directory.GetDirectories(parentFolderPath);
        List<Sprite> allSprites = new List<Sprite>();
        Sprite viewSprite = null;

        foreach (string subFolderPath in subFolderPaths)
        {
            // 각 하위 폴더 내의 모든 폴더 검색 (예: ATK, DEAD, IDLE)
            string[] subSubFolderPaths = Directory.GetDirectories(subFolderPath);
            foreach (string subSubFolderPath in subSubFolderPaths)
            {
                List<Sprite> sprite = LoadSpritesFromFolder(subSubFolderPath);
                allSprites.AddRange(sprite);
                GenerateExtraFiles(subSubFolderPath, animationFolderPath, parentFolderName,
                    new DirectoryInfo(subFolderPath).Name);

                if (subSubFolderPath.EndsWith("Front\\IDLE")) {
                    viewSprite = sprite[0];
                }
            }
        }

        // 상위 폴더에 단일 스프라이트 아틀라스 생성
        CreateSingleSpriteAtlas(animationFolderPath, allSprites, parentFolderName);

        // 애니메이션 컨트롤러에 애니메이션 클립 추가
        var overrideController = AddAnimationsToBaseController(animationFolderPath, parentFolderName);

        // 프리팹 생성
        AddCharacterPrefab(animationFolderPath, parentFolderName, overrideController, viewSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animations created in selected folder group.");
    }

    private static List<Sprite> LoadSpritesFromFolder(string folderPath)
    {
        string[] filePaths = Directory.GetFiles(folderPath, "*.png");
        List<Sprite> sprites = filePaths.Select(path =>
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                // 스프라이트의 텍스처를 로드하고 필터 모드를 Point로 설정
                string texturePath = AssetDatabase.GetAssetPath(sprite.texture);
                TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.filterMode = FilterMode.Point;
                    textureImporter.SaveAndReimport(); // 변경 사항 저장 및 다시 가져오기
                }
            }
            return sprite;
        }).ToList();

        return sprites;
    }


    private static void GenerateExtraFiles(string folderPath, string animationFolderPath,
        string parentFolderName, string subFolderName)
    {
        string folderName = new DirectoryInfo(folderPath).Name;
        List<Sprite> sprites = LoadSpritesFromFolder(folderPath);

        if (sprites.Count == 0)
        {
            Debug.LogWarning($"No sprites found in the folder: {folderPath}");
            return;
        }

        // 애니메이션 클립 생성
        AnimationClip animationClip = new AnimationClip();
        animationClip.frameRate = 12f; // 프레임 레이트를 원하는 값으로 설정

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

        // Animation Event 추가
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
            attackEvent.time = 4 / animationClip.frameRate;
            animationEvents.Add(attackEvent);
        }

        // 애니메이션 이벤트를 클립에 설정
        AnimationUtility.SetAnimationEvents(animationClip, animationEvents.ToArray());

        // 애니메이션 클립 저장
        string savePath = Path.Combine(animationFolderPath, $"{subFolderName}_{folderName}.anim");
        savePath = savePath.Replace("\\", "/"); // 경로가 유니티에서 인식될 수 있도록 슬래시 변경

        AssetDatabase.CreateAsset(animationClip, savePath);
    }

    private static AnimatorOverrideController AddAnimationsToBaseController(string parentFolderPath, string parentFolderName)
    {
        string[] animationClipPaths = Directory.GetFiles(parentFolderPath, "*.anim");
        AnimationClip[] animationClips = animationClipPaths
            .Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path)).ToArray();

        string overrideControllerPath = Path.Combine(parentFolderPath, $"{parentFolderName}_AnimationController.controller");
        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

        string baseControllerName = "BaseCharacterAnimController";
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController " + baseControllerName);
        if (guids.Length == 0)
        {
            Debug.LogError("Failed to find BaseAnimController.");
            return null;
        }

        string baseControllerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);

        if (baseController == null)
        {
            Debug.LogError("Failed to load BaseAnimController.");
            return null;
        }

        overrideController.runtimeAnimatorController = baseController;

        foreach (AnimationClip clip in animationClips)
        {
            overrideController[clip.name] = clip;
        }

        Debug.Log("Animations added to BaseAnimController successfully.");
        return overrideController;
    }

    private static void AddCharacterPrefab(string parentFolderPath, string parentFolderName, AnimatorOverrideController controller, Sprite sprite)
    {
        // BasePrefab 찾기
        string basePrefabName = "BasePrefab";
        string[] guids = AssetDatabase.FindAssets($"t:Prefab {basePrefabName}");
        if (guids.Length == 0)
        {
            Debug.LogError("Failed to find BasePrefab.");
            return;
        }

        string basePrefabPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
        GameObject objSource = (GameObject)PrefabUtility.InstantiatePrefab(source);

        // 원하는 트랜스폼 찾기
        Transform childTransform = objSource.transform.GetChild(0).GetChild(0);

        // SpriteRenderer 업데이트
        SpriteRenderer spriteRenderer = childTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
        else
        {
            Debug.LogError("Failed to find SpriteRenderer.");
            return;
        }

        // Animator 업데이트
        Animator animator = childTransform.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = controller;
        }
        else
        {
            Debug.LogError("Failed to find Animator.");
            return;
        }

        // 프리팹 저장
        string prefabFullPath = Path.Combine(parentFolderPath, $"CharacterView_{parentFolderName}.prefab");
        prefabFullPath = prefabFullPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(objSource, prefabFullPath);

        // 생성된 임시 오브젝트 삭제
        Object.DestroyImmediate(objSource);

        Debug.Log("Prefab created successfully at: " + prefabFullPath);
    }


    private static void CreateSingleSpriteAtlas(string parentFolderPath, List<Sprite> allSprites,
        string parentFolderName)
    {
        if (allSprites.Count == 0)
        {
            Debug.LogWarning("No sprites found to add to the atlas.");
            return;
        }

        // 스프라이트 아틀라스 생성
        SpriteAtlas spriteAtlas = new SpriteAtlas();

        spriteAtlas.SetIncludeInBuild(true);
        spriteAtlas.SetIsVariant(false);

        // Packing settings
        SpriteAtlasPackingSettings packingSettings = spriteAtlas.GetPackingSettings();
        packingSettings.enableRotation = false;
        packingSettings.enableTightPacking = false;
        packingSettings.padding = 2; // 이미지 간 거리를 최소화시킴
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

        // Platform settings for Android
        TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = 2048,
            format = TextureImporterFormat.ASTC_4x4,
            compressionQuality = 50,
            crunchedCompression = false,
            androidETC2FallbackOverride = AndroidETC2FallbackOverride.Quality32Bit
        };
        spriteAtlas.SetPlatformSettings(androidSettings);

        // Platform settings for iOS
        TextureImporterPlatformSettings iosSettings = new TextureImporterPlatformSettings
        {
            name = "iPhone",
            overridden = true,
            maxTextureSize = 2048,
            format = TextureImporterFormat.ASTC_4x4,
            compressionQuality = 50,
            crunchedCompression = false
        };
        spriteAtlas.SetPlatformSettings(iosSettings);

        // 아틀라스 저장
        string savePath = Path.Combine(parentFolderPath, $"{parentFolderName}.spriteatlas");
        savePath = savePath.Replace("\\", "/");
        AssetDatabase.CreateAsset(spriteAtlas, savePath);

        // Packing atlases
        UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[] {spriteAtlas}, EditorUserBuildSettings.activeBuildTarget);

        Debug.Log("Single SpriteAtlas created with all sprites.");
    }
}

