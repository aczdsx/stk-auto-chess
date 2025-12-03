using UnityEditor;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.Editor
{
    [CustomEditor(typeof(UILayer), true)] // 파생 클래스까지 사용
    [CanEditMultipleObjects]              // 다중 선택 허용
    public class UILayerEditor : UnityEditor.Editor
    {
        SerializedProperty uiLayerType;
        SerializedProperty baseAnimator;
        SerializedProperty preloadAddressables;

        void OnEnable()
        {
            uiLayerType = serializedObject.FindProperty("uiLayerType");
            baseAnimator = serializedObject.FindProperty("baseAnimator");
            preloadAddressables = serializedObject.FindProperty("preloadAddressables");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(uiLayerType);
            EditorGUILayout.PropertyField(baseAnimator);
            EditorGUILayout.PropertyField(preloadAddressables, true);
            DrawPropertiesExcluding(serializedObject, "m_Script",
                "uiLayerType", "baseAnimator", "preloadAddressables"); // 파생 필드들도 출력
            serializedObject.ApplyModifiedProperties();
        }
    }
}