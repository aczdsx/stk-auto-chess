#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    internal class ProtoImporterSetting : ScriptableObject
    {
        internal const string AssetPath = "Assets/_Project/Scripts/Editor/ProtoImporter/ProtoImporterSetting.asset";

        [Header("Import 설정")]
        [Tooltip("IDL 저장소의 프로젝트 이름")]
        [SerializeField] private string _idlProjectName = "bm013-grpc";

        [Tooltip("생성된 코드가 저장될 경로 (Asset 경로)")]
        [SerializeField] private string _targetDir = "Assets/_Project/Scripts/gRPC/bm013-grpc";

        [Tooltip("포함할 서비스 목록 (비어있으면 전체 포함)")]
        [SerializeField] private string[] _includeServices = {
            "auth", "lobby", "spec", "level", "player", "player_data", "inventory_flow", "shop"
        };

        [Tooltip("제외할 서비스 목록")]
        [SerializeField] private string[] _excludeServices = { "health" };

        [Header("Cleanup 설정")]
        [Tooltip("Cleanup 대상 폴더 (Asset 경로). 비어있으면 TargetDir/Generated/CSharp 사용")]
        [SerializeField] private string _cleanupTargetDir = "Assets/_Project/Scripts/gRPC/bm013-grpc/Generated/CSharp";

        [Tooltip("삭제할 파일 이름 목록 (.cs 확장자 생략 가능, * 와일드카드 지원)")]
        [SerializeField] private List<string> _removeFileNames = new();

        public string IdlProjectName => _idlProjectName;
        public string TargetDir => _targetDir;
        public string[] IncludeServices => _includeServices;
        public string[] ExcludeServices => _excludeServices;
        public string CleanupTargetDir =>
            string.IsNullOrEmpty(_cleanupTargetDir) ? $"{_targetDir}/Generated/CSharp" : _cleanupTargetDir;
        public List<string> RemoveFileNames => _removeFileNames;

        public bool IsValid()
        {
            bool valid = true;
            if (string.IsNullOrEmpty(_idlProjectName))
            {
                Debug.LogError("[ProtoImporter] IDL Project Name이 비어있습니다.");
                valid = false;
            }
            if (string.IsNullOrEmpty(_targetDir))
            {
                Debug.LogError("[ProtoImporter] Target Dir이 비어있습니다.");
                valid = false;
            }
            return valid;
        }

        public static ProtoImporterSetting GetOrCreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ProtoImporterSetting>(AssetPath);
            if (asset != null)
                return asset;

            asset = CreateInstance<ProtoImporterSetting>();
            string dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir!);
                AssetDatabase.Refresh();
            }
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
#endif
