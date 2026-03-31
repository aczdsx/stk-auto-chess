using CookApps.AutoBattler;
using CookApps.AutoChess.View;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SynergyVfxConfigSO))]
public class SynergyVfxConfigSetup : Editor
{
    private static readonly (SynergyType type, string vfxName)[] ElementalEntries =
    {
        (SynergyType.FIRE,      "fx_common_synergy_fire"),
        (SynergyType.WATER,     "fx_common_synergy_water"),
        (SynergyType.LIGHTNING, "fx_common_synergy_lightning_01"),
        (SynergyType.EARTH,     "fx_common_synergy_ground"),
        (SynergyType.WIND,      "fx_common_synergy_wind"),
    };

    private static readonly (SynergyType type, string vfxName)[] AsterismEntries =
    {
        (SynergyType.NOBLESSE,      "fx_common_asterism_nb_Icon_01"),
        (SynergyType.TROUBLESHOOTER,"fx_common_asterism_ts_Icon_01"),
        (SynergyType.SUPERNOVA,     "fx_common_asterism_sn_Icon_01"),
    };

    private static readonly SynergyType[] VfxPendingTypes =
    {
    };

    private const string VfxBasePath = "Assets/_Project/Addressables/Remote/Prefabs/Fx/Common/";

    private Vector2 _scrollPos;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var entriesProp = serializedObject.FindProperty("_entries");

        EditorGUILayout.Space(4);
        entriesProp.isExpanded = EditorGUILayout.Foldout(entriesProp.isExpanded, $"Entries ({entriesProp.arraySize})", true);

        if (entriesProp.isExpanded)
        {
            EditorGUI.indentLevel++;
            int size = EditorGUILayout.DelayedIntField("Size", entriesProp.arraySize);
            if (size != entriesProp.arraySize)
                entriesProp.arraySize = size;

            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                var element = entriesProp.GetArrayElementAtIndex(i);
                var synergyTypeProp = element.FindPropertyRelative("SynergyType");
                string label = synergyTypeProp.enumDisplayNames[synergyTypeProp.enumValueIndex];

                EditorGUILayout.PropertyField(element, new GUIContent(label), true);
            }
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Setup Default Entries", GUILayout.Height(30)))
        {
            SetupDefaults();
        }
    }

    private void SetupDefaults()
    {
        var entriesProp = serializedObject.FindProperty("_entries");
        entriesProp.ClearArray();

        int entryIndex = 0;

        // 원소 시너지
        foreach (var (synergyType, vfxName) in ElementalEntries)
        {
            string prefabPath = VfxBasePath + vfxName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SynergyVfxConfig] Prefab not found: {prefabPath}");
                continue;
            }

            entriesProp.InsertArrayElementAtIndex(entryIndex);
            var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);
            entryProp.FindPropertyRelative("SynergyType").enumValueIndex = (int)synergyType;
            SetAssetReference(entryProp.FindPropertyRelative("AchieveVfx"), prefab);
            entryProp.FindPropertyRelative("AchievePosition").enumValueIndex = (int)SkillPosition.SKILL_MIDDLE;

            entryIndex++;
        }

        // 성군 시너지
        foreach (var (synergyType, vfxName) in AsterismEntries)
        {
            string prefabPath = VfxBasePath + vfxName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SynergyVfxConfig] Prefab not found: {prefabPath}");
                continue;
            }

            entriesProp.InsertArrayElementAtIndex(entryIndex);
            var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);
            entryProp.FindPropertyRelative("SynergyType").enumValueIndex = (int)synergyType;
            SetAssetReference(entryProp.FindPropertyRelative("AchieveVfx"), prefab);
            entryProp.FindPropertyRelative("AchievePosition").enumValueIndex = (int)SkillPosition.SKILL_MIDDLE;

            entryIndex++;
        }

        // VFX 미정 시너지 (빈 엔트리)
        foreach (var synergyType in VfxPendingTypes)
        {
            entriesProp.InsertArrayElementAtIndex(entryIndex);
            var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);
            entryProp.FindPropertyRelative("SynergyType").enumValueIndex = (int)synergyType;
            entryProp.FindPropertyRelative("AchievePosition").enumValueIndex = (int)SkillPosition.SKILL_MIDDLE;

            entryIndex++;
        }

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        Debug.Log($"[SynergyVfxConfig] {entryIndex} entries created.");
    }

    private static void SetAssetReference(SerializedProperty assetRefProp, GameObject prefab)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
        var assetGuidProp = assetRefProp.FindPropertyRelative("m_AssetGUID");
        if (assetGuidProp != null)
            assetGuidProp.stringValue = guid;
    }
}
