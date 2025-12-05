#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor
{
    internal class ProtoBufCleanupSetting : ScriptableObject
    {
        private const string DefaultAssetPath = "Assets/_Project/Scripts/gRPC/bm013-grpc/Editor/ProtoBufCleanupSetting.asset";

        [Tooltip("삭제 대상이 생성되는 루트 경로 (Asset 경로)")]
        public string targetDir = "Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp";

        [Tooltip("삭제할 파일 이름 목록 (.cs 확장자 생략 가능, * 와일드카드 사용 가능)")]
        public List<string> removeFileNames = new();

        public static ProtoBufCleanupSetting LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ProtoBufCleanupSetting>(DefaultAssetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = CreateInstance<ProtoBufCleanupSetting>();
            AssetDatabase.CreateAsset(asset, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static string AssetPath => DefaultAssetPath;
    }
}
#endif
