using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle;


/// <summary>
/// 시너지 타입을 구분하는 도우미 클래스
/// 위 클래스는 유틸 클래스 역할을 하고있지만 사실 SpecData의 Enum으로 들어가게되면 필요없어질 수 있습니다.
/// </summary>
public static class DistinguishSynergyTypeHelper
{
    private static readonly SynergyType[] _asterismSynergyTypes = new SynergyType[]
    {
            // SynergyType.TANK,
            // SynergyType.WIZARD,
            // SynergyType.RANGER,
            // SynergyType.ASSASSIN,
            // SynergyType.SUPPORTER,
            SynergyType.NOBLESSE,
            SynergyType.SUPERNOVA,
    };
    private static readonly ElementType[] _elementSynergyTypes = new ElementType[]
    {
            ElementType.EARTH,
            ElementType.WIND,
            ElementType.WATER,
            ElementType.FIRE,
            ElementType.LIGHTNING,
    };
    public static bool IsAsterismSynergyType(SynergyType synergyType)
    {
        foreach (var asterismSynergyType in _asterismSynergyTypes)
        {
            if (asterismSynergyType == synergyType)
            {
                return true;
            }
        }
        return false;
    }
    public static bool IsElementSynergyType(ElementType synergyType)
    {
        foreach (var elementSynergyType in _elementSynergyTypes)
        {
            if (elementSynergyType == synergyType)
            {
                return true;
            }
        }
        return false;
    }

    public static int CountAsterismSynergyType()
    {
        return _asterismSynergyTypes.Length;
    }
    public static int CountElementSynergyType()
    {
        return _elementSynergyTypes.Length;
    }
}
