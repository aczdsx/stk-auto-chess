#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CookApps.Editor.ProtoImporter
{
    [CustomEditor(typeof(ProtoImporterSetting))]
    internal class ProtoImporterSettingEditor : UnityEditor.Editor
    {
        private GUIStyle _buttonStyle;

        private GUIStyle ButtonStyle =>
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                fixedHeight = 30,
            };

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }

            EditorGUILayout.Space(10);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("API 업데이트", ButtonStyle))
            {
                ProtoImporter.RunFromSetting();
            }

            if (GUILayout.Button("저장소 리스트", ButtonStyle))
            {
                Application.OpenURL("https://github.com/cookapps-devops/hive-grpc-IDL");
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Cleanup 수동 실행", GUILayout.Height(24)))
            {
                var setting = (ProtoImporterSetting)target;
                ProtoImporter.CleanupGeneratedFiles(setting);
            }
        }
    }
}
#endif
