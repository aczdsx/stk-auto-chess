#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;

namespace CookApps.TeamBattle.UIManagements
{
    [CustomEditor(typeof(CAButton))]
    public class CAButtonEditor : ButtonEditor
    {
        private SerializedProperty isBlockDragProperty;
        private SerializedProperty defaultClickSoundTypeProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            isBlockDragProperty = serializedObject.FindProperty("isBlockDrag");
            defaultClickSoundTypeProperty = serializedObject.FindProperty("defaultClickSoundType");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(isBlockDragProperty);
            EditorGUILayout.PropertyField(defaultClickSoundTypeProperty);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            base.OnInspectorGUI();
        }
    }
}

#endif
