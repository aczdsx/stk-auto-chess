#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CustomEditor(typeof(UIDatabase))]
    public class UIDataEditor : Editor
    {
        private UIDatabase origin;
        private SerializedProperty list;

        private Object obj;

        private void Awake()
        {
            origin = (UIDatabase) target;
        }

        private void OnEnable()
        {
            list = serializedObject.FindProperty("list");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(list, GUIContent.none);
            list.isExpanded = true;

            EditorGUILayout.Space(20);

            obj = EditorGUILayout.ObjectField(obj, typeof(GameObject), false);
            if (GUILayout.Button("Bind"))
            {
                origin.Add(obj);
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}

#endif
