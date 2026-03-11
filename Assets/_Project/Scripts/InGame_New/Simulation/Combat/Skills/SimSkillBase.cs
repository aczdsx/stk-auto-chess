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

        // CC
        public CrowdControlType CCType;
        public int CCDurationFrames;

        // 버프/디버프
        public StatModType BuffStat;
        public int BuffValue;
        public int BuffDurationFrames;

        // 보조 배율
        public int SecondaryPowerPercent;

        // 멀티타겟 / 다단히트
        public int TargetCount;
        public int HitCount;

        // SKL 클립 타이밍 (프레임 단위, SkillSpecAdapter에서 변환)
        /// <summary>SKL 클립 Execute 이벤트 타이밍 (프레임)</summary>
        public int[] SkillHitFrames;
        /// <summary>SKL 클립 전체 길이 (프레임)</summary>
        public int SkillClipFrames;
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

        // CC
        protected CrowdControlType CCType;
        protected int CCDurationFrames;

        // 버프/디버프
        protected StatModType BuffStat;
        protected int BuffValue;
        protected int BuffDurationFrames;

        // 보조 배율
        protected int SecondaryPowerPercent;

        // 멀티타겟 / 다단히트
        protected int TargetCount;
        protected int HitCount;

        // SKL 클립 타이밍 (프레임 단위, SkillParams에서 미리 변환됨)
        protected int[] SkillHitFrames;
        protected int SkillClipFrames;

        public virtual void Initialize(SkillParams p)
        {
            SkillId = p.SkillId;
            PowerPercent = p.PowerPercent;
            DamageType = p.DamageType;
            CastFrames = p.CastFrames;

            CCType = p.CCType;
            CCDurationFrames = p.CCDurationFrames;

            BuffStat = p.BuffStat;
            BuffValue = p.BuffValue;
            BuffDurationFrames = p.BuffDurationFrames;

            SecondaryPowerPercent = p.SecondaryPowerPercent;

            TargetCount = p.TargetCount <= 0 ? 1 : p.TargetCount;
            HitCount = p.HitCount <= 0 ? 1 : p.HitCount;

            SkillHitFrames = p.SkillHitFrames;
            SkillClipFrames = p.SkillClipFrames;
        }

        /// <summary>시전 가능 여부 (마나 외 추가 조건)</summary>
        public virtual bool CanCast(CombatMatchState state, ref CombatUnit caster) => true;

        /// <summary>타겟 선택 (CombatId 반환, -1이면 타겟 없음)</summary>
        public abstract int SelectTarget(CombatMatchState state, ref CombatUnit caster);

        /// <summary>시전 시간 (프레임). CastFrames 명시 시 우선, 아니면 SkillClipFrames에서 자동 계산.</summary>
        public virtual int GetCastFrames()
        {
            if (CastFrames > 0) return CastFrames;
            if (SkillHitFrames != null && SkillHitFrames.Length > 0) return SkillHitFrames[0];
            return 0;
        }

        /// <summary>효과 적용</summary>
        public abstract void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng);

        /// <summary>채널링 스킬 여부</summary>
        public virtual bool IsChanneling => false;

        /// <summary>채널링 틱 처리. false 반환 시 채널링 종료.</summary>
        public virtual bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
            => false;

        /// <summary>풀 반환 시 초기화</summary>
        public virtual void Reset() { }
    }
}
