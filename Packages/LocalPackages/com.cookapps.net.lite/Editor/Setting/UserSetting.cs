/*
 * Copyright (c) CookApps.
 */

using System.Linq;
using CookApps.Inspector;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookApps.NetLite.Editor.Setting
{
    internal class UserSetting : ScriptableObject
    {
        [Title("설정 (매개변수 또는 버튼에 마우스를 대면 설명이 표시됩니다.)")]
        [Folder]
        [Tooltip("루트 경로.\ngRPC API 코드가 생성될 경로를 지정하세요.\n처음 사용한다면 새 폴더를 만들어 지정하세요.")]
        public string RootPath;

        [Tooltip("IDL 프로젝트명.\nIDL 저장소에 있는 본인의 프로젝트 폴더명을 적으세요.\n프로젝트에 특화된 서버 API를 사용할 수 있게 합니다.")]
        public string IDLProjectName = "hive-grpc";

        public static UserSetting GetOrCreateAsset()
        {
            Object existObj = InternalEditorUtility.LoadSerializedFileAndForget(FilePath).FirstOrDefault();
            if(existObj is UserSetting asset)
                return asset;
            asset = CreateInstance<UserSetting>();
            AssetDatabase.CreateAsset(asset, FilePath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static void CreateSetting()
        {
            GetOrCreateAsset();
        }

        public const string FilePath = "Assets/CookApps/Editor/CookAppsGRPC.asset";
    }
}
