using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;

public static class ElementAdvantageHelper
{
    public enum ElementAdvantageResult
    {
        NONE = 0,
        ADVANTAGE = 1,  // 유리 (공격자가 방어자에게 유리)
        RESIST = 2,      // 불리 (공격자가 방어자에게 불리)
    }

    /// <summary>
    /// 속성 상성 체인 순서: Fire → Wind → Earth → Lightning → Water → Fire (순환)
    /// </summary>
    private static readonly SynergyType[] _advantageElementChain = new SynergyType[]
    {
        SynergyType.FIRE,
        SynergyType.WIND,
        SynergyType.EARTH,
        SynergyType.LIGHTNING,
        SynergyType.WATER
    };
    private static readonly string[] _elementAdvantageTexts = new string[] { "WEAK!", "RESIST!" };

    public const float ADVANTAGE_MULTIPLIER = 1.25f;
    public const float RESIST_MULTIPLIER = 0.8f;

    /// <summary>
    /// 공격자와 방어자의 속성 상성 관계를 반환합니다.
    /// </summary>
    /// <param name="attacker">공격자의 속성</param>
    /// <param name="defender">방어자의 속성</param>
    /// <returns>상성 관계 결과</returns>
    private static ElementAdvantageResult GetElementAdvantageResult(SynergyType attackerElementType, SynergyType defenderElementType)
    {
        if (!IsInChain(attackerElementType) || !IsInChain(defenderElementType))
        {
            return ElementAdvantageResult.NONE;
        }

        if (GetNextInChain(attackerElementType) == defenderElementType)
        {
            return ElementAdvantageResult.ADVANTAGE;
        }
        else if (GetPreviousInChain(attackerElementType) == defenderElementType)
        {
            return ElementAdvantageResult.RESIST;
        }

        return ElementAdvantageResult.NONE;
    }

    public static ElementAdvantageResult CalculateElementAdvantageDamage(ref ObfuscatorDouble damage, SynergyType attackerElementType, SynergyType defenderElementType)
    {
        var elementAdvantageResult = GetElementAdvantageResult(attackerElementType, defenderElementType);
        switch (elementAdvantageResult)
        {
            case ElementAdvantageResult.ADVANTAGE:
                damage *= ADVANTAGE_MULTIPLIER;
                break;
            case ElementAdvantageResult.RESIST:
                damage *= RESIST_MULTIPLIER;
                break;
            case ElementAdvantageResult.NONE:
            default:
                break;
        }
        return elementAdvantageResult;
    }

    public static string GetElementAdvantageText(ElementAdvantageResult elementAdvantageResult)
    {
        if (elementAdvantageResult == ElementAdvantageResult.ADVANTAGE)
        {
            return _elementAdvantageTexts[0];
        }
        else if (elementAdvantageResult == ElementAdvantageResult.RESIST)
        {
            return _elementAdvantageTexts[1];
        }
        return string.Empty;
    }

    /// <summary>
    /// 속성이 체인에 포함되어 있는지 확인
    /// </summary>
    private static bool IsInChain(SynergyType elementType)
    {
        return Array.IndexOf(_advantageElementChain, elementType) >= 0;
    }

    /// <summary>
    /// 체인에서 주어진 속성의 우위 속성 반환
    /// Fire → Wind → Lightning → Earth → Water → Fire (순환)
    /// </summary>
    private static SynergyType GetNextInChain(SynergyType elementType)
    {
        if (!IsInChain(elementType))
            return SynergyType.NONE;

        int currentIndex = Array.IndexOf(_advantageElementChain, elementType);
        int nextIndex = (currentIndex + 1) % _advantageElementChain.Length;

        return _advantageElementChain[nextIndex];
    }

    /// <summary>
    /// 체인에서 주어진 속성의 역상성 속성 반환
    /// Fire ← Wind ← Lightning ← Earth ← Water ← Fire (역순환)
    /// </summary>
    private static SynergyType GetPreviousInChain(SynergyType elementType)
    {
        if (!IsInChain(elementType))
            return SynergyType.NONE;

        int currentIndex = Array.IndexOf(_advantageElementChain, elementType);
        int previousIndex = (currentIndex - 1 + _advantageElementChain.Length) % _advantageElementChain.Length;

        return _advantageElementChain[previousIndex];
    }

}
