#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace CookApps.AutoBattler.Editor
{
    public static class CharacterVfxConfigExtractor
    {
        private static readonly string[] PrefabRoots =
        {
            "Assets/_Project/Addressables/Remote/SD/Characters",
            "Assets/_Project/Addressables/Remote/SD/Mob",
        };
        private const string OutputFolder = "Assets/_Project/Addressables/BuiltIn/Data/CharacterVfxConfigs";
        private const string SpecDataPath = "Assets/OriginalSpecData.json";
        private const string LanguageDataPath = "Assets/OriginalSpecLanguage.json";

        [MenuItem("Tools/AutoChess/Extract Character Vfx Configs")]
        public static void Extract()
        {
            if (!Directory.Exists(OutputFolder))
                Directory.CreateDirectory(OutputFolder);

            var nameMap = BuildPrefabIdToNameMap();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", PrefabRoots);

            int created = 0;
            int skipped = 0;

            foreach (var guid in prefabGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fileName = Path.GetFileNameWithoutExtension(path);

                // InGame_{prefab_id} 패턴만 처리
                if (!fileName.StartsWith("InGame_"))
                    continue;

                var prefabIdStr = fileName.Substring("InGame_".Length);
                if (!int.TryParse(prefabIdStr, out int prefabId))
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var view = prefab.GetComponentInChildren<SpriteCharacterView>();
                if (view == null) continue;

                // SerializedObject로 기존 필드 값 읽기
                var so = new SerializedObject(view);
                var projectileProp = so.FindProperty("_projectilePrefab");
                var skillEffectsProp = so.FindProperty("_skillEffectPrefabs");

                bool hasProjectile = projectileProp.objectReferenceValue != null;
                bool hasSkillEffects = skillEffectsProp.arraySize > 0;

                if (!hasProjectile && !hasSkillEffects)
                {
                    skipped++;
                    continue;
                }

                // 캐릭터명 결정
                string charName = nameMap.TryGetValue(prefabId, out var n) ? SanitizeFileName(n) : "";
                string assetName = string.IsNullOrEmpty(charName)
                    ? $"CharacterVfxConfig_{prefabIdStr}"
                    : $"CharacterVfxConfig_{prefabIdStr}_{charName}";

                string assetPath = $"{OutputFolder}/{assetName}.asset";

                // 기존 SO가 있으면 로드, 없으면 생성
                var config = AssetDatabase.LoadAssetAtPath<CharacterVfxConfigSO>(assetPath);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<CharacterVfxConfigSO>();
                    AssetDatabase.CreateAsset(config, assetPath);
                }

                // SO에 데이터 복사
                var configSO = new SerializedObject(config);
                configSO.FindProperty("_sourcePrefab").objectReferenceValue = prefab;
                var configProjectile = configSO.FindProperty("_projectilePrefab");
                var configSkillEffects = configSO.FindProperty("_skillEffectPrefabs");

                configProjectile.objectReferenceValue = projectileProp.objectReferenceValue;

                configSkillEffects.arraySize = skillEffectsProp.arraySize;
                for (int i = 0; i < skillEffectsProp.arraySize; i++)
                {
                    var srcElem = skillEffectsProp.GetArrayElementAtIndex(i);
                    var dstElem = configSkillEffects.GetArrayElementAtIndex(i);

                    dstElem.FindPropertyRelative("prefab").objectReferenceValue =
                        srcElem.FindPropertyRelative("prefab").objectReferenceValue;
                    dstElem.FindPropertyRelative("skillPosition").intValue =
                        srcElem.FindPropertyRelative("skillPosition").intValue;
                    dstElem.FindPropertyRelative("followable").boolValue =
                        srcElem.FindPropertyRelative("followable").boolValue;
                    dstElem.FindPropertyRelative("persistent").boolValue =
                        srcElem.FindPropertyRelative("persistent").boolValue;
                }

                configSO.ApplyModifiedPropertiesWithoutUndo();

                // 프리팹의 _vfxConfig에 SO 연결
                var vfxConfigProp = so.FindProperty("_vfxConfig");
                vfxConfigProp.objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 프리팹 저장
                PrefabUtility.SavePrefabAsset(prefab);

                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CharacterVfxConfigExtractor] 완료: {created}개 SO 생성/업데이트, {skipped}개 스킵 (VFX 데이터 없음)");
        }

        private static Dictionary<int, string> BuildPrefabIdToNameMap()
        {
            var map = new Dictionary<int, string>();

            // 언어 데이터 로드
            var langMap = new Dictionary<string, string>();
            if (File.Exists(LanguageDataPath))
            {
                var langRoot = JObject.Parse(File.ReadAllText(LanguageDataPath));
                foreach (var prop in langRoot.Properties())
                {
                    if (prop.Value is JArray arr)
                    {
                        foreach (var item in arr)
                        {
                            string key = item["key"]?.Value<string>() ?? "";
                            string kr = item["kr"]?.Value<string>() ?? "";
                            if (!string.IsNullOrEmpty(key) && !langMap.ContainsKey(key))
                                langMap[key] = kr;
                        }
                    }
                }
            }

            if (!File.Exists(SpecDataPath)) return map;

            var root = JObject.Parse(File.ReadAllText(SpecDataPath));

            // CharacterInfo + MonsterInfo 모두 처리
            string[] tables = { "CharacterInfo", "MonsterInfo" };
            foreach (var tableName in tables)
            {
                if (root[tableName] is JArray arr)
                {
                    foreach (var item in arr)
                    {
                        int prefabId = item["prefab_id"]?.Value<int>() ?? 0;
                        string nameToken = item["name_token"]?.Value<string>() ?? "";

                        if (prefabId == 0 || map.ContainsKey(prefabId))
                            continue;

                        if (langMap.TryGetValue(nameToken, out var krName) && !string.IsNullOrEmpty(krName))
                            map[prefabId] = krName;
                    }
                }
            }

            return map;
        }

        private static string SanitizeFileName(string name)
        {
            // 파일명에 사용할 수 없는 문자 제거
            return Regex.Replace(name, @"[<>:""/\\|?*\s]", "");
        }
    }
}
#endif
