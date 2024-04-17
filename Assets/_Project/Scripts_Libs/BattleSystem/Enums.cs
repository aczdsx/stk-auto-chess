using System;

namespace CookApps.TeamBattle.BattleSystem
{
    [Flags]
    public enum CrowdControlType
    {
        None = 0x0000,
        Airborne = 0x0001,
        KnockBack = 0x0003,
        Entangle = 0x0004,
        Stun = 0x0008,
        Slowing = 0x0010,
        Provocation = 0x0020,
        Invincibility = 0x0040,
        BlackHole = 0x0080,
        All = 0x0FFF,
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
        Spread,
        Item,
    }

    public enum EffectCodeLifeType
    {
        Instant,
        Permanent,
        LimitedTime,
    }

    public enum RoleType
    {
        None,
        Mage,
        Sword,
        Ranger,
        Buffer,
        All,
    }

    public enum AllianceType
    {
        None,
        Player,
        Enemy,
    }

    public enum BuffDebuffType
    {
        None = 0,
        Meditation,
        Shield,
        Bleeding,
        Poison,
        Burn,
        AttackUp,
        DefenceUp,
        ResistanceUp,
        AttackDown,
        DefenceDown,
        ResistanceDown,
        AttackSpeedUp,
        AttackSpeedDown,
        CriticalProbUp,
        CriticalProbDown,
        Slow,
        Entangle,
        Freezing,
        Stun,
        Provocation,
        Sleep,
        DeathGoldUp, // 나에겐 버프 너에겐 디버프라 디버프로 처리

        Invincibility,
        Paint,
        MAX,
    }

    public enum DamageReturnType
    {
        AlreadyDead,
        Killed,
        Damaging,
    }

    /// <summary>
    /// 애니메이션 키
    /// </summary>
    public enum AnimationKey
    {
        Idle,
        Walk,
        Attack,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5,
        Death,
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
        ExecuteEnd,
    }

    public enum InGameEffectAnimationKey
    {
        Spawn,
        Idle,
        Broken,
        Disappear,
    }

    public enum AttackType
    {
        Melee = 1,
        Projectile = 2,
    }
}
