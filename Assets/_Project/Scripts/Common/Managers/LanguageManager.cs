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

        public string GetTimeText(int targetTimeValue, TimeType type, bool isRemain)
        {
            string formatString = String.Empty;
            
            switch (type)
            {
                case TimeType.DAY:
                    formatString = isRemain ? GetLanguageText("TIME_DAY_REMAIN") : GetLanguageText("TIME_DAY");
                    return string.Format(formatString, targetTimeValue);
                case TimeType.HOUR:
                    formatString = isRemain ? GetLanguageText("TIME_HOUR_REMAIN") : GetLanguageText("TIME_HOUR");
                    return string.Format(formatString, targetTimeValue);
                case TimeType.MINUTE:
                    formatString = isRemain ? GetLanguageText("TIME_MINUTE_REMAIN") : GetLanguageText("TIME_MINUTE");
                    return string.Format(formatString, targetTimeValue);
                case TimeType.SECOND:
                    formatString = isRemain ? GetLanguageText("TIME_SECOND_REMAIN") : GetLanguageText("TIME_SECOND");
                    return string.Format(formatString, targetTimeValue);
            }

            return String.Empty;
        }
        
        public string GetElementText(SynergyType elementType)
        {
            switch (elementType)
            {
                case SynergyType.FIRE:
                    return GetLanguageText("SYNERGY_FIRE");
                case SynergyType.WATER:
                    return GetLanguageText("SYNERGY_WATER");
                case SynergyType.EARTH:
                    return GetLanguageText("SYNERGY_EARTH");
                case SynergyType.WIND:
                    return GetLanguageText("SYNERGY_WIND");
                case SynergyType.LIGHTNING:
                    return GetLanguageText("SYNERGY_LIGHTNING");
                default:
                    return string.Empty;
            }
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
                    return GetLanguageText("CLASS_TANK");
                case CharacterPositionType.GUARDIAN:
                    return GetLanguageText("CLASS_GUARDIAN");
                case CharacterPositionType.RANGER:
                    return GetLanguageText("CLASS_RANGER");
                case CharacterPositionType.WIZARD:
                    return GetLanguageText("CLASS_WIZARD");
                case CharacterPositionType.SUPPORTER:
                    return GetLanguageText("CLASS_SUPPORTER");
                case CharacterPositionType.ASSASSIN:
                    return GetLanguageText("CLASS_ASSASSIN");
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
                    return GetLanguageText("TIER_BRONZE");
                case PVPTierType.SILVER:
                    return GetLanguageText("TIER_SILVER");
                case PVPTierType.GOLD:
                    return GetLanguageText("TIER_GOLD");
                case PVPTierType.PLATINUM:
                    return GetLanguageText("TIER_PLATINUM");
                case PVPTierType.DIAMOND:
                    return GetLanguageText("TIER_DIAMOND");
                default:
                    return string.Empty;
            }
        }

        public string GetAtkTypeText(AtkType type)
        {
            switch (type)
            {
                case AtkType.AP:
                    return GetLanguageText("UI_TYPE_MAGICAL");
                case AtkType.AD:
                    return GetLanguageText("UI_TYPE_PHYSICAL");
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
                timeTextList.Append(GetTimeText(timeSpanData.Days, TimeType.DAY, false));
            }
            
            if (timeSpanData.Hours > 0)
            {
                timeTextList.Append(GetTimeText(timeSpanData.Hours, TimeType.HOUR, false));
            }
            
            timeTextList.Append(GetTimeText(timeSpanData.Minutes, TimeType.MINUTE, true));
            
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
                timeTextList.Append(GetTimeText(targetTimeSpan.Days, TimeType.DAY, false));
            }
            
            if (hasHours)
            {
                timeTextList.Append(GetTimeText(targetTimeSpan.Hours, TimeType.HOUR, false));
            }

            if (hasMinutes)
            {
                timeTextList.Append(GetTimeText(targetTimeSpan.Minutes, TimeType.MINUTE, true));
            }

            if (!hasDays && !hasHours) // 1시간 미만으로 남은 경우 초 단위 표시
            {
                timeTextList.Append(GetTimeText(targetTimeSpan.Seconds, TimeType.SECOND, true));
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
                timeTextList.Append(GetTimeText(targetTimeSpan.Days, TimeType.DAY, false));
            }
            
            if (hasHours)
            {
                timeTextList.Append(GetTimeText(targetTimeSpan.Hours, TimeType.HOUR, false));
            }

            if (hasMinutes)
            {
                timeTextList.Append(GetTimeText(targetTimeSpan.Minutes, TimeType.MINUTE, true));
            }

            if (!hasDays && !hasHours) // 1시간 미만으로 남은 경우 초 단위 표시
            {
                timeTextList.Append(GetTimeText(targetTimeSpan.Days, TimeType.SECOND, true));
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
