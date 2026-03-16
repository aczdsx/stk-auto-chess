using System.Collections.Generic;
using CookApps.AutoBattler;

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
        public SkillTargetType TargetType;

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

        /// <summary>GameWorld.TickRate — 모드별 상이하므로 초→프레임 변환 시 사용</summary>
        public int WorldTickRate;
    }

    /// <summary>
    /// 시뮬레이션 스킬 추상 베이스.
    /// 구체 스킬 클래스가 상속하여 타겟 선택과 효과 적용을 구현.
    /// </summary>
    public abstract class SimSkillBase
    {
        public int SkillId { get; private set; }

        /// <summary>스킬 실행 패턴. 서브클래스에서 override하여 지정.</summary>
        public virtual SkillExecutionType ExecutionType => SkillExecutionType.Instant;
        protected int PowerPercent;

        // ── DelayedApply 지원 ──
        private int _delayTimer = -1;
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

        // 타겟팅
        protected SkillTargetType TargetType;

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

            TargetType = p.TargetType;
            TargetCount = p.TargetCount <= 0 ? 1 : p.TargetCount;
            HitCount = p.HitCount <= 0 ? 1 : p.HitCount;

            SkillHitFrames = p.SkillHitFrames;
            SkillClipFrames = p.SkillClipFrames;
        }

        /// <summary>
        /// 스펙 리스트를 직접 받아 초기화. 커스텀 스킬이 override하여 자체 스펙 파싱.
        /// 기본 구현은 Initialize(baseParams)만 호출하므로 미마이그레이션 스킬도 정상 동작.
        /// </summary>
        public virtual void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            Initialize(baseParams);
        }

        /// <summary>시전 가능 여부 (마나 외 추가 조건)</summary>
        public virtual bool CanCast(CombatMatchState state, ref CombatUnit caster) => true;

        /// <summary>타겟 선택 (CombatId 반환, -1이면 타겟 없음)</summary>
        public abstract int SelectTarget(CombatMatchState state, ref CombatUnit caster);

        /// <summary>
        /// true이면 "지연 1회 발동" 패턴을 base가 자동 처리.
        /// SkillHitFrames[0] 프레임 대기 후 ApplySkillEffect() 1회 호출.
        /// ExecutionType == DelayedApply일 때 자동 true.
        /// </summary>
        protected virtual bool IsDelayedSingleApply => ExecutionType == SkillExecutionType.DelayedApply;

        /// <summary>IsDelayedSingleApply=true일 때, 딜레이 후 호출되는 실제 효과.</summary>
        protected virtual void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng) { }

        /// <summary>
        /// Execute() 호출 전 대기 프레임 수.
        ///
        /// ■ 반환값 > 0 : SkillSystem이 SkillCastTimer를 세팅하고,
        ///   매 프레임 카운트다운한 뒤 0이 되면 Execute()를 호출한다.
        ///   → 단순 "N프레임 뒤 1회 발동" 스킬에 적합.
        ///
        /// ■ 반환값 == 0 : TryCast() 시점에 Execute()를 즉시 호출한다.
        ///   이때 IsChanneling이 true이면 상태를 CastingSkill로 유지하여
        ///   다음 프레임부터 OnChannelTick()이 매 프레임 호출된다.
        ///   → "즉시 초기화 + 프레임 단위 자유 제어" 스킬에 적합.
        ///
        /// 기본 우선순위: DelayedApply/Channeling → 0 > CastFrames > SkillHitFrames[0] > 0
        /// </summary>
        public virtual int GetCastFrames()
        {
            if (ExecutionType == SkillExecutionType.DelayedApply ||
                ExecutionType == SkillExecutionType.Channeling)
                return 0;
            if (CastFrames > 0) return CastFrames;
            if (SkillHitFrames != null && SkillHitFrames.Length > 0) return SkillHitFrames[0];
            return 0;
        }

        /// <summary>효과 적용</summary>
        public abstract void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng);

        /// <summary>
        /// 채널링 스킬 여부.
        ///
        /// ■ false (기본값) : Execute() 1회 호출 후 즉시 Idle로 전환.
        ///   타이밍 제어는 GetCastFrames()의 SkillCastTimer에만 의존.
        ///
        /// ■ true : Execute() 호출 후에도 CastingSkill 상태를 유지하며,
        ///   매 프레임 OnChannelTick()을 호출한다.
        ///   OnChannelTick()이 false를 반환하면 채널링 종료 → Idle.
        ///   SkillCastTimer는 사용되지 않으며, 스킬 내부에서 자체 타이머로 제어.
        ///
        /// ── 대표 조합 ──
        /// GetCastFrames()=0 + IsChanneling=true  (커스텀 스킬 대부분)
        ///   → Execute()에서 타이머/상태 초기화만 수행,
        ///     OnChannelTick()에서 SkillHitFrames[] 타이밍에 맞춰 효과 적용.
        ///     지연 1회 발동, 다단히트, 반복 틱 등 자유로운 시간 제어 가능.
        /// </summary>
        public virtual bool IsChanneling => ExecutionType != SkillExecutionType.Instant;

        /// <summary>
        /// 투사체를 발사하는 스킬 여부.
        /// true인 경우 View의 OnUnitCastSkill에서 정적 스킬 VFX를 생성하지 않음.
        /// 투사체 전용 프리팹이 없는 캐릭터(아트레시아 등)는 OnProjectileSpawned에서
        /// skillPrefabs[0]을 fallback 투사체 VFX로 사용하는데,
        /// 여기서 정적 VFX까지 생성하면 같은 프리팹이 중복 생성됨.
        /// 이를 시뮬레이션 레벨에서 알려줘야 하는 이유:
        /// UnitCastSkill 이벤트와 ProjectileSpawned 이벤트가 서로 다른 프레임에 도달하므로
        /// View 레이어에서 사후 판단으로는 중복을 방지할 수 없음.
        /// </summary>
        public virtual bool HasProjectile => false;

        /// <summary>채널링 틱 처리. false 반환 시 채널링 종료.</summary>
        public virtual bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (ExecutionType != SkillExecutionType.DelayedApply) return false;

            if (_delayTimer < 0)
                _delayTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                    ? SkillHitFrames[0] : 10;

            _delayTimer--;
            if (_delayTimer > 0) return true;

            ApplySkillEffect(state, ref caster, caster.CurrentTargetId, ref rng);
            return false;
        }

        /// <summary>풀 반환 시 초기화</summary>
        public virtual void Reset()
        {
            _delayTimer = -1;
        }
    }
}
