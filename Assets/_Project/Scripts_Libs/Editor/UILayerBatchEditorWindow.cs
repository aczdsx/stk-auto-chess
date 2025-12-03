using System.Linq;
using UnityEditor;
using UnityEngine;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.Editor
{
    public class UILayerBatchEditorWindow : EditorWindow
    {
        private SerializedObject serializedLayers;
        private SerializedProperty uiLayerTypeProp;
        private SerializedProperty baseAnimatorProp;
        private SerializedProperty preloadAddressablesProp;

        [MenuItem("Tools/UILayer Batch Edit")]
        private static void Open()
        {
            GetWindow<UILayerBatchEditorWindow>("UILayer Batch Edit");
        }

        private void OnEnable()
        {
            RefreshSelection();
        }

        private void OnSelectionChange()
        {
            RefreshSelection();
            Repaint();
        }

        private void RefreshSelection()
        {
            var layers = Selection.GetFiltered<UILayer>(SelectionMode.Editable | SelectionMode.DeepAssets);
            if (layers.Length == 0)
            {
                serializedLayers = null;
                uiLayerTypeProp = null;
                baseAnimatorProp = null;
                preloadAddressablesProp = null;
                return;
            }

            serializedLayers = new SerializedObject(layers.Cast<Object>().ToArray());
            uiLayerTypeProp = serializedLayers.FindProperty("uiLayerType");
            baseAnimatorProp = serializedLayers.FindProperty("baseAnimator");
            preloadAddressablesProp = serializedLayers.FindProperty("preloadAddressables");
        }

        private void OnGUI()
        {
            if (serializedLayers == null)
            {
                EditorGUILayout.HelpBox("씬 오브젝트나 프리팹에서 UILayer 파생 컴포넌트를 하나 이상 선택하세요.", MessageType.Info);
                return;
            }

            serializedLayers.Update();

            EditorGUILayout.LabelField($"Editing {serializedLayers.targetObjects.Length} UILayer component(s)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(uiLayerTypeProp);
            EditorGUILayout.PropertyField(baseAnimatorProp);
            EditorGUILayout.PropertyField(preloadAddressablesProp, true);

            serializedLayers.ApplyModifiedProperties();
        }
    }
}
