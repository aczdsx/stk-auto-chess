/*
* Copyright (c) CookApps.
*/

using UnityEditor;
using UnityEngine;

namespace CookApps.NetLite.Editor.Asset
{
    [CustomEditor(typeof(NetLiteAsset))]
    internal class NetLiteAssetEditor : UnityEditor.Editor
    {
        private GUIStyle _buttonStyle;

        private GUIStyle ButtonStyle
        {
            get
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 16,
                    fontStyle =  FontStyle.Bold,
                    fixedHeight = 30,
                };
                return _buttonStyle;
            }
        }

        public override void OnInspectorGUI()
        {
            // 변경 감지 시작
            EditorGUI.BeginChangeCheck();

            // 기본 인스펙터 그리기 (필드 자동)
            base.OnInspectorGUI();

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if(GUILayout.Button("API 업데이트", ButtonStyle))
            {
                ProtoCSharpGenerator.UserGenerateCSharpFromProtoFilesFromAsset();
            }

            if(GUILayout.Button("저장소 리스트", ButtonStyle))
            {
                Application.OpenURL("https://github.com/cookapps-devops/hive-grpc-IDL");
            }

            // 가로 레이아웃 끝
            GUILayout.EndHorizontal();

#if TECH_ONLY && TECH_PKG_NET_LITE // 패키지 제작의 경우만 보이게
            EditorGUILayout.Space(5);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
            if(GUILayout.Button("[Tech Only] hive-grpc 업데이트", ButtonStyle))
            {
                ProtoCSharpGenerator.InternalGenerateCSharpFromProtoFiles();
            }
#endif
            // 값이 변경되었으면
            if (!EditorGUI.EndChangeCheck())
            {
                return;
            }

            // 저장
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
        }
    }
}
