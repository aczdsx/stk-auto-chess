using CookApps.AutoBattler;
using CookApps.TeamBattle.Utility;

/// <summary>
/// 시너지 타입을 구분하는 도우미 클래스
/// 위 클래스는 유틸 클래스 역할을 하고있지만 사실 SpecData의 Enum으로 들어가게되면 필요없어질 수 있습니다.
/// </summary>
public static class DistinguishSpecTypeHelper
{
    private static readonly SynergyType[] _asterismSynergyTypes = new SynergyType[]
    {
            SynergyType.NOBLESSE,
            SynergyType.SUPERNOVA,
            SynergyType.TROUBLESHOOTER,
    };
    private static readonly SynergyType[] _elementSynergyTypes = new SynergyType[]
    {
            SynergyType.EARTH,
            SynergyType.WIND,
            SynergyType.WATER,
            SynergyType.FIRE,
            SynergyType.LIGHTNING,
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
    public static bool IsElementSynergyType(SynergyType synergyType)
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

    public static SimpleSwapType ToSwapType(SynergyType synergyType)
    {
        return synergyType switch
        {
            SynergyType.FIRE => SimpleSwapType.Fire,
            SynergyType.WIND => SimpleSwapType.Wind,
            SynergyType.LIGHTNING => SimpleSwapType.Lightning,
            SynergyType.EARTH => SimpleSwapType.Earth,
            SynergyType.WATER => SimpleSwapType.Water,
            SynergyType.NOBLESSE => SimpleSwapType.Noblesse,
            SynergyType.TROUBLESHOOTER => SimpleSwapType.Troubleshooter,
            SynergyType.SUPERNOVA => SimpleSwapType.Supernova,
            _ => SimpleSwapType.Normal,
        };
    }

    public static SimpleSwapType ToSwapType(CharacterPositionType positionType)
    {
        return positionType switch
        {
            CharacterPositionType.GUARDIAN => SimpleSwapType.GUARDIAN,
            CharacterPositionType.STRIKER => SimpleSwapType.STRIKER,
            CharacterPositionType.ORACLE => SimpleSwapType.ORACLE,
            CharacterPositionType.SHARPSHOOTER => SimpleSwapType.SHARPSHOOTER,
            CharacterPositionType.ESPER => SimpleSwapType.ESPER,
            CharacterPositionType.GHOST => SimpleSwapType.GHOST,
            _ => SimpleSwapType.Normal,
        };
    }
}
