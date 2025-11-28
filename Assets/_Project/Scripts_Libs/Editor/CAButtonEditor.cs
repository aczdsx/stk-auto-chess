using UnityEditor;
using UnityEditor.UI;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.Editor
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
