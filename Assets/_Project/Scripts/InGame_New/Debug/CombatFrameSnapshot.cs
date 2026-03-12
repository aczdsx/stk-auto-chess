namespace CookApps.AutoChess
{
    public class CombatFrameSnapshot
    {
        public int FrameIndex;
        public UnitSnapshot[] Units;
        public int UnitCount;
        public ProjectileSnapshot[] Projectiles;
        public int ProjectileCount;
        public SimEventSnapshot[] Events;
        public int EventCount;
    }

    public struct UnitSnapshot
    {
        public int CombatId;
        public byte GridCol, GridRow;
        public int CurrentHp, MaxHp;
        public int CurrentMana, MaxMana;
        public CombatState State;
        public int CurrentTargetId;
        public byte TeamIndex;
        public int SkillSpecId;
        public int ChampionSpecId;
        public bool IsAlive;
        public int Attack;
        public int ShieldAmount;
        public CrowdControlType ActiveCC;
        public int CCRemainingFrames;
    }

    public struct ProjectileSnapshot
    {
        public int ProjectileId;
        public byte Col, Row;
        public sbyte DirCol, DirRow;
        public int MoveInterval, MoveTimer;
        public ProjectileHitBehavior HitBehavior;
        public int SourceCombatId;
        public ProjectileType Type;
        public int Damage;
        public int TraveledDistance;
        public int MaxDistance;
    }

    public struct SimEventSnapshot
    {
        public SimEventType Type;
        public int SourceId, TargetId;
        public int Value0, Value1;
        public byte Col, Row;
        public string Description;
    }
}
