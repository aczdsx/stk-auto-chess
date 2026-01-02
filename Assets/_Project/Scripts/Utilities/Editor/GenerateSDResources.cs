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

public static class GenerateSDResources
{
    private const int DEAD_FRAME_COUNT = 10;
    private const string LOCK_SUFFIX = "_lock";

    /// <summary>
    /// 폴더가 SD 리소스 생성에서 제외되어야 하는지 확인합니다.
    /// 폴더명 끝에 "_lock" 접미사가 있으면 제외됩니다. (대소문자 구분 없음)
    ///
    /// 사용법: 아트 작업자가 수동 작업이 필요한 폴더의 이름을 변경
    /// 예시: "80109003" → "80109003_lock" (또는 "80109003_Lock", "80109003_LOCK" 등)
    /// </summary>
    private static bool IsExcludedPath(string path)
    {
        string folderName = new DirectoryInfo(path).Name;
        return folderName.EndsWith(LOCK_SUFFIX, System.StringComparison.OrdinalIgnoreCase);
    }

    public static void CreateAllSDResources()
    {
        string[] groupFolderPaths = Directory.GetDirectories(ResourcePath.SD_PATH);

        List<SpriteAtlas> createdAtlases = new List<SpriteAtlas>();
        int totalFolders = groupFolderPaths.Length;
        EditorUtility.DisplayProgressBar("Generating SD Resources", "Generating...", 0f);

        // Import 파이프라인 일시 중지 (대량 에셋 작업 최적화)
        AssetDatabase.StartAssetEditing();
        try
        {
            for (var i = 0; i < groupFolderPaths.Length; i++)
            {
                var groupFolderPath = groupFolderPaths[i];
                var subFolders = Directory.GetDirectories(groupFolderPath);
                foreach (var subFolder in subFolders)
                {
                    // 제외 경로 체크
                    if (IsExcludedPath(subFolder))
                        continue;

                    var folderName = new DirectoryInfo(subFolder).Name;
                    if (int.TryParse(folderName, out _))
                    {
                        CreateAnimationsFromPath(subFolder, createdAtlases);
                    }
                }

                EditorUtility.DisplayProgressBar("Generating SD Resources", "Generating...", (float)(i + 1) / totalFolders);
            }
        }
        finally
        {
            // 반드시 StopAssetEditing 호출 (예외 발생해도)
            AssetDatabase.StopAssetEditing();
        }

        SpriteAtlasUtility.PackAtlases(createdAtlases.ToArray(), EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.ClearProgressBar();
    }

    public static void CreateAnimationsFromPath(string parentFolderPath, List<SpriteAtlas> createdAtlases)
    {
        string parentFolderName = new DirectoryInfo(parentFolderPath).Name;
        string generateResourcesPath = Path.Combine(parentFolderPath, "GenerateResources");

        if (!AssetDatabase.IsValidFolder(generateResourcesPath))
        {
            AssetDatabase.CreateFolder(parentFolderPath, "GenerateResources");
        }

        string normalizedParentPath = parentFolderPath.Replace("\\", "/");

        // 한 번에 모든 스프라이트 검색 후 폴더별로 그룹화
        var spritesByFolder = LoadAndGroupSprites(normalizedParentPath);

        // Back, Front 폴더 경로 수집
        List<string> atlasFolderPaths = new List<string>();
        Sprite viewSprite = null;

        List<AnimationClip> animationClips = new List<AnimationClip>();
        foreach (var (folderPath, sprites) in spritesByFolder)
        {
            // 폴더 구조: .../Back/IDLE, .../Front/ATK 등
            string[] pathParts = folderPath.Split('/');
            if (pathParts.Length < 2) continue;

            string subFolderName = pathParts[^2]; // Back 또는 Front
            string actionName = pathParts[^1];    // IDLE, ATK, SKL 등

            if (subFolderName is not "Back" and not "Front")
                continue;

            // SpriteAtlas용 폴더 경로 수집 (중복 방지)
            string subFolderPath = folderPath[..folderPath.LastIndexOf('/')];
            if (!atlasFolderPaths.Contains(subFolderPath))
                atlasFolderPaths.Add(subFolderPath);

            // 애니메이션 생성
            var animationClip = GenerateExtraFiles(sprites, actionName, generateResourcesPath, subFolderName);
            if (animationClip != null)
                animationClips.Add(animationClip);

            // viewSprite 설정 (Front/IDLE의 첫 번째 스프라이트)
            if (viewSprite == null && subFolderName == "Front" && actionName == "IDLE" && sprites.Count > 0)
                viewSprite = sprites[0];
        }

        createdAtlases.Add(CreateOrUpdateSpriteAtlas(generateResourcesPath, atlasFolderPaths, parentFolderName));

        var overrideController = AddAnimationsToBaseController(generateResourcesPath, parentFolderName, animationClips);

        AddInGameCharacterPrefab(generateResourcesPath, parentFolderName, overrideController, viewSprite);
        AddUICharacterPrefab(generateResourcesPath, parentFolderName, viewSprite);
        AddElpisCharacterPrefab(generateResourcesPath, parentFolderName, overrideController, viewSprite);

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

    private static Dictionary<string, List<Sprite>> LoadAndGroupSprites(string parentFolderPath)
    {
        int ExtractNumberFromName(string name)
        {
            int result = 0;
            foreach (char c in name)
            {
                if (char.IsDigit(c))
                    result = result * 10 + (c - '0');
            }
            return result == 0 ? int.MaxValue : result;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { parentFolderPath });

        return guids
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .GroupBy(path => Path.GetDirectoryName(path).Replace("\\", "/"))
            .ToDictionary(
                g => g.Key,
                g => g.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path))
                      .Where(sprite => sprite != null)
                      .OrderBy(sprite => ExtractNumberFromName(sprite.name))
                      .ToList()
            );
    }

    private static AnimationClip GenerateExtraFiles(List<Sprite> sprites, string actionName,
        string animationFolderPath, string subFolderName)
    {
        if (sprites.Count == 0)
        {
            Debug.Log("[Fail] No Sprite");
            return null;
        }

        // 애니메이션 클립 생성
        AnimationClip animationClip = new AnimationClip();
        animationClip.frameRate = sprites.Count > 12 ? sprites.Count : 12f;
        if (actionName == "IDLE" || actionName == "MOVE")
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

        int frameCount = actionName == "DEAD" ? sprites.Count * DEAD_FRAME_COUNT : sprites.Count;
        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i / animationClip.frameRate;
            keyFrames[i].value = sprites[i / (actionName == "DEAD" ? DEAD_FRAME_COUNT : 1)];
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

        if (actionName == "ATK")
        {
            AnimationEvent attackEvent = new AnimationEvent();
            attackEvent.functionName = "InvokeAnimationEvent";
            attackEvent.intParameter = (int)AnimationEventKey.Execute1Per1;
            attackEvent.time = 0.3f;
            animationEvents.Add(attackEvent);
        }

        if (actionName == "SKL")
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
        string savePath = Path.Combine(animationFolderPath, $"{subFolderName}_{actionName}.anim");
        savePath = savePath.Replace("\\", "/");

        string clipName = $"{subFolderName}_{actionName}";
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(savePath);
        if (existingClip != null)
        {
            EditorUtility.CopySerialized(animationClip, existingClip);
            existingClip.name = clipName;
            EditorUtility.SetDirty(existingClip);
        }
        else
        {
            animationClip.name = clipName;
            AssetDatabase.CreateAsset(animationClip, savePath);
        }

        return animationClip;
    }

    private static AnimatorOverrideController AddAnimationsToBaseController(string parentFolderPath, string parentFolderName, List<AnimationClip> animationClips)
    {
        if (animationClips == null || animationClips.Count == 0)
        {
            Debug.LogError("[Fail] Animation Clips are not loaded properly");
            return null;
        }

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

        string overrideControllerPath = Path.Combine(parentFolderPath, $"{parentFolderName}_AnimationController.controller");
        overrideControllerPath = overrideControllerPath.Replace("\\", "/");

        AnimatorOverrideController existingController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overrideControllerPath);
        AnimatorOverrideController overrideController;

        if (existingController != null)
        {
            overrideController = existingController;
        }
        else
        {
            overrideController = new AnimatorOverrideController();
            AssetDatabase.CreateAsset(overrideController, overrideControllerPath);
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

        EditorUtility.SetDirty(overrideController);

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

        Object.DestroyImmediate(objSource);

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

        Object.DestroyImmediate(objSource);

        Debug.Log("[Success] UI Prefab Created");
    }

    private static void AddElpisCharacterPrefab(string parentFolderPath, string parentFolderName, AnimatorOverrideController controller, Sprite sprite)
    {
        if (!parentFolderPath.Contains("Characters"))
            return;

        string basePrefabName = "BasePrefab_Elpis";
        string[] guids = AssetDatabase.FindAssets($"t:Prefab {basePrefabName}", new [] { ResourcePath.SD_PATH });
        if (guids.Length == 0)
        {
            Debug.Log("[Fail] Find BasePrefab_Elpis");
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

        string prefabFullPath = Path.Combine(parentFolderPath, $"Elpis_{parentFolderName}.prefab");
        prefabFullPath = prefabFullPath.Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(objSource, prefabFullPath);

        Object.DestroyImmediate(objSource);

        Debug.Log("[Success] Elpis Prefab Created");
    }

    private static SpriteAtlas CreateOrUpdateSpriteAtlas(string parentFolderPath, List<string> folderPaths,
        string parentFolderName)
    {
        string savePath = Path.Combine(parentFolderPath, $"{parentFolderName}.spriteatlas");
        savePath = savePath.Replace("\\", "/");

        // 기존 SpriteAtlas가 있으면 로드, 없으면 새로 생성
        SpriteAtlas spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(savePath);
        bool isNewAtlas = spriteAtlas == null;

        if (isNewAtlas)
        {
            spriteAtlas = new SpriteAtlas();

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

            AssetDatabase.CreateAsset(spriteAtlas, savePath);
        }

        // 기존 packable 객체들 제거 후 새로 등록
        Object[] existingPackables = spriteAtlas.GetPackables();
        if (existingPackables.Length > 0)
        {
            spriteAtlas.Remove(existingPackables);
        }

        // Back/Front 폴더를 SpriteAtlas에 등록
        List<Object> foldersToAdd = new List<Object>();
        foreach (string folderPath in folderPaths)
        {
            string normalizedPath = folderPath.Replace("\\", "/");
            Object folder = AssetDatabase.LoadAssetAtPath<Object>(normalizedPath);
            if (folder != null)
            {
                foldersToAdd.Add(folder);
            }
        }

        if (foldersToAdd.Count > 0)
        {
            spriteAtlas.Add(foldersToAdd.ToArray());
        }

        EditorUtility.SetDirty(spriteAtlas);

        Debug.Log(isNewAtlas ? "[Success] Sprite Atlas Created" : "[Success] Sprite Atlas Updated");
        return spriteAtlas;
    }
}
#endif