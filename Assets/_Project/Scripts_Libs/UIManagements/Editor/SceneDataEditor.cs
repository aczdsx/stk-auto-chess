#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
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
                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                SerializedProperty property = list.GetArrayElementAtIndex(index);
                SerializedProperty nameProperty = property.FindPropertyRelative("sceneName");
                nameProperty.stringValue = obj.name;
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
