namespace CookApps.AutoChess
{
    /// <summary>스킬 초기화 파라미터</summary>
    public struct SkillParams
    {
        public int SkillId;
        public int PowerPercent;
        public DamageType DamageType;
        public int CastFrames;
        public int Param0;
        public int Param1;
        public int Param2;
        public int Param3;
    }

    /// <summary>
    /// 시뮬레이션 스킬 추상 베이스.
    /// 구체 스킬 클래스가 상속하여 타겟 선택과 효과 적용을 구현.
    /// </summary>
    public abstract class SimSkillBase
    {
        public int SkillId { get; private set; }
        protected int PowerPercent;
        protected DamageType DamageType;
        protected int CastFrames;

        public virtual void Initialize(SkillParams p)
        {
            SkillId = p.SkillId;
            PowerPercent = p.PowerPercent;
            DamageType = p.DamageType;
            CastFrames = p.CastFrames;
        }

        /// <summary>시전 가능 여부 (마나 외 추가 조건)</summary>
        public virtual bool CanCast(CombatMatchState state, ref CombatUnit caster) => true;

        /// <summary>타겟 선택 (CombatId 반환, -1이면 타겟 없음)</summary>
        public abstract int SelectTarget(CombatMatchState state, ref CombatUnit caster);

        /// <summary>시전 시간 (프레임)</summary>
        public virtual int GetCastFrames() => CastFrames;

        /// <summary>효과 적용</summary>
        public abstract void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng);

        /// <summary>풀 반환 시 초기화</summary>
        public virtual void Reset() { }
    }
}
