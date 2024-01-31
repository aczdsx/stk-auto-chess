#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.UIManagements
{
    [CustomEditor(typeof(UILayerDatabase))]
    public class UILayerDatabaseEditor : Editor
    {
        private UILayerDatabase origin;
        private SerializedProperty list;

        private Object obj;

        private void Awake()
        {
            origin = (UILayerDatabase) target;
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
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                SerializedProperty property = list.GetArrayElementAtIndex(index);
                SceneUILayerManager.UILayerType layerType;
                if (obj.name.Contains("Popup"))
                {
                    layerType = SceneUILayerManager.UILayerType.Popup;
                }
                else if (obj.name.Contains("Modal"))
                {
                    layerType = SceneUILayerManager.UILayerType.Modal;
                }
                else
                {
                    layerType = SceneUILayerManager.UILayerType.Cover;
                }

                SerializedProperty nameProperty = property.FindPropertyRelative("name");
                nameProperty.stringValue = obj.name;
                SerializedProperty layerTypeProperty = property.FindPropertyRelative("layerType");
                layerTypeProperty.enumValueIndex = (int) layerType;
                SerializedProperty addressableNameProperty = property.FindPropertyRelative("addressableName");
                AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
                if (settings != null)
                {
                    // string addressableName = settings.FindAssetEntry(guid).address;
                    foreach (AddressableAssetGroup group in settings.groups)
                    {
                        var results = new List<AddressableAssetEntry>();
                        group.GatherAllAssets(results, true, true, true);
                        var isFound = false;
                        foreach (AddressableAssetEntry result in results)
                        {
                            if (result.AssetPath == AssetDatabase.GetAssetPath(obj))
                            {
                                addressableNameProperty.stringValue = result.address;
                                isFound = true;
                                break;
                            }
                        }

                        if (isFound)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    addressableNameProperty.stringValue = string.Empty;
                }
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}

#endif
