#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SDTextureImporter : AssetPostprocessor
{
    public const string FolderPath = "Assets/_Project/Addressables/Remote/SD";

    private void OnPreprocessTexture()
    {
        // 특정 폴더 내의 텍스처만 처리
        if (!assetPath.StartsWith(FolderPath))
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
        }
    }
}
#endif
