#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CustomEditor(typeof(SceneDatabase))]
    public class SceneDataEditor : Editor
    {
        private SceneDatabase origin;
        private SerializedProperty list;

        private Object obj;

        private void Awake()
        {
            origin = (SceneDatabase) target;
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

            obj = EditorGUILayout.ObjectField(obj, typeof(SceneAsset), false);
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
