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
using UnityEditor.UI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CustomEditor(typeof(PGButton))]
    public class PGButtonEditor : ButtonEditor
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
                serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
    }
}

#endif
