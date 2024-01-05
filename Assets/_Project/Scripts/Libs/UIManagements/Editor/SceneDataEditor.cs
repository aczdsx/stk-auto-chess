#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine.UIElements;
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

            obj = EditorGUILayout.ObjectField(obj, typeof(SceneAsset), allowSceneObjects: false);
            if (GUILayout.Button("Bind"))
                origin.Add(obj);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}
#endif
