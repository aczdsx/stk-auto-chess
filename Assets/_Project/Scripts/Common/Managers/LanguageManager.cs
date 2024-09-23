using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        public LanguageType CurrentLanguageType { get; private set; } = LanguageType.NONE;

        // 언어 환경 세팅
        public void InitLanguage()
        {
            var settingLanguage = Preference.LoadPreference(Pref.LANGUAGE, (int)LanguageType.NONE);
            
            if (settingLanguage == (int)LanguageType.NONE)
            {
                settingLanguage = (int)GetSystemLanguageType();
            }
            
            SetGameLanguage((LanguageType)settingLanguage);
        }
        
        public void SetGameLanguage(LanguageType type)
        {
            Preference.SavePreference(Pref.LANGUAGE, (int)type);
            
            CurrentLanguageType = type;
        }
        
        public string GetLanguageText(string tokenKey)
        {
            return SpecDataManager.Instance.GetLanguageText(tokenKey, CurrentLanguageType);
        }

        public string GetSynergyText(ElementType type)
        {
            switch (type)
            {
                case ElementType.FIRE:
                    return "불";
                case ElementType.WATER:
                    return "물";
                case ElementType.EARTH:
                    return "대지";
                case ElementType.WIND:
                    return "바람";
                case ElementType.LIGHT:
                    return "빛";
                case ElementType.DARK:
                    return "어둠";
                default:
                    return string.Empty;
            }
        }

        public string GetClassText(CharacterPositionType type)
        {
            switch (type)
            {
                case CharacterPositionType.TANK:
                    return "탱커";
                case CharacterPositionType.GUARDIAN:
                    return "전사";
                case CharacterPositionType.RANGER:
                    return "레인저";
                case CharacterPositionType.WIZARD:
                    return "마법사";
                case CharacterPositionType.SUPPORTER:
                    return "서포터";
                case CharacterPositionType.ASSASSIN:
                    return "암살자";
                default:
                    return string.Empty;
            }
        }

        public string GetItemCategoryText(ItemCategoryType type)
        {
            switch (type)
            {
                case ItemCategoryType.CURRENCY:
                    return GetLanguageText("UI_ITEM_CATEGORY_NAME_1");
                case ItemCategoryType.CHARACTER:
                    return GetLanguageText("UI_ITEM_CATEGORY_NAME_2");
                case ItemCategoryType.ETC:
                    return GetLanguageText("UI_ITEM_CATEGORY_NAME_3");
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
                case GradeType.LEGEND:
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
                    return "브론즈";
                case PVPTierType.SILVER:
                    return "실버";
                case PVPTierType.GOLD:
                    return "골드";
                case PVPTierType.PLATINUM:
                    return "플래티넘";
                case PVPTierType.DIAMOND:
                    return "다이아몬드";
                default:
                    return string.Empty;
            }
        }

        public string GetAtkTypeText(AtkType type)
        {
            switch (type)
            {
                case AtkType.AP:
                    return "마법";
                case AtkType.AD:
                    return "물리";
                default:
                    return string.Empty;
            }
        }

        public string GetTimeSpanFromNowText(long targetTimestamp)
        {
            var timeSpanData = TimeManager.Instance.GetTimeSpanFromNow(targetTimestamp);
            
            StringBuilder timeTextList = new StringBuilder();

            if (timeSpanData.Days > 0)
            {
                timeTextList.Append($"{timeSpanData.Days}일");
            }
            
            if (timeSpanData.Hours > 0)
            {
                timeTextList.Append($"{timeSpanData.Hours}시간");
            }
            
            timeTextList.Append($"{timeSpanData.Minutes}분 전");
            
            return timeTextList.ToString();
        }
        
        public string GetTimeSpanFromTargetText(long targetTimestamp)
        {
            var targetTimeSpan = TimeManager.Instance.GetTimeSpanFromTarget(targetTimestamp);
            
            StringBuilder timeTextList = new StringBuilder();

            bool hasDays = targetTimeSpan.Days > 0;
            bool hasHours = targetTimeSpan.Hours > 0;
            bool hasMinutes = targetTimeSpan.Minutes > 0;
            
            if (hasDays)
            {
                timeTextList.Append($"{targetTimeSpan.Days}일");
            }
            
            if (hasHours)
            {
                timeTextList.Append($"{targetTimeSpan.Hours}시간");
            }

            if (hasMinutes)
            {
                timeTextList.Append($"{targetTimeSpan.Minutes}분 남음");
            }

            if (!hasDays && !hasHours) // 1시간 미만으로 남은 경우 초 단위 표시
            {
                timeTextList.Append($"{targetTimeSpan.Seconds}초 남음");
            }
            
            return timeTextList.ToString();
        }

        public string GetRemainTimeText(TimeSpan targetTimeSpan)
        {
            StringBuilder timeTextList = new StringBuilder();

            bool hasDays = targetTimeSpan.Days > 0;
            bool hasHours = targetTimeSpan.Hours > 0;
            bool hasMinutes = targetTimeSpan.Minutes > 0;
            
            if (hasDays)
            {
                timeTextList.Append($"{targetTimeSpan.Days}일");
            }
            
            if (hasHours)
            {
                timeTextList.Append($"{targetTimeSpan.Hours}시간");
            }

            if (hasMinutes)
            {
                timeTextList.Append($"{targetTimeSpan.Minutes}분 남음");
            }

            if (!hasDays && !hasHours) // 1시간 미만으로 남은 경우 초 단위 표시
            {
                timeTextList.Append($"{targetTimeSpan.Seconds}초 남음");
            }
            
            return timeTextList.ToString();
        }
        
        public LanguageType GetSystemLanguageType()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Korean:
                    return LanguageType.KR;
                default:
                    return LanguageType.EN;
            }
        }
    }
}
