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

    [MenuItem("CookApps/Generate Pixel Resources For All Subfolders")]
    private static void CreateAnimationsForAllSubfolders()
    {
        string specificFolderPath = "Assets/_Project/Characters";

        string[] subFolderPaths = Directory.GetDirectories(specificFolderPath);

        foreach (string subFolderPath in subFolderPaths)
        {
            string folderName = new DirectoryInfo(subFolderPath).Name;
            if (int.TryParse(folderName, out _))
            {
                CreateAnimationsFromPath(subFolderPath);
            }
        }
    }

    [MenuItem("Assets/Generate Pixel Resources", true)]
    private static bool ValidateCreateAnimation()
    {
        return Selection.activeObject != null &&
               AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }


    [MenuItem("Assets/Generate Pixel Resources")]
    private static void CreateAnimations()
    {
        string parentFolderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string animationFolderPath = Path.Combine(parentFolderPath, "GenerateResources");

        if (!AssetDatabase.IsValidFolder(animationFolderPath))
        {
            AssetDatabase.CreateFolder(parentFolderPath, "GenerateResources");
        }

        CreateAnimationsFromPath(parentFolderPath);
    }

    private static void CreateAnimationsFromPath(string parentFolderPath)
    {
        string parentFolderName = new DirectoryInfo(parentFolderPath).Name;
        string animationFolderPath = Path.Combine(parentFolderPath, "GenerateResources");

        if (!AssetDatabase.IsValidFolder(animationFolderPath))
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
                GenerateExtraFiles(subSubFolderPath, animationFolderPath, parentFolderName,
                    new DirectoryInfo(subFolderPath).Name);

                if (subSubFolderPath.EndsWith("Front\\IDLE")) {
                    viewSprite = sprite[0];
                }
            }
        }

        CreateSingleSpriteAtlas(animationFolderPath, allSprites, parentFolderName);

        var overrideController = AddAnimationsToBaseController(animationFolderPath, parentFolderName);

        AddCharacterPrefab(animationFolderPath, parentFolderName, overrideController, viewSprite);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Success]");
    }

    private static List<Sprite> LoadSpritesFromFolder(string folderPath)
    {
        string[] filePaths = Directory.GetFiles(folderPath, "*.png");
        List<Sprite> sprites = filePaths.Select(path =>
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
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
            Debug.Log("[Fail] No Sprite");
            return;
        }

        // 애니메이션 클립 생성
        AnimationClip animationClip = new AnimationClip();
        animationClip.frameRate = 12f;

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
            attackEvent.time = 4 / animationClip.frameRate;
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

        string overrideControllerPath = Path.Combine(parentFolderPath, $"{parentFolderName}_AnimationController.controller");
        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

        string baseControllerName = "BaseCharacterAnimController";
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController " + baseControllerName);
        if (guids.Length == 0)
        {
            Debug.Log("[Fail] Base Anim Controller");
            return null;
        }

        string baseControllerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);

        if (baseController == null)
        {
            Debug.Log("[Fail] Base Anim Controller");
            return null;
        }

        overrideController.runtimeAnimatorController = baseController;

        foreach (AnimationClip clip in animationClips)
        {
            overrideController[clip.name] = clip;
        }

        Debug.Log("[Success] AddAnimationsToBaseController");
        return overrideController;
    }

    private static void AddCharacterPrefab(string parentFolderPath, string parentFolderName, AnimatorOverrideController controller, Sprite sprite)
    {
        string basePrefabName = "BasePrefab";
        string[] guids = AssetDatabase.FindAssets($"t:Prefab {basePrefabName}");
        if (guids.Length == 0)
        {
            Debug.Log("[Fail] Find BasePrefab");
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

        string prefabFullPath = Path.Combine(parentFolderPath, $"CharacterView_{parentFolderName}.prefab");
        prefabFullPath = prefabFullPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(objSource, prefabFullPath);

        Object.DestroyImmediate(objSource);

        Debug.Log("[Success] Prefab");
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

        string savePath = Path.Combine(parentFolderPath, $"{parentFolderName}.spriteatlas");
        savePath = savePath.Replace("\\", "/");
        AssetDatabase.CreateAsset(spriteAtlas, savePath);

        // Packing atlases
        UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[] {spriteAtlas}, EditorUserBuildSettings.activeBuildTarget);

        Debug.Log("[Success] Sprite Atlas");
    }
}

