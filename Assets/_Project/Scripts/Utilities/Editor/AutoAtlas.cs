using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class AtlasGenerator : Editor
{
    [MenuItem("버거몬스터/아틀라스 생성", priority = 1000)]
    private static void ExecuteAnimation()
    {
        AutoAtlas.ExecuteAtlas();
    }
    private static void ExecuteAtlas()
    {
        AutoAtlas.ExecuteAtlas();
    }
}




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

}





internal class AutoAtlas
{
    internal static void ExecuteAtlas()
    {
        string rootPath = Path.Combine("Assets", "_Project", "Characters");
        FindChildFolderRecursive(rootPath);

        // 원하는 경로를 추가하세요
        // rootPath = Path.Combine("Assets", "JH_Resource", "CHARACTER2");
        // FindChildFolderRecursive(rootPath);

        AssetDatabase.Refresh();
    }


    private static void FindChildFolderRecursive(string rootPath)
    {
        string[] rootFolers = Directory.GetDirectories(Path.Combine(Path.GetFullPath("."), rootPath));
        string fullPath = Path.GetFullPath(".");
        for (int i = 0; i < rootFolers.Length; i++)
        {
            string rootFolder = rootFolers[i];
            string[] files = Directory.GetFiles(rootFolder);
            List<string> imageFiles = new List<string>();
            foreach (string file in files)
            {
                string extension = Path.GetExtension(file);
                if (extension.Equals(".png") || extension.Equals(".jpg"))
                {
                    imageFiles.Add(file);
                }
            }

            //png나 jpg가 있는 폴더
            if (imageFiles.Count > 0)
            {
                string currentDirectory = new DirectoryInfo(rootFolder).Name;
                //.으로 시작하는 폴더는 무시
                if (currentDirectory.Substring(0, 1).Equals("."))
                {
                    continue;
                }
                string atlasName = $"{currentDirectory}.spriteatlas";
                List<Texture2D> textures = new List<Texture2D>();
                for (int j = 0; j < imageFiles.Count; j++)
                {
                    string imagePath = imageFiles[j].Replace($"{fullPath}\\", "");
                    Texture2D texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                    textures.Add(texture2D);
                }

                SpriteAtlas spriteAtlas = null;
                string spriteFilePath = Path.Combine(rootPath, currentDirectory, atlasName);
                string spriteFileFullPath = Path.Combine(Path.GetFullPath("."), spriteFilePath);
                if (File.Exists(spriteFileFullPath))
                {
                    spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(spriteFilePath);
                    spriteAtlas.Remove(SpriteAtlasExtensions.GetPackables(spriteAtlas));
                }
                else
                {
                    spriteAtlas = new SpriteAtlas();
                    AssetDatabase.CreateAsset(spriteAtlas, spriteFilePath);
                }

                spriteAtlas.SetIncludeInBuild(true);
                spriteAtlas.SetIsVariant(false);
                SpriteAtlasPackingSettings packaingSettings = spriteAtlas.GetPackingSettings();
                packaingSettings.enableRotation = true;
                packaingSettings.enableTightPacking = false;
                packaingSettings.padding = 4;       //이미지간 거리를 최소화시킴
                spriteAtlas.SetPackingSettings(packaingSettings);

                SpriteAtlasTextureSettings textureSettings = new SpriteAtlasTextureSettings();
                textureSettings.sRGB = true;       //sRGB 값 true
                textureSettings.generateMipMaps = false;
                textureSettings.readable = false;
                textureSettings.filterMode = FilterMode.Point;
                spriteAtlas.SetTextureSettings(textureSettings);


                spriteAtlas.Add(textures.ToArray());

                UnityEditor.U2D.SpriteAtlasUtility.PackAtlases(new[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget);
            }

            FindChildFolderRecursive(rootFolder.Replace($"{fullPath}\\", ""));
        }
    }
}
