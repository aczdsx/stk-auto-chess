#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// 에디터에서 SpecData JSON을 직접 파싱하여 캐릭터/몬스터 정보를 제공
    /// </summary>
    [InitializeOnLoad]
    public static class InGameTestSpecDataHelper
    {
        static InGameTestSpecDataHelper()
        {
            Reload();
        }
        private static readonly string SpecDataPath = "Assets/OriginalSpecData.json";
        private static readonly string LanguageDataPath = "Assets/OriginalSpecLanguage.json";

        private static List<LanguageEntry> _languages;
        private static Dictionary<string, string> _languageMap; // key -> kr 매핑
        private static List<CharacterEntry> _characters;
        private static List<CharacterEntry> _monsters;
        private static List<StageEntry> _stages;
        private static List<StageMonsterEntry> _stageMonsters;
        private static bool _isLoaded;

        public struct CharacterEntry
        {
            public int Id;
            public string NameToken;
            public string DisplayName; // id와 이름을 조합한 표시용 문자열
        }

        public struct LanguageEntry
        {
            public int Id;
            public string token_key;
            public string language_kr;
        }

        public struct StageEntry
        {
            public int StageId;
            public int ChapterId;
            public int StageNumber;
            public int MapWidth;
            public int MapHeight;
            public string DisplayName; // "{chapter_id}-{stage_number}" 형식
        }

        public struct StageMonsterEntry
        {
            public int Id;
            public int ChapterId;
            public int StageNumber;
            public int MonsterId;
            public int MonsterLevel;
            public int CoordX;
            public int CoordY;
            public float MultipleAtk;
            public float MultipleHp;
        }

        public static List<CharacterEntry> Characters
        {
            get
            {
                EnsureLoaded();
                return _characters;
            }
        }

        public static List<CharacterEntry> Monsters
        {
            get
            {
                EnsureLoaded();
                return _monsters;
            }
        }

        public static List<StageEntry> Stages
        {
            get
            {
                EnsureLoaded();
                return _stages;
            }
        }

        public static void Reload()
        {
            _isLoaded = false;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_isLoaded) return;

            _languages = new List<LanguageEntry>();
            _languageMap = new Dictionary<string, string>();
            _characters = new List<CharacterEntry>();
            _monsters = new List<CharacterEntry>();
            _stages = new List<StageEntry>();
            _stageMonsters = new List<StageMonsterEntry>();

            // OriginalSpecLanguage.json 로드 (HEROES_NAME_, MONSTER_NAME_ 매핑)
            LoadLanguageData();

            if (!File.Exists(SpecDataPath))
            {
                Debug.LogWarning($"[InGameTestSpecDataHelper] SpecData not found: {SpecDataPath}");
                _isLoaded = true;
                return;
            }

            try
            {
                string json = File.ReadAllText(SpecDataPath);
                var root = JObject.Parse(json);

                if (root["Language"] is JArray languageArray)
                {
                    foreach (var item in languageArray)
                    {
                        var entry = new LanguageEntry
                        {
                            Id = item["id"]?.Value<int>() ?? 0,
                            token_key = item["token_key"]?.Value<string>() ?? "",
                            language_kr = item["language_kr"]?.Value<string>() ?? ""
                        };
                        _languages.Add(entry);
                    }
                }

                // CharacterInfo 파싱
                if (root["CharacterInfo"] is JArray characterArray)
                {
                    foreach (var item in characterArray)
                    {
                        var entry = new CharacterEntry
                        {
                            Id = item["id"]?.Value<int>() ?? 0,
                            NameToken = item["name_token"]?.Value<string>() ?? "",
                        };
                        // name_token을 키로 사용하여 이름 조회
                        entry.DisplayName = $"[{entry.Id}] {GetNameByToken(entry.NameToken)}";
                        _characters.Add(entry);
                    }
                }

                // MonsterInfo 파싱
                if (root["MonsterInfo"] is JArray monsterArray)
                {
                    foreach (var item in monsterArray)
                    {
                        var entry = new CharacterEntry
                        {
                            Id = item["id"]?.Value<int>() ?? 0,
                            NameToken = item["name_token"]?.Value<string>() ?? "",
                        };
                        // name_token을 키로 사용하여 이름 조회
                        entry.DisplayName = $"[{entry.Id}] {GetNameByToken(entry.NameToken)}";
                        _monsters.Add(entry);
                    }
                }

                // StageInfo 파싱
                if (root["StageInfo"] is JArray stageArray)
                {
                    foreach (var item in stageArray)
                    {
                        var entry = new StageEntry
                        {
                            StageId = item["stage_id"]?.Value<int>() ?? 0,
                            ChapterId = item["chapter_id"]?.Value<int>() ?? 0,
                            StageNumber = item["stage_number"]?.Value<int>() ?? 0,
                            MapWidth = 5,
                            MapHeight = 7
                        };

                        // map_size 파싱 (예: "5,7")
                        string mapSize = item["map_size"]?.Value<string>() ?? "5,7";
                        var sizeParts = mapSize.Split(',');
                        if (sizeParts.Length >= 2)
                        {
                            int.TryParse(sizeParts[0], out entry.MapWidth);
                            int.TryParse(sizeParts[1], out entry.MapHeight);
                        }

                        entry.DisplayName = $"{entry.ChapterId}-{entry.StageNumber}";
                        _stages.Add(entry);
                    }
                }

                // StageMonster 파싱
                if (root["StageMonster"] is JArray stageMonsterArray)
                {
                    foreach (var item in stageMonsterArray)
                    {
                        var entry = new StageMonsterEntry
                        {
                            Id = item["id"]?.Value<int>() ?? 0,
                            ChapterId = item["chapter_id"]?.Value<int>() ?? 0,
                            StageNumber = item["stage_number"]?.Value<int>() ?? 0,
                            MonsterId = item["monster_id"]?.Value<int>() ?? 0,
                            MonsterLevel = item["monster_lv"]?.Value<int>() ?? 1,
                            MultipleAtk = item["multiple_atk"]?.Value<float>() ?? 1f,
                            MultipleHp = item["multiple_hp"]?.Value<float>() ?? 1f,
                            CoordX = 0,
                            CoordY = 0
                        };

                        // coordinate 파싱 (예: "2,5")
                        string coord = item["coordinate"]?.Value<string>() ?? "0,0";
                        var coordParts = coord.Split(',');
                        if (coordParts.Length >= 2)
                        {
                            int.TryParse(coordParts[0], out entry.CoordX);
                            int.TryParse(coordParts[1], out entry.CoordY);
                        }

                        _stageMonsters.Add(entry);
                    }
                }

                // ID 순으로 정렬
                _characters.Sort((a, b) => a.Id.CompareTo(b.Id));
                _monsters.Sort((a, b) => a.Id.CompareTo(b.Id));
                // 스테이지는 챕터 → 스테이지 순으로 정렬
                _stages.Sort((a, b) =>
                {
                    int chapterCompare = a.ChapterId.CompareTo(b.ChapterId);
                    return chapterCompare != 0 ? chapterCompare : a.StageNumber.CompareTo(b.StageNumber);
                });

                _isLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[InGameTestSpecDataHelper] Failed to parse SpecData: {e.Message}");
                _isLoaded = true;
            }
        }

        /// <summary>
        /// OriginalSpecLanguage.json에서 언어 데이터 로드
        /// </summary>
        private static void LoadLanguageData()
        {
            if (!File.Exists(LanguageDataPath))
            {
                Debug.LogWarning($"[InGameTestSpecDataHelper] Language data not found: {LanguageDataPath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(LanguageDataPath);
                var root = JObject.Parse(json);

                // 여러 카테고리에서 key-kr 매핑 추출
                foreach (var property in root.Properties())
                {
                    if (property.Value is JArray array)
                    {
                        foreach (var item in array)
                        {
                            string key = item["key"]?.Value<string>() ?? "";
                            string kr = item["kr"]?.Value<string>() ?? "";
                            if (!string.IsNullOrEmpty(key) && !_languageMap.ContainsKey(key))
                            {
                                _languageMap[key] = kr;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[InGameTestSpecDataHelper] Failed to parse Language data: {e.Message}");
            }
        }

        /// <summary>
        /// name_token 키로 이름 조회 (HEROES_NAME_xxx, MONSTER_NAME_xxx 등)
        /// </summary>
        private static string GetNameByToken(string nameToken)
        {
            if (string.IsNullOrEmpty(nameToken))
                return "";
            
            if (_languageMap != null && _languageMap.TryGetValue(nameToken, out string name))
            {
                return name;
            }
            return nameToken;
        }

        private static string GetSimpleName(string nameToken)
        {
            // _languages에서 token_key가 일치하는 항목 찾아서 language_kr 반환
            foreach (var lang in _languages)
            {
                if (lang.token_key == nameToken)
                    return lang.language_kr;
            }
            return nameToken;
        }

        /// <summary>
        /// 모든 캐릭터와 몬스터를 합친 리스트 반환
        /// </summary>
        public static List<CharacterEntry> GetAllEntries()
        {
            EnsureLoaded();
            var all = new List<CharacterEntry>();

            // 캐릭터 추가 (구분자 포함)
            all.Add(new CharacterEntry { Id = -1, DisplayName = "--- 캐릭터 ---" });
            all.AddRange(_characters);

            // 몬스터 추가 (구분자 포함)
            all.Add(new CharacterEntry { Id = -2, DisplayName = "--- 몬스터 ---" });
            all.AddRange(_monsters);

            return all;
        }

        /// <summary>
        /// ID로 엔트리 찾기
        /// </summary>
        public static CharacterEntry? FindById(int id)
        {
            EnsureLoaded();

            foreach (var c in _characters)
            {
                if (c.Id == id) return c;
            }
            foreach (var m in _monsters)
            {
                if (m.Id == id) return m;
            }
            return null;
        }

        /// <summary>
        /// StageId로 스테이지 엔트리 찾기
        /// </summary>
        public static StageEntry? FindStageById(int stageId)
        {
            EnsureLoaded();

            foreach (var s in _stages)
            {
                if (s.StageId == stageId) return s;
            }
            return null;
        }

        /// <summary>
        /// 특정 스테이지의 몬스터 목록 가져오기
        /// </summary>
        public static List<StageMonsterEntry> GetStageMonsters(int chapterId, int stageNumber)
        {
            EnsureLoaded();

            var result = new List<StageMonsterEntry>();
            foreach (var sm in _stageMonsters)
            {
                if (sm.ChapterId == chapterId && sm.StageNumber == stageNumber)
                {
                    result.Add(sm);
                }
            }
            return result;
        }

        /// <summary>
        /// StageId로 몬스터 목록 가져오기
        /// </summary>
        public static List<StageMonsterEntry> GetStageMonstersByStageId(int stageId)
        {
            var stage = FindStageById(stageId);
            if (stage.HasValue)
            {
                return GetStageMonsters(stage.Value.ChapterId, stage.Value.StageNumber);
            }
            return new List<StageMonsterEntry>();
        }
    }
}
#endif
