#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.U2D;

public static class ResourcePath
{
    public const string SD_PATH = "Assets/_Project/Addressables/Remote/SD";
    public const string LD_PATH = "Assets/_Project/Addressables/Remote/LD";
}

public class GenerateResourcesMenu
{
    
    [MenuItem("Tools/Resources/Generate All LD Resources")]
    private static void CreateLDResourcesForAllCharacters()
    {
        GenerateLDResources.CreateAllLDResources();
    }

    [MenuItem("Tools/Resources/Generate All SD Resources")]
    private static void CreateAnimationsForAllSubfolders()
    {
        GenerateSDResources.CreateAllSDResources();
    }

    [MenuItem("Assets/Generate Resources", true)]
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

    [MenuItem("Assets/Generate Resources")]
    private static void CreateResourcesForSelectedFolders()
    {
        List<SpriteAtlas> createdAtlas = new List<SpriteAtlas>();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var obj in Selection.objects)
            {
                string folderPath = AssetDatabase.GetAssetPath(obj);
                string normalizedPath = folderPath.Replace("\\", "/");
                string[] pathParts = normalizedPath.Split('/');
                if (pathParts.Length < 4)
                    continue;

                if (pathParts[^4] != "Remote")
                    continue;

                if (pathParts[^3].Contains("LD"))
                {
                    CreateLDResources(folderPath);
                }
                else if (pathParts[^3].Contains("SD"))
                {
                    CreateSDResource(folderPath, createdAtlas);
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (createdAtlas.Count > 0)
        {
            SpriteAtlasUtility.PackAtlases(createdAtlas.ToArray(), EditorUserBuildSettings.activeBuildTarget);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateSDResource(string folderPath, List<SpriteAtlas> createdAtlas)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        GenerateSDResources.CreateAnimationsFromPath(folderPath, createdAtlas);
    }

    private static void CreateLDResources(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        GenerateLDResources.CreateLDResourceFromPath(folderPath);
    }

}
#endif