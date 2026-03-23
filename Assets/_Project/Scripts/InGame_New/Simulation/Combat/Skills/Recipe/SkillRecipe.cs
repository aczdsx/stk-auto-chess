namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 레시피 — 스킬의 구조적 정의 (코드에서 static으로 존재, deserialize 비용 0).
    /// 밸런스 수치(PowerPercent, CC 지속시간 등)는 SpecData에서 ParamSlots를 통해 주입.
    ///
    /// 사용 흐름:
    /// 1. SkillRecipeRegistry에 static으로 정의
    /// 2. SkillFactory에서 SimSkillGeneric에 주입
    /// 3. SimSkillGeneric.InitializeFromSpec()에서 ParamSlots 기반으로 specList에서 수치 추출
    /// 4. SimSkillGeneric.Execute()/OnChannelTick()에서 Actions를 타이밍에 따라 디스패치
    /// </summary>
    public class SkillRecipe
    {
        /// <summary>스킬 실행 패턴 (Instant, DelayedApply, Channeling)</summary>
        public SkillExecutionType ExecutionType;

        /// <summary>타겟 선정 규칙 (기존 SkillTargetType 재사용)</summary>
        public SkillTargetType TargetRule;

        /// <summary>투사체 발사 스킬 여부 (true이면 View에서 정적 VFX 생성 스킵)</summary>
        public bool HasProjectile;

        /// <summary>액션 배열 — 스킬이 하는 모든 것의 시퀀스</summary>
        public SkillAction[] Actions;

        /// <summary>
        /// specList 인덱스 → 의미 매핑.
        /// specList[ParamSlots[i].SpecIndex].base_rate를 ParamValues[i]에 저장.
        /// 첫 번째 슬롯(index 0)은 관례적으로 PowerPercent.
        /// </summary>
        public ParamSlot[] ParamSlots;
    }

    /// <summary>
    /// 스킬 액션 — 레시피의 한 단계.
    /// "언제, 뭘, 누구에게, 어디서" 를 정의.
    /// ActionExecutor에서 Effect 타입에 따라 기존 Helper를 호출.
    /// </summary>
    public struct SkillAction
    {
        // ── 타이밍 ──
        /// <summary>액션 발동 조건 (OnCast, AtHitFrame, OnTick, OnComplete)</summary>
        public SkillTriggerType Trigger;
        /// <summary>AtHitFrame일 때 SkillHitFrames[N] 인덱스</summary>
        public byte HitFrameIndex;

        // ── 효과 ──
        /// <summary>효과 타입 (Damage, Heal, CC 등). None이면 VFX만 스폰.</summary>
        public SkillEffectType Effect;
        /// <summary>대상 필터 (PrimaryTarget, EnemiesInArea, AlliesInArea 등)</summary>
        public SkillTargetFilter TargetFilter;

        // ── 범위 ──
        /// <summary>범위 형태 (None, Circle, Diamond, Plus 등)</summary>
        public SkillAreaShape AreaShape;
        /// <summary>범위 크기 (반경 타일 수)</summary>
        public byte AreaRange;

        // ── 수치 참조 ──
        /// <summary>ParamSlots 인덱스 (-1이면 base PowerPercent 사용)</summary>
        public sbyte ParamIndex;
        /// <summary>보조 ParamSlots 인덱스 (-1이면 사용 안 함). CC/넉백 거리, 디버프 지속 등.</summary>
        public sbyte SecondaryParamIndex;

        // ── VFX ──
        /// <summary>skill_vfxs[N] 인덱스 (-1이면 VFX 없음)</summary>
        public sbyte VfxIndex;
        /// <summary>VFX 배치 위치 (AtCaster, AtTarget, AtGridPos 등)</summary>
        public SkillVfxPlacement VfxAt;

        // ── 조건 ──
        /// <summary>조건부 실행 (Always, EveryNth2, EveryNth3 등)</summary>
        public SkillActionCondition Condition;

        // ── 반복 (OnTick용) ──
        /// <summary>반복 횟수 (0이면 클립 길이 기반 동적 계산)</summary>
        public byte RepeatCount;
        /// <summary>반복 간격 (프레임 직접 지정). 0이면 RepeatIntervalMs 또는 기본값 사용.</summary>
        public short RepeatIntervalFrames;
        /// <summary>반복 간격 (밀리초, tickRate 기반 자동 변환). RepeatIntervalFrames보다 우선.</summary>
        public short RepeatIntervalMs;
        /// <summary>true이면 SKL 클립 길이에서 틱 수/간격 자동 계산</summary>
        public bool DynamicFromClip;

        // ── CC 전용 ──
        /// <summary>CC 타입 (Stun, Silence, Knockback 등)</summary>
        public CrowdControlType CCType;

        // ── 버프/디버프 전용 ──
        /// <summary>StatModType 기반 버프/디버프 대상 스탯</summary>
        public StatModType BuffStat;
        /// <summary>StatusEffectType 기반 디버프 (HealReduction 등). default(0)이면 StatModType 기반.</summary>
        public StatusEffectType StatusEffect;

        // ── 마커 전용 ──
        /// <summary>SkillMarkerType 값 (StatusEffect.SkillMarker의 하위 분류)</summary>
        public byte MarkerType;
    }

    /// <summary>
    /// specList 인덱스와 의미를 매핑하는 슬롯.
    /// specList[SpecIndex]의 base_rate를 ParamValues[슬롯인덱스]에 저장.
    /// </summary>
    public struct ParamSlot
    {
        /// <summary>specList 인덱스 (0=쿨타임, 1~N=스킬별 파라미터)</summary>
        public byte SpecIndex;
        /// <summary>값 타입 — Int면 RoundToInt, Frames면 초→프레임 변환</summary>
        public ParamValueType ValueType;
        /// <summary>specList에 해당 인덱스가 없을 때 기본값</summary>
        public float Fallback;

        public ParamSlot(byte specIndex, ParamValueType valueType, float fallback)
        {
            SpecIndex = specIndex;
            ValueType = valueType;
            Fallback = fallback;
        }
    }

    /// <summary>ParamSlot 값 변환 타입</summary>
    public enum ParamValueType : byte
    {
        /// <summary>base_rate를 RoundToInt (퍼센트, 카운트 등)</summary>
        Int,
        /// <summary>base_rate(초)를 tickRate 기반 프레임으로 변환</summary>
        Frames,
    }
}
