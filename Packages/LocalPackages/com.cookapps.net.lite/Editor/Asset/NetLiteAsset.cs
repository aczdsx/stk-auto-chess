/*
* Copyright (c) CookApps.
*/

using CookApps.Inspector;
using UnityEditor;
using UnityEngine;

namespace CookApps.NetLite.Editor.Asset
{
    [FilePath("Assets/CookApps/Editor/NetLiteAsset.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class NetLiteAsset : ScriptableObject
    {
        [SerializeField]
        private string _idlProjectName;

        [SerializeField][Folder]
        private string _targetDir;

        public string TargetDir => _targetDir;
        public string IdlProjectName => _idlProjectName;

        public static NetLiteAsset GetOrCreateAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<NetLiteAsset>(AssetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = CreateInstance<NetLiteAsset>();

            string dir = System.IO.Path.GetDirectoryName(AssetPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir!);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CreateAsset(asset, AssetPath);
            return asset;
        }

        public static string AssetPath => "Assets/CookApps/Editor/NetLiteAsset.asset";

        /// NetLiteAsset의 필드 값들이 유효한지 검증합니다.
        public bool IsValid()
        {
            var isValid = true;

            if (string.IsNullOrEmpty(IdlProjectName))
            {
                Debug.LogError("NetLiteAsset: IdlProjectName 값이 올바르지 않습니다.");
                isValid = false;
            }

            if (string.IsNullOrEmpty(TargetDir))
            {
                Debug.LogError("NetLiteAsset: TargetDir 값이 올바르지 않습니다.");
                isValid = false;
            }

            return isValid;
        }
    }
}
