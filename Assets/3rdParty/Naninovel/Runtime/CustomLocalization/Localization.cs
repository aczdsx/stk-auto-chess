using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class Localization
{
    private const string StringTable = "StringTable";
    private const string StringTalk = "StringTalk"; // 어쩌면 대사는 시트를 따로 뺄수도 있어서.. 만들어둠

    /// 일반 로컬라이제이션 얻기
    public static string GetLocalizedString(string key)
    {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(StringTable, key);
        if (str == default)
            return $"not found key : {key}";
        return str;
    }

    /// 일반 로컬라이제이션 얻기
    public static string GetLocalizedString(string key, params object[] args)
    {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(StringTable, key);
        if (str == default)
            return $"not found key : {key}";
        return string.Format(str, args);
    }
    
    public static string GetLeftTimeString(long sec, bool usingPostFix = false)
    {
        TimeSpan leftTime = TimeSpan.FromSeconds(sec);
        string returnStr = string.Empty;
        Locale currentSelectedLocale = LocalizationSettings.SelectedLocale;
        ILocalesProvider availableLocales = LocalizationSettings.AvailableLocales;

        if (currentSelectedLocale == availableLocales.GetLocale("en"))
        {
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}d {1:00}h", leftTime.Days, leftTime.Hours);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}h {1:00}m", leftTime.Hours, leftTime.Minutes);
            }
            else if (leftTime.Minutes == 0)
            {
                returnStr = string.Format("{0:00}s", leftTime.Seconds);
            }
            else
            {
                returnStr = string.Format("{0:00}m {1:00}s", leftTime.Minutes, leftTime.Seconds);
            }
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("ko"))
        {
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}일 {1:00}시간", leftTime.Days, leftTime.Hours);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}시간 {1:00}분", leftTime.Hours, leftTime.Minutes);
            }
            else if (leftTime.Minutes == 0)
            {
                returnStr = string.Format("{0:00}초", leftTime.Seconds);
            }
            else
            {
                returnStr = string.Format("{0:00}분 {1:00}초", leftTime.Minutes, leftTime.Seconds);
            }
        }
        else if(currentSelectedLocale == availableLocales.GetLocale("ja"))
        {
            //10日4時間30分20秒
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}日 {1:00}時間", leftTime.Days, leftTime.Hours);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}時間 {1:00}分", leftTime.Hours, leftTime.Minutes);
            }
            else if (leftTime.Minutes == 0)
            {
                returnStr = string.Format("{0:00}秒", leftTime.Seconds);
            }
            else
            {
                returnStr = string.Format("{0:00}分 {1:00}秒", leftTime.Minutes, leftTime.Seconds);
            }
        }
        else
        {
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}d {1:00}h", leftTime.Days, leftTime.Hours);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}h {1:00}m", leftTime.Hours, leftTime.Minutes);
            }
            else if (leftTime.Minutes == 0)
            {
                returnStr = string.Format("{0:00}s", leftTime.Seconds);
            }
            else
            {
                returnStr = string.Format("{0:00}m {1:00}s", leftTime.Minutes, leftTime.Seconds);
            }
        }

       

        return returnStr;
    }
    
    public static string GetLeftTimeStringSimple(long sec, bool usingPostFix = false)
    {
        TimeSpan leftTime = TimeSpan.FromSeconds(sec);
        string returnStr = string.Empty;
        Locale currentSelectedLocale = LocalizationSettings.SelectedLocale;
        ILocalesProvider availableLocales = LocalizationSettings.AvailableLocales;

        if (currentSelectedLocale == availableLocales.GetLocale("en"))
        {
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}d", leftTime.Days);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}h ", leftTime.Hours);
            }
            else
            {
                returnStr = string.Format("{0:00}m", leftTime.Minutes);
            }
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("ko"))
        {
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}일", leftTime.Days);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}시간", leftTime.Hours);
            }
            else 
            {
                returnStr = string.Format("{0:00}분", leftTime.Minutes);
            }
        }
        else if(currentSelectedLocale == availableLocales.GetLocale("ja"))
        {
            //10日4時間30分20秒
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}日", leftTime.Days);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}時間", leftTime.Hours);
            }
            else
            {
                returnStr = string.Format("{0:00}分", leftTime.Minutes);
            }
           
        }
        else
        {
            if (leftTime.Days > 0)
            {
                returnStr = string.Format("{0:00}d", leftTime.Days);
            }
            else if (leftTime.Hours > 0)
            {
                returnStr = string.Format("{0:00}h ", leftTime.Hours);
            }
            else
            {
                returnStr = string.Format("{0:00}m", leftTime.Minutes);
            }
        }

       

        return returnStr;
    }
    public static string GetLanguageNameText()
    {
        Locale currentSelectedLocale = LocalizationSettings.SelectedLocale;
        ILocalesProvider availableLocales = LocalizationSettings.AvailableLocales;
        if (currentSelectedLocale == availableLocales.GetLocale("en"))
        {
            return "ENGLISH";
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("ko"))
        {
            return "한국어";
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("ja"))
        {
            return "日本語";
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("zh"))
        {
            return "中文";
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("zh-TW"))
        {
            return "中文（繁体）";
        }
        else if (currentSelectedLocale == availableLocales.GetLocale("zh-CN"))
        {
            return "中文";
        }
        // switch (currentSelectedLocale)
        // {
        //     case availableLocales.GetLocale("en"): return "ENGLISH";
        //     case availableLocales.GetLocale("ko"): return "한국어";
        //     case Language.JP: return "日本語";
        //     case Language.TC: return "繁體中文";
        //     case Language.SC: return "简体中文";
        //     case Language.FR: return "Français";
        //     case Language.DE: return "Deutsch";
        //     case Language.RU: return "Pусский";
        //     case Language.ES: return "Español";
        //     case Language.IT: return "Italiano";
        //     case Language.PT: return "Português";
        //     case Language.VN: return "Tiếng Việt";
        //     case Language.ID: return "bahasa Indonesia";
        //     case Language.TH: return "ไทย";
        //     case Language.TR: return "Türk";
        //     case Language.HI: return "हिन्दी";
        // }

        return string.Empty;
    }
    public static void SetLanguage(Locale lo)
    {
        LocalizationSettings.SelectedLocale = lo;
    }

    public static Locale GetCurrentLocale()
    {
        return LocalizationSettings.SelectedLocale;
    }
}
