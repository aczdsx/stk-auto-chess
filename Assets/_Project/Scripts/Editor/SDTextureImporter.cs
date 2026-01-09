#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SDTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        // 특정 폴더 내의 텍스처만 처리
        if (!assetPath.StartsWith(ResourcePath.SD_PATH))
            return;

        TextureImporter textureImporter = assetImporter as TextureImporter;
        if (textureImporter != null)
        {
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;

            TextureImporterSettings settings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(settings);
            settings.spriteGenerateFallbackPhysicsShape = false;
            textureImporter.SetTextureSettings(settings);
            textureImporter.ClearPlatformTextureSettings("Android");
            textureImporter.ClearPlatformTextureSettings("iPhone");
        }
    }
}
#endif
