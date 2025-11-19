using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using System.IO;
using System.Reflection;
using System.Linq;

public class AtlasExporter
{
    [MenuItem("Assets/Export Sprite Atlas", false, 0)]
    static void ExportAtlas()
    {
        var selectedAtlases = Selection.objects.OfType<SpriteAtlas>().ToList();
        if (selectedAtlases.Count == 0)
        {
            Debug.LogError("SpriteAtlas를 선택해주세요.");
            return;
        }

        // Assets/ExportedAtlases 폴더 생성
        string exportFolder = "Assets/ExportedAtlases";
        if (!AssetDatabase.IsValidFolder(exportFolder))
        {
            string parentFolder = Path.GetDirectoryName(exportFolder);
            string folderName = Path.GetFileName(exportFolder);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }

        int successCount = 0;
        int failCount = 0;

        foreach (var atlas in selectedAtlases)
        {
            var tex = GetPreviewTexture(atlas);
            if (tex == null)
            {
                Debug.LogWarning($"Preview 텍스처를 가져올 수 없습니다: {atlas.name}");
                failCount++;
                continue;
            }

            // 읽을 수 있는 텍스처 복사본 생성
            var readableTex = CreateReadableTexture(tex);
            if (readableTex == null)
            {
                Debug.LogWarning($"텍스처를 읽을 수 있는 형태로 변환할 수 없습니다: {atlas.name}");
                failCount++;
                continue;
            }

            string fileName = atlas.name;
            string exportPath = Path.Combine(exportFolder, $"{fileName}.png").Replace("\\", "/");

            byte[] bytes = readableTex.EncodeToPNG();
            File.WriteAllBytes(exportPath, bytes);
            
            // 임시 텍스처 정리
            Object.DestroyImmediate(readableTex);
            
            successCount++;
        }
        
        AssetDatabase.Refresh();
        
        Debug.Log($"Atlas export 완료: 성공 {successCount}개, 실패 {failCount}개 (저장 위치: {exportFolder})");
    }

    [MenuItem("Assets/Export Sprite Atlas", true)]
    static bool ValidateExportAtlas()
    {
        return Selection.objects.OfType<SpriteAtlas>().Any();
    }

    static Texture2D GetPreviewTexture(SpriteAtlas atlas)
    {
        // UnityEditor.U2D.SpriteAtlasExtensions.GetPreviewTextures 사용
        var extensionType = System.Type.GetType("UnityEditor.U2D.SpriteAtlasExtensions,UnityEditor");
        if (extensionType != null)
        {
            var method = extensionType.GetMethod("GetPreviewTextures", BindingFlags.NonPublic | BindingFlags.Static);
            if (method != null)
            {
                var result = method.Invoke(null, new object[] { atlas });
                if (result is Texture2D[] textures && textures.Length > 0)
                {
                    return textures[0];
                }
            }
        }
        return null;
    }

    static Texture2D CreateReadableTexture(Texture2D source)
    {
        // RenderTexture를 사용해서 읽을 수 있는 텍스처 복사본 생성
        var tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, tmp);
        
        RenderTexture.active = tmp;
        try
        {
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            copy.Apply();
            return copy;
        }
        finally
        {
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(tmp);
        }
    }
}