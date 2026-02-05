#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace CookApps.AutoBattler.Editor
{
    public static class AddressableSpriteAtlasGuard
    {
        public static void PreAddressableBuild(BuildTarget target)
        {
            Debug.Log("[AtlasGuard] SpriteAtlas 검증 시작...");

            // .spriteatlas 파일을 파일시스템에서 직접 탐색
            var files = System.IO.Directory.GetFiles(
                "Assets", "*.spriteatlasv2", System.IO.SearchOption.AllDirectories);

            var broken = new List<string>();

            foreach (var file in files)
            {
                var path = file.Replace("\\", "/");
                var asset = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

                // 1. SpriteAtlas로 로드 자체가 안 되면 깨진 것
                if (asset == null)
                {
                    broken.Add(path);
                }
            }

            if (broken.Count == 0)
            {
                Debug.Log($"[AtlasGuard] {files.Length}개 아틀라스 정상");
                return;
            }

            // 깨진 아틀라스 복구 시도
            Debug.LogWarning($"[AtlasGuard] {broken.Count}개 깨진 아틀라스 → reimport 시도");

            foreach (var path in broken)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            // reimport 후 리패킹
            SpriteAtlasUtility.PackAllAtlases(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 재검증
            var stillBroken = new List<string>();
            foreach (var path in broken)
            {
                var asset = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (asset == null || asset.spriteCount == 0)
                {
                    stillBroken.Add(path);
                }
            }

            if (stillBroken.Count > 0)
            {
                var msg = "[AtlasGuard] reimport 후에도 복구 실패:\n"
                          + string.Join("\n", stillBroken);
                Debug.LogError(msg);

                if (Application.isBatchMode)
                    EditorApplication.Exit(1);

                throw new System.Exception(msg);
            }

            Debug.Log("[AtlasGuard] 깨진 아틀라스 복구 완료");
        }
    }
}
#endif