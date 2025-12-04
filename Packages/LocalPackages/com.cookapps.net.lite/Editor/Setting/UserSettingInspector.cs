/*
* Copyright (c) CookApps.
*/

#if DISABLE
using System;
using System.Collections;
using System.IO;
using CookApps.Package.Window.Editor;
using UnityEditor;
using UnityEngine;

namespace CookApps.NetLite.Editor.Setting
{
    [CustomEditor(typeof(UserSetting))]
    internal class UserSettingInspector : UnityEditor.Editor
    {
        // 스타일/콘텐츠는 여러 인스턴스에서 재사용하도록 static으로 유지
        private static GUIContent _sUpdateButtonContent;
        private static GUIStyle _sButtonStyle;
        // 인스턴스별 지연 초기화 플래그
        private bool _initialized;

        [InitializeOnLoadMethod]
        private static void AddToCookAppsPackageWindow()
        {
            string rootPath = Path.GetFullPath(".");
            string assetsPath = UserSetting.FilePath;
            string fullPath = Path.Combine(rootPath, assetsPath);
            Action onInitialize = null;

            if (File.Exists(fullPath) == false)
            {
                onInitialize = UserSetting.CreateSetting;
            }

            CookAppsPackageWindow.Add("Net-Lite", assetsPath, onInitialize);
        }

        private static IEnumerator InitCoroutine()
        {
            UserSettingInspector[] windows = Resources.FindObjectsOfTypeAll<UserSettingInspector>();
            foreach (UserSettingInspector wnd in windows)
            {
                DestroyImmediate(wnd);
            }

            if (File.Exists(UserSetting.FilePath))
            {
                ScriptableObject asset;
                do
                {
                    asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(UserSetting.FilePath);
                    if (asset == null)
                    {
                        yield return null;
                    }
                } while (asset == null);
            }

            var instance = UserSetting.GetOrCreateAsset();
            UnityEditor.Editor window = CreateEditor(instance, typeof(UserSettingInspector));
            CookAppsPackageWindow.SetContentVisualElement(window.CreateInspectorGUI());
        }

        // Unity API가 준비되지 않았을 수 있으므로 안전하게 지연 초기화합니다.
        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _sUpdateButtonContent ??= new GUIContent(
                "API 업데이트",
                ".proto 파일을 내려받아 .cs 파일로 변환하고, gRPC 서비스 및 모듈 코드를 생성합니다.");

            _sButtonStyle ??= new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
            };

            _initialized = true;
        }

        public override void OnInspectorGUI()
        {
            EnsureInitialized();

            DrawTop();
            GUILayout.Space(10);
            DrawAsset();
            GUILayout.Space(10);
            if (GUILayout.Button(_sUpdateButtonContent, _sButtonStyle, GUILayout.Height(40)))
            {

            }
        }

        /// <summary>
        /// UserSetting ScriptableObject의 프로퍼티를 인스펙터에 표시하고, 변경 시 저장 처리까지 담당합니다.
        /// </summary>
        private void DrawAsset()
        {
            // 인스펙터와 오브젝트 동기화
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            // 모든 프로퍼티 순회
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); // 첫 번째 프로퍼티로 이동
            while (property.NextVisible(false))
            {
                // 각 프로퍼티를 인스펙터에 표시
                EditorGUILayout.PropertyField(property, true);
            }

            /// 변경 사항이 있을 때만 저장
            if (EditorGUI.EndChangeCheck())
            {
                // SerializedObject에 변경 적용
                serializedObject.ApplyModifiedProperties();

                // 변경된 내용을 에셋에 저장하도록 dirty 플래그 설정 및 저장
                EditorUtility.SetDirty(target);
                if (AssetDatabase.Contains(target))
                {
                    AssetDatabase.SaveAssets();
                }
            }
        }

        /// <summary>
        /// 상단에 IDL 저장소 및 서비스 표로 이동하는 버튼을 그립니다.
        /// </summary>
        private static void DrawTop()
        {
            GUILayout.BeginHorizontal();
            // IDL 저장소 및 서비스 표로 이동하는 버튼
            if (GUILayout.Button("IDL 저장소 & 서비스 표", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/cookapps-devops/hive-grpc-IDL");
            }
            GUILayout.EndHorizontal();
        }
    }
}

#endif
