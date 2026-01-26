using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoBattler
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        public const string DefaultTableName = "Default";
        public const string DialogueTableName = "Dialogue";

        // 테이블 캐시
        private StringTable defaultTable;
        private StringTable DefaultTable
        {
            get
            {
#if UNITY_EDITOR
                if (defaultTable == null)
                {
                    var collections = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections();
                    foreach (var collection in collections)
                    {
                        if (collection.TableCollectionName != DefaultTableName)
                            continue;
                        var locales = UnityEditor.Localization.LocalizationEditorSettings.GetLocales();
                        GetLocaleCode(Application.systemLanguage, out var localeCode);
                        foreach (var locale in locales)
                        {
                            if (locale.Identifier.Code != localeCode)
                                continue;

                            var table = collection.GetTable(locale.Identifier) as StringTable;
                            if (table == null)
                                continue;

                            defaultTable = table;
                        }
                    }
                }
#endif 
                return defaultTable;
            }
        }

        private StringTable dialogueTable;
        private StringTable DialogueTable
        {
            get
            {
#if UNITY_EDITOR
                if (dialogueTable == null)
                {
                    var collections = UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollections();
                    foreach (var collection in collections)
                    {
                        if (collection.TableCollectionName != DialogueTableName)
                            continue;
                        var locales = UnityEditor.Localization.LocalizationEditorSettings.GetLocales();
                        GetLocaleCode(Application.systemLanguage, out var localeCode);
                        foreach (var locale in locales)
                        {
                            if (locale.Identifier.Code != localeCode)
                                continue;

                            var table = collection.GetTable(locale.Identifier) as StringTable;
                            if (table == null)
                                continue;

                            dialogueTable = table;
                        }
                    }
                }
#endif
                return dialogueTable;
            }
        }

        // 로드 핸들 (Release용)
        private AsyncOperationHandle<StringTable> defaultTableHandle;
        private AsyncOperationHandle<StringTable> dialogueTableHandle;

        public SystemLanguage CurrentLanguageType { get; private set; }
        private bool isInitialized;

        private SystemLanguage GetLocaleCode(SystemLanguage language, out string localeCode)
        {
            switch (language)
            {
                case SystemLanguage.Korean:
                    localeCode = "ko";
                    break;
                case SystemLanguage.English:
                    localeCode = "en";
                    break;
                default:
                    localeCode = "en";
                    language = SystemLanguage.English;
                    break;
            }
            return language;
        }


        public async UniTask InitializeAsync()
        {
            if (isInitialized)
                return;

            InitLanguage();

            // Unity Localization 초기화 대기
            var initOperation = LocalizationSettings.InitializationOperation;
            if (!initOperation.IsDone)
            {
                await initOperation.WaitUntilDone();
            }

            // 테이블 비동기 로드
            await LoadTableAsync();

            isInitialized = true;
            Debug.Log("[LocalizationLoader] 초기화 완료");
        }

        /// <summary>
        /// 모든 테이블 비동기 로드
        /// </summary>
        private async UniTask LoadTableAsync()
        {
            // 기존 핸들 해제
            ReleaseDefaultTableHandle();
            ReleaseDialogueTableHandle();

            await LoadDefaultTableAsync();
            await LoadDialogueTableAsync();
        }

        /// <summary>
        /// Default 테이블 로드
        /// </summary>
        private async UniTask LoadDefaultTableAsync()
        {
            ReleaseDefaultTableHandle();

            defaultTableHandle = LocalizationSettings.StringDatabase.GetTableAsync(DefaultTableName);
            await defaultTableHandle.WaitUntilDone();

            if (defaultTableHandle.Status == AsyncOperationStatus.Succeeded)
            {
                defaultTable = defaultTableHandle.Result;
                Debug.Log($"[LocalizationLoader] {DefaultTableName} 테이블 로드 완료");
            }
            else
            {
                Debug.LogWarning($"[LocalizationLoader] {DefaultTableName} 테이블 로드 실패");
            }
        }

        /// <summary>
        /// Dialogue 테이블 로드
        /// </summary>
        public async UniTask LoadDialogueTableAsync()
        {
            ReleaseDialogueTableHandle();

            dialogueTableHandle = LocalizationSettings.StringDatabase.GetTableAsync(DialogueTableName);
            await dialogueTableHandle.WaitUntilDone();

            if (dialogueTableHandle.Status == AsyncOperationStatus.Succeeded)
            {
                dialogueTable = dialogueTableHandle.Result;
                Debug.Log($"[LocalizationLoader] {DialogueTableName} 테이블 로드 완료");
            }
            else
            {
                Debug.LogWarning($"[LocalizationLoader] {DialogueTableName} 테이블 로드 실패");
            }
        }

        /// <summary>
        /// Default 테이블 핸들 해제
        /// </summary>
        private void ReleaseDefaultTableHandle()
        {
            if (defaultTableHandle.IsValid())
            {
                UnityEngine.AddressableAssets.Addressables.Release(defaultTableHandle);
            }
            defaultTable = null;
        }

        /// <summary>
        /// Dialogue 테이블 핸들 해제
        /// </summary>
        public void ReleaseDialogueTableHandle()
        {
            if (dialogueTableHandle.IsValid())
            {
                UnityEngine.AddressableAssets.Addressables.Release(dialogueTableHandle);
            }
            dialogueTable = null;
        }

        /// <summary>
        /// 리소스 해제 (앱 종료 또는 씬 전환 시 호출)
        /// </summary>
        public void Release()
        {
            ReleaseDefaultTableHandle();
            ReleaseDialogueTableHandle();
            isInitialized = false;
            Debug.Log("[LocalizationLoader] 리소스 해제 완료");
        }

        /// <summary>
        /// 언어 변경 (테이블 다시 로드)
        /// </summary>
        public async UniTask SetLanguageAsync(SystemLanguage language)
        {
            language = GetLocaleCode(language, out var localeCode);

            var availableLocales = LocalizationSettings.AvailableLocales;
            Locale targetLocale = null;
            foreach (var locale in availableLocales.Locales)
            {
                if (locale.Identifier.Code == localeCode)
                {
                    targetLocale = locale;
                    break;
                }
            }

            if (targetLocale == null || LocalizationSettings.SelectedLocale == targetLocale)
            {
                Debug.LogWarning($"[LocalizationLoader] 언어 변경 실패: {language} ({localeCode})");
                return;
            }

            LocalizationSettings.SelectedLocale = targetLocale;
            UpdateLanguage(language);

            // 언어 변경 후 테이블 다시 로드
            await LoadTableAsync();

            Debug.Log($"[LocalizationLoader] 언어 변경 완료: {language} ({localeCode})");
        }

        /// <summary>
        /// Default 테이블에서 텍스트 조회
        /// 기존 LanguageManager.GetDefaultText() 대체
        /// </summary>
        public string GetDefaultText(string tokenKey)
        {
            return GetTextFromTable(DefaultTable, tokenKey);
        }

        /// <summary>
        /// Dialogue 테이블에서 텍스트 조회
        /// 기존 LanguageManager.GetDialogueText() 대체
        /// </summary>
        public string GetDialogueText(string tokenKey)
        {
            return GetTextFromTable(DialogueTable, tokenKey);
        }

        /// <summary>
        /// Dialogue 테이블이 로드되었는지 확인하고, 안되어 있으면 로드
        /// </summary>
        public async UniTask EnsureDialogueTableLoadedAsync()
        {
            if (dialogueTable != null)
                return;

            await LoadDialogueTableAsync();
        }

        /// <summary>
        /// 테이블에서 텍스트 조회 (내부 헬퍼)
        /// </summary>
        private string GetTextFromTable(StringTable table, string tokenKey)
        {
            if (table == null)
            {
                Debug.LogWarning($"[LanguageManager] 테이블이 로드되지않았습니다.tokenKey: {tokenKey}");
                return tokenKey;
            }

            var entry = table.GetEntry(tokenKey);
            if (entry == null)
            {
                // 엔트리를 찾을 수 없으면 토큰 키 반환
                return tokenKey;
            }

            return entry.GetLocalizedString() ?? tokenKey;
        }

        // 언어 환경 세팅
        private void InitLanguage()
        {
            var settingLanguage = Preference.LoadPreference(Pref.LANGUAGE, -1);

            if (settingLanguage == -1)
            {
                settingLanguage = Application.systemLanguage switch
                {
                    SystemLanguage.Korean => (int)SystemLanguage.Korean,
                    SystemLanguage.English => (int)SystemLanguage.English,
                    _ => (int)SystemLanguage.English
                };
            }

            var language = (SystemLanguage)settingLanguage;
            language = GetLocaleCode(language, out var localeCode);

            // LocalizationSettings.SelectedLocale 설정
            var availableLocales = LocalizationSettings.AvailableLocales;
            foreach (var locale in availableLocales.Locales)
            {
                if (locale.Identifier.Code == localeCode)
                {
                    LocalizationSettings.SelectedLocale = locale;
                    break;
                }
            }

            UpdateLanguage(language);
        }

        private void UpdateLanguage(SystemLanguage language)
        {
            var settingLanguage = language switch
            {
                SystemLanguage.Korean => SystemLanguage.Korean,
                SystemLanguage.English => SystemLanguage.English,
                _ => SystemLanguage.English
            };
            if (CurrentLanguageType == settingLanguage)
                return;
            CurrentLanguageType = settingLanguage;
            Preference.SavePreference(Pref.LANGUAGE, (int)settingLanguage);
        }

        private string GetTimeText(int targetTimeValue, TimeType type, bool isRemain)
        {
            return type switch
            {
                TimeType.DAY => isRemain
                    ? GetDefaultTextWithFormat("TIME_DAY_REMAIN", targetTimeValue)
                    : GetDefaultTextWithFormat("TIME_DAY", targetTimeValue),
                TimeType.HOUR => isRemain
                    ? GetDefaultTextWithFormat("TIME_HOUR_REMAIN", targetTimeValue)
                    : GetDefaultTextWithFormat("TIME_HOUR", targetTimeValue),
                TimeType.MINUTE => isRemain
                    ? GetDefaultTextWithFormat("TIME_MINUTE_REMAIN", targetTimeValue)
                    : GetDefaultTextWithFormat("TIME_MINUTE", targetTimeValue),
                TimeType.SECOND => isRemain
                    ? GetDefaultTextWithFormat("TIME_SECOND_REMAIN", targetTimeValue)
                    : GetDefaultTextWithFormat("TIME_SECOND", targetTimeValue),
                _ => string.Empty
            };
        }

        public string GetSynergyText(SynergyType elementType)
        {
            switch (elementType)
            {
                case SynergyType.NOBLESSE:
                case SynergyType.SUPERNOVA:
                case SynergyType.TROUBLESHOOTER:
                default:
                    return string.Empty;
            }
        }

        public string GetClassText(CharacterPositionType positionType)
        {
            switch (positionType)
            {
                case CharacterPositionType.TANK:
                    return GetDefaultText("CLASS_TANK");
                case CharacterPositionType.GUARDIAN:
                    return GetDefaultText("CLASS_GUARDIAN");
                case CharacterPositionType.RANGER:
                    return GetDefaultText("CLASS_RANGER");
                case CharacterPositionType.WIZARD:
                    return GetDefaultText("CLASS_WIZARD");
                case CharacterPositionType.SUPPORTER:
                    return GetDefaultText("CLASS_SUPPORTER");
                case CharacterPositionType.ASSASSIN:
                    return GetDefaultText("CLASS_ASSASSIN");
                default:
                    return string.Empty;
            }
        }

        public string GetItemCategoryText(ItemCategoryType type)
        {
            switch (type)
            {
                case ItemCategoryType.CURRENCY:
                    return GetDefaultText("UI_ITEM_CATEGORY_NAME_1");
                case ItemCategoryType.CHARACTER:
                    return GetDefaultText("UI_ITEM_CATEGORY_NAME_2");
                case ItemCategoryType.ETC:
                    return GetDefaultText("UI_ITEM_CATEGORY_NAME_3");
                default:
                    return string.Empty;
            }
        }

        public string GetGradeText(GradeType type)
        {
            switch (type)
            {
                case GradeType.COMMON:
                    return "N";
                case GradeType.RARE:
                    return "R";
                case GradeType.EPIC:
                    return "SR";
                case GradeType.LEGENDARY:
                    return "SSR";
                default:
                    return string.Empty;
            }
        }

        public string GetPVPTierText(PVPTierType type)
        {
            switch (type)
            {
                case PVPTierType.BRONZE:
                    return GetDefaultText("TIER_BRONZE");
                case PVPTierType.SILVER:
                    return GetDefaultText("TIER_SILVER");
                case PVPTierType.GOLD:
                    return GetDefaultText("TIER_GOLD");
                case PVPTierType.PLATINUM:
                    return GetDefaultText("TIER_PLATINUM");
                case PVPTierType.DIAMOND:
                    return GetDefaultText("TIER_DIAMOND");
                default:
                    return string.Empty;
            }
        }

        public string GetAtkTypeText(AtkType type)
        {
            switch (type)
            {
                case AtkType.AP:
                    return GetDefaultText("UI_TYPE_MAGICAL");
                case AtkType.AD:
                    return GetDefaultText("UI_TYPE_PHYSICAL");
                default:
                    return string.Empty;
            }
        }

        public string GetRemainTimeText(TimeSpan targetTimeSpan)
        {
            using var sb = ZString.CreateStringBuilder();

            int days = targetTimeSpan.Days;
            int hours = targetTimeSpan.Hours;
            int minutes = targetTimeSpan.Minutes;
            int seconds = targetTimeSpan.Seconds;

            if (days > 0)
            {
                sb.Append(GetTimeText(days, TimeType.DAY, false));
            }

            if (hours > 0)
            {
                sb.Append(GetTimeText(hours, TimeType.HOUR, false));
            }

            if (minutes > 0)
            {
                sb.Append(GetTimeText(minutes, TimeType.MINUTE, true));
            }

            // 1시간 미만으로 남은 경우 초 단위 표시
            if (days == 0 && hours == 0)
            {
                sb.Append(GetTimeText(seconds, TimeType.SECOND, true));
            }

            return sb.ToString();
        }

        #region Format Methods

        /// <summary>
        /// Default 텍스트 조회 (포맷팅 지원 - 1개 인자)
        /// </summary>
        public string GetDefaultTextWithFormat<T0>(string tokenKey, T0 arg0)
        {
            string text = GetDefaultText(tokenKey);
            try
            {
                return ZString.Format(text, arg0);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Default 텍스트 조회 (포맷팅 지원 - 2개 인자)
        /// </summary>
        public string GetDefaultTextWithFormat<T0, T1>(string tokenKey, T0 arg0, T1 arg1)
        {
            string text = GetDefaultText(tokenKey);
            try
            {
                return ZString.Format(text, arg0, arg1);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Default 텍스트 조회 (포맷팅 지원 - 3개 인자)
        /// </summary>
        public string GetDefaultTextWithFormat<T0, T1, T2>(string tokenKey, T0 arg0, T1 arg1, T2 arg2)
        {
            string text = GetDefaultText(tokenKey);
            try
            {
                return ZString.Format(text, arg0, arg1, arg2);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Default 텍스트 조회 (포맷팅 지원 - 4개 인자)
        /// </summary>
        public string GetDefaultTextWithFormat<T0, T1, T2, T3>(string tokenKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            string text = GetDefaultText(tokenKey);
            try
            {
                return ZString.Format(text, arg0, arg1, arg2, arg3);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Dialogue 텍스트 조회 (포맷팅 지원 - 1개 인자)
        /// </summary>
        public string GetDialogueTextWithFormat<T0>(string tokenKey, T0 arg0)
        {
            string text = GetDialogueText(tokenKey);
            try
            {
                return ZString.Format(text, arg0);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Dialogue 텍스트 조회 (포맷팅 지원 - 2개 인자)
        /// </summary>
        public string GetDialogueTextWithFormat<T0, T1>(string tokenKey, T0 arg0, T1 arg1)
        {
            string text = GetDialogueText(tokenKey);
            try
            {
                return ZString.Format(text, arg0, arg1);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Dialogue 텍스트 조회 (포맷팅 지원 - 3개 인자)
        /// </summary>
        public string GetDialogueTextWithFormat<T0, T1, T2>(string tokenKey, T0 arg0, T1 arg1, T2 arg2)
        {
            string text = GetDialogueText(tokenKey);
            try
            {
                return ZString.Format(text, arg0, arg1, arg2);
            }
            catch
            {
                return text;
            }
        }

        /// <summary>
        /// Dialogue 텍스트 조회 (포맷팅 지원 - 4개 인자)
        /// </summary>
        public string GetDialogueTextWithFormat<T0, T1, T2, T3>(string tokenKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            string text = GetDialogueText(tokenKey);
            try
            {
                return ZString.Format(text, arg0, arg1, arg2, arg3);
            }
            catch
            {
                return text;
            }
        }

        #endregion
    }
}
