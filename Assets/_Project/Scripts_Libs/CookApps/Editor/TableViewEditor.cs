using UnityEditor;
using UnityEditor.UI;

namespace CookApps.TeamBattle.UI.Editors
{
    [CustomEditor(typeof(TableView), true)]
    [CanEditMultipleObjects]
    public class TableViewEditor : ScrollRectEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemVerticalAlign"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemHorizontalAlign"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loadAll"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
