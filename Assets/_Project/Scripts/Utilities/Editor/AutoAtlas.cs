using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class AtlasGenerator : Editor
{
    [MenuItem("버거몬스터/아틀라스 생성", priority = 1000)]
    private static void ExecuteAtlas()
    {
        AutoAtlas.ExecuteAtlas();
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