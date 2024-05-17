using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class SpriteAnimationFromFolderGroup : Editor
{
    [MenuItem("Assets/Create Sprite Animations From Folder Group", true)]
    private static bool ValidateCreateAnimation()
    {
        // 메뉴가 활성화될 조건: 선택한 것이 폴더여야 함
        return Selection.activeObject != null && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
    }

    [MenuItem("Assets/Create Sprite Animations From Folder Group")]
    private static void CreateAnimations()
    {
        // 선택된 상위 폴더의 경로
        string parentFolderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        string parentFolderName = new DirectoryInfo(parentFolderPath).Name;
        string animationFolderPath = Path.Combine(parentFolderPath, "Animation");

        // Animation 폴더 생성
        if (!AssetDatabase.IsValidFolder(animationFolderPath))
        {
            AssetDatabase.CreateFolder(parentFolderPath, "Animation");
        }

        // 상위 폴더 내의 모든 하위 폴더 검색 (예: Front, Back)
        string[] subFolderPaths = Directory.GetDirectories(parentFolderPath);

        foreach (string subFolderPath in subFolderPaths)
        {
            // 각 하위 폴더 내의 모든 폴더 검색 (예: ATK, DEAD, IDLE)
            string[] subSubFolderPaths = Directory.GetDirectories(subFolderPath);
            foreach (string subSubFolderPath in subSubFolderPaths)
            {
                CreateAnimationFromFolder(subSubFolderPath, animationFolderPath, parentFolderName, new DirectoryInfo(subFolderPath).Name);
            }
        }

        // 애니메이션 컨트롤러에 애니메이션 클립 추가
        AddAnimationsToBaseController(parentFolderPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animations created in selected folder group.");
    }

    private static void CreateAnimationFromFolder(string folderPath, string animationFolderPath, string parentFolderName, string subFolderName)
    {

        // 스프라이트 아틀라스 생성
        string atlasFolderPath = Path.Combine(parentFolderName, "SpriteAtlases");
        string atlasName = $"{subFolderName}_Atlas";
        CreateSpriteAtlas(folderPath, atlasFolderPath, atlasName);

        string folderName = new DirectoryInfo(folderPath).Name;

        // 하위 폴더 내의 모든 스프라이트 로드
        string[] filePaths = Directory.GetFiles(folderPath, "*.png");
        Sprite[] sprites = filePaths.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"No sprites found in the folder: {folderPath}");
            return;
        }

        // 애니메이션 클립 생성
        AnimationClip animationClip = new AnimationClip();
        animationClip.frameRate = 12f;  // 프레임 레이트를 원하는 값으로 설정

        EditorCurveBinding curveBinding = new EditorCurveBinding();
        curveBinding.type = typeof(SpriteRenderer);
        curveBinding.path = "";
        curveBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i / animationClip.frameRate;
            sprites[i].texture.filterMode = FilterMode.Point;
            keyFrames[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(animationClip, curveBinding, keyFrames);

        // 애니메이션 클립 저장
        string savePath = Path.Combine(animationFolderPath, $"{subFolderName}_{folderName}.anim");
        savePath = savePath.Replace("\\", "/"); // 경로가 유니티에서 인식될 수 있도록 슬래시 변경

        AssetDatabase.CreateAsset(animationClip, savePath);
    }

    private static void AddAnimationsToBaseController(string parentFolderPath)
    {
        // Animation 폴더 내의 애니메이션 클립 가져오기
        string animationFolderPath = Path.Combine(parentFolderPath, "Animation");
        string[] animationClipPaths = Directory.GetFiles(animationFolderPath, "*.anim");
        AnimationClip[] animationClips = animationClipPaths.Select(path => AssetDatabase.LoadAssetAtPath<AnimationClip>(path)).ToArray();

        // AnimationOverrideController 생성
        string overrideControllerPath = Path.Combine(animationFolderPath, "AnimationOverrideController.controller");
        AnimatorOverrideController overrideController = new AnimatorOverrideController();
        AssetDatabase.CreateAsset(overrideController, overrideControllerPath);

        // BaseAnimController 가져오기
        string baseControllerName = "BaseCharacterAnimController";
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController " + baseControllerName);
        if (guids.Length == 0)
        {
            Debug.LogError("Failed to find BaseAnimController.");
            return;
        }

        string baseControllerPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimatorController baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(baseControllerPath);

        if (baseController == null)
        {
            Debug.LogError("Failed to load BaseAnimController.");
            return;
        }

        // AnimationOverrideController에 Animator Controller 할당
        overrideController.runtimeAnimatorController = baseController;

        // 애니메이션 오버라이드 컨트롤러에 애니메이션 할당
        foreach (AnimationClip clip in animationClips)
        {
            overrideController[clip.name] = clip;
        }

        Debug.Log("Animations added to BaseAnimController successfully.");
    }

    private static void CreateSpriteAtlas(string folderPath, string atlasFolderPath, string atlasName)
    {
        // 폴더 내의 모든 스프라이트 로드
        string[] filePaths = Directory.GetFiles(folderPath, "*.png");
        Sprite[] sprites = filePaths.Select(path => AssetDatabase.LoadAssetAtPath<Sprite>(path)).ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogWarning($"No sprites found in the folder: {folderPath}");
            return;
        }

        // 스프라이트 아틀라스 생성
        SpriteAtlas spriteAtlas = new SpriteAtlas();
        spriteAtlas.Add(sprites);

        // 아틀라스 저장
        string savePath = Path.Combine(folderPath, $"{atlasName}.spriteatlas");
        savePath = savePath.Replace("\\", "/"); // 경로가 유니티에서 인식될 수 있도록 슬래시 변경
        AssetDatabase.CreateAsset(spriteAtlas, savePath);
    }
}

