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

        private static List<LanguageEntry> _languages;
        private static List<CharacterEntry> _characters;
        private static List<CharacterEntry> _monsters;
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

        public static void Reload()
        {
            _isLoaded = false;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (_isLoaded) return;

            _languages = new List<LanguageEntry>();
            _characters = new List<CharacterEntry>();
            _monsters = new List<CharacterEntry>();

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
                        entry.DisplayName = $"[{entry.Id}] {GetSimpleName(entry.NameToken)}";
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
                        entry.DisplayName = $"[{entry.Id}] {GetSimpleName(entry.NameToken)}";
                        _monsters.Add(entry);
                    }
                }

                // ID 순으로 정렬
                _characters.Sort((a, b) => a.Id.CompareTo(b.Id));
                _monsters.Sort((a, b) => a.Id.CompareTo(b.Id));

                _isLoaded = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[InGameTestSpecDataHelper] Failed to parse SpecData: {e.Message}");
                _isLoaded = true;
            }
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
    }
}
#endif
