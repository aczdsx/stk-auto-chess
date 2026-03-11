using System;

namespace CookApps.AutoBattler
{
    public enum TimeType
    {
        DAY,
        HOUR,
        MINUTE,
        SECOND
    }
    
    public enum LanguageType
    {
        NONE,
        KR,
        EN,
    }

    public enum ShopBannerStateType
    {
        INACTIVE,
        ACTIVE,
    }
    
    public enum MissionStateType
    {
        NONE, // 진행중 아님
        WAIT, // 진행중 (대기 상태)
        REWARD, // 보상 수령 가능
        CLEAR // 보상 수령 후 클리어
    }

    public enum QuestStateType
    {
        WAIT, // 진행중 (대기 상태)
        REWARD, // 보상 수령 가능
        CLEAR // 보상 수령 후 클리어
    }

    public enum EventStateType
    {
        NONE, // 진행중 아님
        WAIT, // 진행중 (대기 상태)
        REWARD, // 보상 수령 가능
        CLEAR // 보상 수령 후 클리어
    }

    public enum DungeonStateType
    {
        WAIT, // 진행중 (대기 상태)
        REWARD, // 보상 수령 가능
        CLEAR // 보상 수령 후 클리어
    }
}

namespace CookApps.BattleSystem
{
    [Flags]
    public enum CrowdControlType
    {
        None = 0x0000,
        Airborne = 0x0001,
        KnockBack = 0x0002,
        Entangle = 0x0004,
        Stun = 0x0008,
        Slowing = 0x0010,
        Provocation = 0x0020,
        Freezing = 0x0040,
        Silence = 0x0080,
        MisaRestraint = 0x0100,
        All = 0xFFFF
    }

    public enum EffectCodeType : byte
    {
        Base,
        Game,
        Stat,
        Character,
        Buff,
        Debuff,
        CrowdControl,
        Item,
        Tile,
        CommanderSkill
    }

    public enum EffectCodeLifeType
    {
        Instant,
        Permanent,
        LimitedTime
    }

    public enum RoleType
    {
        None,
        Mage,
        Sword,
        Assassin,
        Ranger,
        Buffer,
        All
    }

    public enum AllianceType
    {
        None,
        Player,
        Enemy,
        Neutral,
        Wall,
        BattleItem
    }

    public enum BuffDebuffType
    {
        None = 0,
        Meditation,
        Shield,
        AttackUp,
        AbilityPowerUp,
        DefenceUp,
        ResistanceUp,
        AttackSpeedUp,
        CriticalProbUp,
        NormalAttackShield,
        NoDamageShield,
        Invincibility,
        CoolTimeUp,
        TargetImpossible,
        Immune,
        AvoidProbUp,
        AprilStander,
        TetorraAnger,
        ShirayukiAvoidAndAttack,
        EnkiPassiveHealUp,
        AdriaPassiveTeamHelp,
        BlinHeat,
        RukidaFoxfire,
        AttackDown = 1000,
        DefenceDown,
        CoolTimeDown,
        ResistanceDown,
        AttackSpeedDown,
        CriticalProbDown,
        HealDown,
        Bleeding,
        Poison,
        Burn,
        Slow,
        Entangle,
        Freezing,
        Stun,
        Sleep,
        Provocation,// 도발
        Anger,
        Trap,
        Silence,
        Airborne,
        Misa,
        MarieAracne,
        OdetteCold,
        MAX
    }

    public enum DamageReturnType
    {
        AlreadyDead,
        Killed,
        Damaging
    }

    /// <summary>
    /// 애니메이션 키
    /// </summary>
    public enum AnimationKey
    {
        IDLE,
        MOVE,
        ATK,
        ATK2,
        CRIT,
        SKL,
        SKL2,
        SKLPRE,
        SKLLOOP,
        SKLEND,
        SKL2PRE,
        SKL2LOOP,
        SKL2END,
        SKL3,
        SKL4,
        SKL5,
        DEAD,
        BUFF,
        GROGGY,
        GROGGYPRE,
        GROGGYLOOP,
        GROGGYEND,
        PARRY,
        Spawn,
        Crying,
        LongCrying,
    }
    
    /// <summary>
    /// 애니메이션 내에 발생하는 이벤트 키
    /// </summary>
    public enum AnimationEventKey
    {
        Start,
        End,

        VFXStart = 100,
        VFX1,
        VFX2,
        VFX3,
        VFX4,
        VFX5,
        VFXEnd,

        ExecuteStart = 1000,
        Execute1Per1,
        Execute1Per2,
        Execute1Per3,
        Execute1Per4,
        Execute1Per5,
        Execute1Per6,
        Execute1Per7,
        Execute1Per8,
        Execute1Per9,
        Execute1Per10,
        Execute1Per11,
        Execute1Per12,
        ExecuteEnd
    }

    public enum InGameVfxAnimationKey
    {
        Spawn,
        Idle,
        Broken,
        Disappear
    }

    public enum AttackRangeShape
    {
        Rectangle = 1,
        RectangleCut1Edge,
        RectangleCut2Edge,
        RectangleCut3Edge,
        RectangleCut4Edge,
        RectangleCut5Edge
    }

    public enum AttackType
    {
        Melee = 1,
        Projectile = 2
    }

    public enum ScanType
    {
        None,
        Nearest,
        Farthest
    }    
}