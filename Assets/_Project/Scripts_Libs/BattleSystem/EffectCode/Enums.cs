namespace CookApps.TeamBattle.BattleSystem
{
    public enum CrowdControlType
    {
        None = 0x0000,
        Airborne = 0x0001,
        Knockback = 0x0003,
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
}
