using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using CookApps.AutoBattler;
using UnityEngine;

public static class StringUtil
{
    private static char[] BigNumberLiteral = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
    private static StringBuilder StringBuilder = new StringBuilder();

    public static string GetStageString(SpecStage stage)
    {
        return $"스테이지 {stage.chapter_id}-{stage.stage_number}";
    }
    
    public static string GetTrialDungeonString(SpecDungeonTrial trialDungeon)
    {
        
        return (trialDungeon.step == 1) ? $"{trialDungeon.trial_type}" : $"{trialDungeon.trial_type}-{trialDungeon.step}";
    }

    public static string BigIntToShortString(BigInteger value)
    {
        if (value < 100000)
            return value.ToString("n0");

        //입력된 값에서 자리수 파악
        int digits = Convert.ToInt32(Math.Truncate(BigInteger.Log10(value)));

        if (digits < 3)
        {
            return value.ToString();
        }
        else
        {
            StringBuilder.Clear();

            //제일 앞쪽 정수부분과 소수점 부분 최대 5자리 가져오기.
            BigInteger fourDigits = BigInteger.Pow(10, (digits / 3) * 3) / 1000;

            float result = (float)(value / fourDigits) / 1000f;
            StringBuilder.AppendFormat("{0:0.###}", result);

            List<char> literals = new List<char>();

            int maxLiteralCnt = (digits / 3) / 26;
            for (int idx = 0; idx < maxLiteralCnt; ++idx)
            {
                //StringBuilder.Append(BigNumberLiteral[0]);
                if (literals.Count > 0)
                    literals[literals.Count - 1] = BigNumberLiteral[0];

                literals.Add(BigNumberLiteral[25]);
            }

            int endLiteral = (digits / 3) % 26 - 1;
            if (endLiteral >= 0)
            {
                if (literals.Count > 0)
                    literals[literals.Count - 1] = BigNumberLiteral[0];

                literals.Add(BigNumberLiteral[endLiteral]);
            }

            for (int idx = 0; idx < literals.Count; ++idx)
                StringBuilder.Append(literals[idx]);
        }

        return StringBuilder.ToString();
    }

    public static string PriceColorString(int have, int cost, bool isOnlyCost = false, bool isColorReverse = false)
    {
        string result = string.Empty;
        string colorNotEnough = "#C5C5B2"; // Red color
        string colorEnough = "#CA6E71";  // Green color

        if (isColorReverse)
        {
            string tempColor = colorNotEnough;
            colorNotEnough = colorEnough;
            colorEnough = tempColor;
        }

        if (isOnlyCost)
        {
            if (cost > have)
            {
                result = $"<color={colorNotEnough}>{cost.ToString("n0")}</color>";
            }
            else
            {
                result = $"<color={colorEnough}>{cost.ToString("n0")}</color>";
            }
        }
        else
        {
            if (cost > have)
            {
                result = $"<color={colorNotEnough}>{have.ToString("n0")}</color>/{cost.ToString("n0")}";
            }
            else
            {
                result = $"<color={colorEnough}>{have.ToString("n0")}</color>/{cost.ToString("n0")}";
            }
        }

        return result;
    }

    public static string GetCompareString(int have, int cost, bool isOnlyCost = false)
    {
        string result = string.Empty;
        string colorNotEnough = "#FFFCD0"; 
        string colorEnough = "#CA6E71"; 
        
        if (isOnlyCost)
        {
            if (cost > have)
            {
                result = $"<color={colorNotEnough}>{cost.ToString("n0")}</color>";
            }
            else
            {
                result = $"<color={colorEnough}>{cost.ToString("n0")}</color>";
            }
        }
        else
        {
            if (cost > have)
            {
                result = $"{cost.ToString("n0")}/<color={colorNotEnough}>{have.ToString("n0")}</color>";
            }
            else
            {
                result = $"{cost.ToString("n0")}/<color={colorEnough}>{have.ToString("n0")}</color>";
            }
        }

        return result;
    }

    public static BigInteger ShortStringToBigInt(string shortString)
    {
        shortString = shortString.ToUpper();

        BigInteger temp = 1;
        int firstSymbolPos = 0;
        for (int idx = shortString.Length - 1; idx >= 0; --idx)
        {
            char element = shortString[idx];

            if (element >= 'A' && element <= 'Z')
            {
                int multiplier = (int)element - 64;

                if (firstSymbolPos > 0 && multiplier == 1)
                {
                    multiplier = 26;
                }

                temp *= BigInteger.Pow(1000, multiplier);

                firstSymbolPos = idx;
            }
        }

        string numberPart = (firstSymbolPos == 0) ? shortString : shortString.Substring(0, firstSymbolPos);

        BigInteger result = 0;

        if (firstSymbolPos == 0)
        {
            result = BigInteger.Parse(numberPart, System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            float floatingNumber = float.Parse(numberPart, System.Globalization.CultureInfo.InvariantCulture) * 100f;
            result = (BigInteger)floatingNumber;
            result = (result * temp) / 100;
        }

        return result;
    }

    public static string CalcurateScore(int score, int count = 4)
    {
        string scoreString = score.ToString();
        int length = scoreString.Length;
        int zeroLength = count - length;
        for (int i = 0; i < zeroLength; i++)
        {
            scoreString = "0" + scoreString;
        }

        return scoreString;
    }

    public static string GetRomanNumberStr(int number)
    {
        switch (number)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            case 4: return "IV";
            case 5: return "V";
            case 6: return "VI";
            case 7: return "VII";
            case 8: return "VIII";
            case 9: return "IX";
            case 10: return "X";
            case 11: return "XI";
            case 12: return "XII";
            case 13: return "XIII";
            case 14: return "XIV";
            case 15: return "XV";
            case 16: return "XVI";
            case 17: return "XVII";
            case 18: return "XVIII";
            case 19: return "XIX";
            case 20: return "XX";
            case 21: return "XXI";
            case 22: return "XXII";
            case 23: return "XXIII";
            case 24: return "XXIV";
            case 25: return "XXV";
            case 26: return "XXVI";
            case 27: return "XXVII";
            case 28: return "XXVIII";
            case 29: return "XXIX";
            case 30: return "XXX";

        }

        return string.Empty;
    }

    public static string GetListToStringBuilder(List<int> list)
    {
        //App Event
        StringBuilder dataList = new StringBuilder();
        for (int idx = 0; idx < list.Count; ++idx)
        {
            if (list.Count > idx)
            {
                dataList.Append($"{list[idx]};");
            }
            else
            {
                dataList.Append("0;");
            }
        }

        return dataList.ToString();
    }
}
