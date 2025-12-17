#if UNITY_EDITOR
using System.IO;
using UnityEditor;

public class GenerateResourcesMenu
{
    
    [MenuItem("Tools/LD Resources/Generate All LD Resources")]
    private static void CreateLDResourcesForAllCharacters()
    {
        GenerateLDResources.CreateAllLDResources(false);
    }

    [MenuItem("Tools/LD Resources/Force Generate All LD Resources")]
    public static void ForceCreateLDResourcesForAllCharacters()
    {
        GenerateLDResources.CreateAllLDResources(true);
    }

    [MenuItem("Tools/Pixel Resources/Generate All Pixel Resources")]
    private static void CreateAnimationsForAllSubfolders()
    {
        GeneratePixelResources.CreateAllPixelResources(false);
    }

    [MenuItem("Tools/Pixel Resources/Force Generate All Pixel Resources")]
    public static void ForceCreateAnimationsForAllSubfolders()
    {
        GeneratePixelResources.CreateAllPixelResources(true);
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
                CreatePixelResource(folderPath);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
       
    }

    private static void CreatePixelResource(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        string generateResourcesPath = Path.Combine(folderPath, "GenerateResources");

        if (AssetDatabase.IsValidFolder(generateResourcesPath))
        {
            AssetDatabase.DeleteAsset(generateResourcesPath);
        }

        GeneratePixelResources.CreateAnimationsFromPath(folderPath);
    }

    private static void CreateLDResources(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        string generateResourcesPath = Path.Combine(folderPath, "GenerateResources");

        if (AssetDatabase.IsValidFolder(generateResourcesPath))
        {
            AssetDatabase.DeleteAsset(generateResourcesPath);
        }

        GenerateLDResources.CreateLDResourceFromPath(folderPath);
    }

}
#endif