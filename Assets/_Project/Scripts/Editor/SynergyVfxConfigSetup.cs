using CookApps.AutoBattler;
using CookApps.AutoChess.View;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SynergyVfxConfigSO))]
public class SynergyVfxConfigSetup : Editor
{
    private static readonly (SynergyType type, string baseName)[] ElementalEntries =
    {
        (SynergyType.FIRE,      "fx_common_synergy_fire"),
        (SynergyType.WATER,     "fx_common_synergy_water"),
        (SynergyType.LIGHTNING, "fx_common_synergy_lightning_01"),
        (SynergyType.EARTH,     "fx_common_synergy_ground"),
        (SynergyType.WIND,      "fx_common_synergy_wind"),
    };

    private static readonly string[] ElementalTierSuffixes = { "", "_02", "_03" };

    private static readonly (SynergyType type, string vfxName)[] AsterismEntries =
    {
        (SynergyType.NOBLESSE,      "fx_common_asterism_nb_Icon_01"),
        (SynergyType.TROUBLESHOOTER,"fx_common_asterism_ts_Icon_01"),
        (SynergyType.SUPERNOVA,     "fx_common_asterism_sn_Icon_01"),
    };

    private const string VfxBasePath = "Assets/_Project/Addressables/Remote/Prefabs/Fx/Common/";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

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
        foreach (var (synergyType, baseName) in ElementalEntries)
        {
            entriesProp.InsertArrayElementAtIndex(entryIndex);
            var entryProp = entriesProp.GetArrayElementAtIndex(entryIndex);
            entryProp.FindPropertyRelative("SynergyType").enumValueIndex = (int)synergyType;

            var tiersProp = entryProp.FindPropertyRelative("Tiers");
            tiersProp.ClearArray();

            string stem = baseName;
            if (stem.EndsWith("_01"))
                stem = stem.Substring(0, stem.Length - 3);

            for (int tier = 0; tier < 3; tier++)
            {
                string vfxName = tier == 0 ? baseName : stem + ElementalTierSuffixes[tier];
                string prefabPath = VfxBasePath + vfxName + ".prefab";

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[SynergyVfxConfig] Prefab not found: {prefabPath}");
                    continue;
                }

                tiersProp.InsertArrayElementAtIndex(tiersProp.arraySize);
                var tierProp = tiersProp.GetArrayElementAtIndex(tiersProp.arraySize - 1);

                tierProp.FindPropertyRelative("TierIndex").intValue = tier;
                SetAssetReference(tierProp.FindPropertyRelative("AchieveVfx"), prefab);
                tierProp.FindPropertyRelative("AchievePosition").enumValueIndex = (int)SkillPosition.SKILL_MIDDLE;
                tierProp.FindPropertyRelative("AchieveFollowable").boolValue = true;
            }
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

            var tiersProp = entryProp.FindPropertyRelative("Tiers");
            tiersProp.ClearArray();
            tiersProp.InsertArrayElementAtIndex(0);
            var tierProp = tiersProp.GetArrayElementAtIndex(0);

            tierProp.FindPropertyRelative("TierIndex").intValue = 0;
            SetAssetReference(tierProp.FindPropertyRelative("AchieveVfx"), prefab);
            tierProp.FindPropertyRelative("AchievePosition").enumValueIndex = (int)SkillPosition.SKILL_MIDDLE;
            tierProp.FindPropertyRelative("AchieveFollowable").boolValue = true;

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
