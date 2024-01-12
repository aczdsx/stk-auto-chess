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

    public enum AnimationEventKey
    {
        Start,
        End,
        Effect1,
        Effect2,
        Effect3,
        Effect4,
        Effect5,

        ActivateStart = 1000,
        Activate1Per1,
        Activate1Per2,
        Activate1Per3,
        Activate1Per4,
        Activate1Per5,
        Activate1Per6,
        Activate1Per7,
        Activate1Per8,
        Activate1Per9,
        Activate1Per10,
        Activate1Per11,
        Activate1Per12,
        ActivateEnd,
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

    public enum AnimationKey
    {
        Idle,
        Walk,
        Attack,
        Skill,
        Skill2,
        Skill3,
        Skill4,
        Skill5,
        Death,
        Spawn,
        Crying,
        LongCrying,
        Attack_1,
        Idle_1,
        Walk_1,
    }

    public enum InGameEffectAnimationKey
    {
        Spawn,
        Idle,
        Broken,
        Disappear,
    }
}
