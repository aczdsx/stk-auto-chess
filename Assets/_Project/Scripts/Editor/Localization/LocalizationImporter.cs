#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// 임포트 대상 테이블 타입
    /// </summary>
    public enum ImportTableType
    {
        Default,    // Language → Default 테이블
        Dialogue    // DialogueLanguage → Dialogue 테이블
    }

    /// <summary>
    /// JSON Language 데이터를 Unity Localization StringTable로 임포트하는 유틸리티
    /// </summary>
    public static class LocalizationImporter
    {
        private const string LocalizationBasePath = "Assets/_Project/Addressables/BuiltIn/Localization";
        private const string LocaleFolder = "Locale";
        private const string TableFolder = "Table";

        // 토큰 키 필드명
        private const string TokenKeyField = "key";

        /// <summary>
        /// 언어 필드명 → 로케일 코드 매핑
        /// </summary>
        private static readonly Dictionary<string, string> LanguageFieldToLocaleCode = new()
        {
            { "kr", "ko" },
            { "en", "en" },
            { "ja", "ja" },
            { "zh", "zh-Hans" },
            { "tw", "zh-Hant" },
        };

        /// <summary>
        /// 로케일 코드 → 표시 이름 매핑
        /// </summary>
        private static readonly Dictionary<string, string> LocaleCodeToDisplayName = new()
        {
            { "ko", "Korean (ko)" },
            { "en", "English (en)" },
            { "ja", "Japanese (ja)" },
            { "zh-Hans", "Chinese (Simplified) (zh-Hans)" },
            { "zh-Hant", "Chinese (Traditional) (zh-Hant)" },
        };

        /// <summary>
        /// StringTable 캐시: (tableName, localeCode) → StringTable
        /// </summary>
        private static Dictionary<(string, string), StringTable> _tableCache;
        private static bool _cacheInitialized;

        /// <summary>
        /// JSON 문자열에서 모든 테이블(Default, Dialogue) 임포트
        /// </summary>
        public static ImportResult ImportAllFromJsonString(string json)
        {
            var result = new ImportResult();

            try
            {
                // Default 테이블 임포트
                var defaultResult = ImportFromJsonString(json, ImportTableType.Default);

                // Dialogue 테이블 임포트
                var dialogueResult = ImportFromJsonString(json, ImportTableType.Dialogue);

                // 결과 병합
                result.Success = defaultResult.Success || dialogueResult.Success;
                result.DetectedLanguages = defaultResult.DetectedLanguages;

                if (!defaultResult.Success && !dialogueResult.Success)
                {
                    result.ErrorMessage = $"Default: {defaultResult.ErrorMessage}\nDialogue: {dialogueResult.ErrorMessage}";
                }
                else
                {
                    result.TotalEntries = defaultResult.TotalEntries + dialogueResult.TotalEntries;

                    // 언어별 통계 병합
                    foreach (var lang in defaultResult.DetectedLanguages)
                    {
                        int added = 0;
                        int updated = 0;

                        if (defaultResult.AddedPerLanguage.TryGetValue(lang, out var da))
                            added += da;
                        if (dialogueResult.AddedPerLanguage.TryGetValue(lang, out var dia))
                            added += dia;
                        if (defaultResult.UpdatedPerLanguage.TryGetValue(lang, out var du))
                            updated += du;
                        if (dialogueResult.UpdatedPerLanguage.TryGetValue(lang, out var diu))
                            updated += diu;

                        result.AddedPerLanguage[lang] = added;
                        result.UpdatedPerLanguage[lang] = updated;
                    }

                    result.TableResults["Default"] = defaultResult;
                    result.TableResults["Dialogue"] = dialogueResult;
                }
            }
            catch (Exception e)
            {
                result.ErrorMessage = $"JSON 파싱 실패: {e.Message}";
            }

            return result;
        }

        /// <summary>
        /// JSON 파일에서 모든 테이블(Default, Dialogue) 임포트
        /// </summary>
        public static ImportResult ImportAllFromJsonFile(string jsonPath)
        {
            var result = new ImportResult();

            if (!File.Exists(jsonPath))
            {
                result.ErrorMessage = $"JSON 파일을 찾을 수 없습니다: {jsonPath}";
                return result;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                return ImportAllFromJsonString(json);
            }
            catch (Exception e)
            {
                result.ErrorMessage = $"JSON 파일 읽기 실패: {e.Message}";
            }

            return result;
        }

        /// <summary>
        /// JSON 파일에서 특정 테이블 타입 데이터를 StringTable로 임포트
        /// </summary>
        public static ImportResult ImportFromJsonFile(string jsonPath, ImportTableType tableType)
        {
            var result = new ImportResult();

            if (!File.Exists(jsonPath))
            {
                result.ErrorMessage = $"JSON 파일을 찾을 수 없습니다: {jsonPath}";
                return result;
            }

            try
            {
                string json = File.ReadAllText(jsonPath);
                return ImportFromJsonString(json, tableType);
            }
            catch (Exception e)
            {
                result.ErrorMessage = $"JSON 파일 읽기 실패: {e.Message}";
                return result;
            }
        }

        /// <summary>
        /// JSON 문자열에서 특정 테이블 타입 데이터를 StringTable로 임포트
        /// </summary>
        public static ImportResult ImportFromJsonString(string json, ImportTableType tableType)
        {
            var result = new ImportResult();

            // 테이블 타입에 따른 설정
            string tableName = tableType == ImportTableType.Default ? LanguageManager.DefaultTableName : LanguageManager.DialogueTableName;

            try
            {
                var root = JObject.Parse(json);
                var languageArray = root[tableName] as JArray;

                if (languageArray == null || languageArray.Count == 0)
                {
                    result.ErrorMessage = $"JSON에서 {tableName} 배열을 찾을 수 없습니다.";
                    return result;
                }

                // 1. 언어 필드 감지
                var detectedFields = DetectLanguageFields(languageArray);
                if (detectedFields.Count == 0)
                {
                    result.ErrorMessage = $"{tableName} 데이터에서 언어 필드를 찾을 수 없습니다.";
                    return result;
                }

                result.DetectedLanguages = detectedFields.Select(f =>
                    LanguageFieldToLocaleCode.TryGetValue(f, out var code) ? code : f
                ).ToList();

                // 2. 데이터 파싱
                var languageData = ParseLanguageData(languageArray, detectedFields, TokenKeyField);
                result.TotalEntries = languageData.Count;

                // 3. StringTableCollection 가져오기 또는 생성
                var tableCollection = GetOrCreateTableCollection(tableName);
                if (tableCollection == null)
                {
                    result.ErrorMessage = $"StringTableCollection을 생성할 수 없습니다: {tableName}";
                    return result;
                }

                // 4. 기존 키 목록 수집
                var existingKeys = new HashSet<string>();
                foreach (var entry in tableCollection.SharedData.Entries)
                {
                    existingKeys.Add(entry.Key);
                }

                // 5. 새 키 목록
                var newKeys = new HashSet<string>(languageData.Keys);

                // 6. 삭제할 키 (기존에 있지만 새 데이터에 없는 키)
                var keysToRemove = new List<string>();
                foreach (var key in existingKeys)
                {
                    if (!newKeys.Contains(key))
                    {
                        keysToRemove.Add(key);
                    }
                }

                // 7. 각 언어별 Locale 및 Table 업데이트
                foreach (var field in detectedFields)
                {
                    if (!LanguageFieldToLocaleCode.TryGetValue(field, out var localeCode))
                    {
                        Debug.LogWarning($"[LocalizationImporter] 알 수 없는 언어 필드: {field}");
                        continue;
                    }

                    var locale = GetOrCreateLocale(localeCode);
                    if (locale == null)
                    {
                        Debug.LogWarning($"[LocalizationImporter] Locale 생성 실패: {localeCode}");
                        continue;
                    }

                    var stringTable = GetOrCreateStringTable(tableCollection, locale);
                    if (stringTable == null)
                    {
                        Debug.LogWarning($"[LocalizationImporter] StringTable 생성 실패: {tableName}_{localeCode}");
                        continue;
                    }

                    int addedCount = 0;
                    int updatedCount = 0;
                    int skippedCount = 0;

                    // 8. 데이터 추가/업데이트
                    foreach (var entry in languageData)
                    {
                        string tokenKey = entry.Key;
                        if (!entry.Value.TryGetValue(field, out var newText))
                            continue;

                        newText ??= string.Empty;

                        var existingEntry = stringTable.GetEntry(tokenKey);
                        if (existingEntry != null)
                        {
                            // 기존 엔트리: 값이 다른 경우에만 업데이트
                            if (existingEntry.Value != newText)
                            {
                                existingEntry.Value = newText;
                                updatedCount++;
                            }
                            else
                            {
                                // 값이 같으면 스킵
                                skippedCount++;
                            }
                        }
                        else
                        {
                            // 새 엔트리 추가 (SharedData에 키가 없으면 먼저 추가)
                            if (!existingKeys.Contains(tokenKey))
                            {
                                tableCollection.SharedData.AddKey(tokenKey);
                                existingKeys.Add(tokenKey); // 다른 언어 테이블에서 중복 추가 방지
                            }
                            stringTable.AddEntry(tokenKey, newText);
                            addedCount++;
                        }
                    }

                    // 9. 삭제할 엔트리 제거
                    foreach (var key in keysToRemove)
                    {
                        var entryToRemove = stringTable.GetEntry(key);
                        if (entryToRemove != null)
                        {
                            stringTable.RemoveEntry(entryToRemove.KeyId);
                        }
                    }

                    result.AddedPerLanguage[localeCode] = addedCount;
                    result.UpdatedPerLanguage[localeCode] = updatedCount;
                    result.SkippedPerLanguage[localeCode] = skippedCount;

                    EditorUtility.SetDirty(stringTable);
                }

                // 10. SharedData에서 삭제할 키 제거
                foreach (var key in keysToRemove)
                {
                    var sharedEntry = tableCollection.SharedData.GetEntry(key);
                    if (sharedEntry != null)
                    {
                        tableCollection.SharedData.RemoveKey(sharedEntry.Id);
                    }
                }

                // 11. 저장
                EditorUtility.SetDirty(tableCollection);
                EditorUtility.SetDirty(tableCollection.SharedData);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 9. 캐시 무효화
                InvalidateCache();

                result.Success = true;
            }
            catch (Exception e)
            {
                result.ErrorMessage = $"임포트 실패: {e.Message}\n{e.StackTrace}";
            }

            return result;
        }

        /// <summary>
        /// Language 배열에서 언어 필드 감지
        /// </summary>
        private static List<string> DetectLanguageFields(JArray languageArray)
        {
            var fields = new List<string>();

            if (languageArray.Count == 0)
                return fields;

            var firstItem = languageArray[0] as JObject;
            if (firstItem == null)
                return fields;

            foreach (var prop in firstItem.Properties())
            {
                // key 필드 제외, LanguageFieldToLocaleCode에 정의된 언어 필드만 감지
                if (LanguageFieldToLocaleCode.ContainsKey(prop.Name))
                {
                    fields.Add(prop.Name);
                }
            }

            return fields;
        }

        /// <summary>
        /// Language 데이터 파싱
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> ParseLanguageData(
            JArray languageArray,
            List<string> languageFields,
            string tokenKeyFieldName = TokenKeyField)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();

            foreach (var item in languageArray)
            {
                var tokenKey = item[tokenKeyFieldName]?.Value<string>();
                if (string.IsNullOrEmpty(tokenKey))
                    continue;

                var translations = new Dictionary<string, string>();
                foreach (var field in languageFields)
                {
                    var text = item[field]?.Value<string>() ?? string.Empty;
                    translations[field] = text;
                }

                if (!result.ContainsKey(tokenKey))
                {
                    result[tokenKey] = translations;
                }
            }

            return result;
        }

        /// <summary>
        /// 캐시 초기화 (필요 시 호출)
        /// </summary>
        private static void EnsureCacheInitialized()
        {
            if (_cacheInitialized && _tableCache != null)
                return;

            _tableCache = new Dictionary<(string, string), StringTable>();
            _cacheInitialized = true;

            var collections = LocalizationEditorSettings.GetStringTableCollections();
            var locales = LocalizationEditorSettings.GetLocales();

            foreach (var collection in collections)
            {
                foreach (var locale in locales)
                {
                    var table = collection.GetTable(locale.Identifier) as StringTable;
                    if (table != null)
                    {
                        _tableCache[(collection.TableCollectionName, locale.Identifier.Code)] = table;
                    }
                }
            }
        }

        /// <summary>
        /// 캐시 무효화 (임포트 후 호출)
        /// </summary>
        public static void InvalidateCache()
        {
            _tableCache = null;
            _cacheInitialized = false;
        }

        /// <summary>
        /// 에디터에서 Localization 텍스트 가져오기 (캐시 사용)
        /// </summary>
        /// <param name="key">토큰 키</param>
        /// <param name="localeCode">로케일 코드 (ko, en, ja 등). null이면 ko 사용</param>
        /// <param name="tableName">테이블 이름 (Default, Dialogue). null이면 Default 사용</param>
        public static string GetText(string key, string localeCode = "ko", string tableName = "Default")
        {
            EnsureCacheInitialized();

            if (_tableCache.TryGetValue((tableName, localeCode), out var table))
            {
                var entry = table.GetEntry(key);
                return entry?.Value;
            }

            return null;
        }

        /// <summary>
        /// StringTableCollection 가져오기 또는 생성
        /// </summary>
        private static StringTableCollection GetOrCreateTableCollection(string tableName)
        {
            // 기존 테이블 검색
            var existingCollections = LocalizationEditorSettings.GetStringTableCollections();
            foreach (var collection in existingCollections)
            {
                if (collection.TableCollectionName == tableName)
                {
                    return collection;
                }
            }

            // 새 테이블 생성
            string tablePath = $"{LocalizationBasePath}/{TableFolder}";
            EnsureDirectoryExists(tablePath);

            var newCollection = LocalizationEditorSettings.CreateStringTableCollection(
                tableName,
                tablePath,
                LocalizationEditorSettings.GetLocales()
            );

            return newCollection;
        }

        /// <summary>
        /// Locale 가져오기 또는 생성
        /// </summary>
        private static Locale GetOrCreateLocale(string localeCode)
        {
            // 기존 Locale 검색
            var existingLocales = LocalizationEditorSettings.GetLocales();
            foreach (var locale in existingLocales)
            {
                if (locale.Identifier.Code == localeCode)
                {
                    return locale;
                }
            }

            // 새 Locale 생성
            string localePath = $"{LocalizationBasePath}/{LocaleFolder}";
            EnsureDirectoryExists(localePath);

            var newLocale = Locale.CreateLocale(new LocaleIdentifier(localeCode));

            if (LocaleCodeToDisplayName.TryGetValue(localeCode, out var displayName))
            {
                newLocale.name = displayName;
            }

            string assetPath = $"{localePath}/{newLocale.name}.asset";
            AssetDatabase.CreateAsset(newLocale, assetPath);

            LocalizationEditorSettings.AddLocale(newLocale);

            return newLocale;
        }

        /// <summary>
        /// StringTable 가져오기 또는 생성
        /// </summary>
        private static StringTable GetOrCreateStringTable(StringTableCollection collection, Locale locale)
        {
            var existingTable = collection.GetTable(locale.Identifier) as StringTable;
            if (existingTable != null)
            {
                return existingTable;
            }

            // 새 테이블 생성
            collection.AddNewTable(locale.Identifier);
            return collection.GetTable(locale.Identifier) as StringTable;
        }

        /// <summary>
        /// 디렉토리 존재 확인 및 생성
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 임포트 결과
        /// </summary>
        public class ImportResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
            public int TotalEntries { get; set; }
            public List<string> DetectedLanguages { get; set; } = new();
            public Dictionary<string, int> AddedPerLanguage { get; set; } = new();
            public Dictionary<string, int> UpdatedPerLanguage { get; set; } = new();
            public Dictionary<string, int> SkippedPerLanguage { get; set; } = new();
            public Dictionary<string, ImportResult> TableResults { get; set; } = new();

            public string GetSummary()
            {
                if (!Success)
                {
                    return $"[실패] {ErrorMessage}";
                }

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"[성공] 총 {TotalEntries}개 엔트리 처리");
                sb.AppendLine($"감지된 언어: {string.Join(", ", DetectedLanguages)}");
                sb.AppendLine();

                // 테이블별 결과가 있는 경우
                if (TableResults.Count > 0)
                {
                    foreach (var tableResult in TableResults)
                    {
                        sb.AppendLine($"=== {tableResult.Key} 테이블 ===");
                        sb.AppendLine($"  엔트리: {tableResult.Value.TotalEntries}개");
                        foreach (var lang in tableResult.Value.DetectedLanguages)
                        {
                            int added = tableResult.Value.AddedPerLanguage.GetValueOrDefault(lang, 0);
                            int updated = tableResult.Value.UpdatedPerLanguage.GetValueOrDefault(lang, 0);
                            int skipped = tableResult.Value.SkippedPerLanguage.GetValueOrDefault(lang, 0);                                                                     
                            sb.AppendLine($"    {lang}: 추가 {added}, 업데이트 {updated}, 스킵 {skipped}");
                        }
                        sb.AppendLine();
                    }
                }
                else
                {
                    foreach (var lang in DetectedLanguages)
                    {
                        int added = AddedPerLanguage.GetValueOrDefault(lang, 0);
                        int updated = UpdatedPerLanguage.GetValueOrDefault(lang, 0);
                        int skipped = SkippedPerLanguage.GetValueOrDefault(lang, 0);
                        sb.AppendLine($"    {lang}: 추가 {added}, 업데이트 {updated}, 스킵 {skipped}");
                    }
                }

                return sb.ToString();
            }
        }
    }
}
#endif
