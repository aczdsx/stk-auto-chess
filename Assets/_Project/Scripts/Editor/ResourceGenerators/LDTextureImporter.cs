#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LDTextureImporter : AssetPostprocessor
{
    public const string FolderPath = "Assets/_Project/Addressables/Remote/LD";
    
    private void OnPreprocessTexture()
    {
        // 특정 폴더 내의 텍스처만 처리
        if (!assetPath.StartsWith(FolderPath))
            return;

        var textureImporter = assetImporter as TextureImporter;
        if (textureImporter == null)
            return;

        if (assetPath.Contains("Illust"))
        {
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            TextureImporterSettings settings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            textureImporter.SetTextureSettings(settings);
            textureImporter.ClearPlatformTextureSettings("Android");
            textureImporter.ClearPlatformTextureSettings("iPhone");
        }
        else
        {
            textureImporter.mipmapEnabled = false;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.ClearPlatformTextureSettings("Android");
            textureImporter.ClearPlatformTextureSettings("iPhone");
        }
    }
}
#endif
