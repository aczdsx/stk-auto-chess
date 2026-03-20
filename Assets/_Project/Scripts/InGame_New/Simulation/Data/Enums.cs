namespace CookApps.AutoChess
{
    /// <summary>게임 페이즈</summary>
    public enum GamePhase : byte
    {
        Preparation,   // 준비 (유닛 배치, 상점, 아이템)
        Combat,        // 전투 (자동 전투)
        Result,        // 결과 (데미지, 보상, 탈락 체크)
        SharedDraft,   // 공유 드래프트 (캐러셀)
    }

    /// <summary>게임 모드</summary>
    public enum GameModeType : byte
    {
        ClassicBattle,  // 1인 - 보유 캐릭터로 전투 (상점/경제 없음)
        PvECampaign,    // 1인 - 스토리/캠페인 (상점/경제 있음)
        Competitive,    // 4인 - 경쟁 멀티플레이 (풀 시스템)
    }

    /// <summary>유닛 위치</summary>
    public enum UnitLocation : byte
    {
        None,
        Board,
        Bench,
    }

    /// <summary>전투 유닛 상태</summary>
    public enum CombatState : byte
    {
        Idle,
        Moving,
        Attacking,
        CastingSkill,
        CrowdControlled,
        Dead,
    }

    /// <summary>데미지 타입</summary>
    public enum DamageType : byte
    {
        Physical,  // 물리 (방어력으로 감소)
        Magical,   // 마법 (마법저항으로 감소)
        True,      // 고정 (감소 없음)
    }

    /// <summary>투사체 타입</summary>
    public enum ProjectileType : byte
    {
        None,
        Homing,      // 추적 (단일 대상)
        Linear,      // 직선 관통 (경로 상 모든 적)
        AreaTarget,  // 지점 폭발 (범위 데미지)
    }

    /// <summary>투사체 히트 행동 — 유닛에 닿았을 때 무엇을 하는가</summary>
    public enum ProjectileHitBehavior : byte
    {
        None,          // VFX 전용, 유닛 영향 없음 (기존 NoHit=true)
        DamageEnemy,   // 적에게 데미지 (기존 NoHit=false)
        HealAlly,      // 아군에게 힐
    }

    /// <summary>라운드 타입</summary>
    public enum RoundType : byte
    {
        PvE,          // 크립 라운드
        PvP,          // 플레이어 대전
        SharedDraft,  // 공유 드래프트
    }

    /// <summary>스킬 타겟 타입</summary>
    public enum SkillTargetType : byte
    {
        CurrentTarget,
        NearestEnemy,
        FarthestEnemy,
        LowestHPEnemy,
        AllEnemies,
        AllAllies,
        Self,
        Area,
        RandomEnemies,
        HighestAttackEnemy,
    }

    /// <summary>커맨드 타입 (플레이어 입력)</summary>
    public enum CommandType : byte
    {
        PlaceUnit,          // 벤치 → 보드 배치
        MoveUnit,           // 보드 내 이동
        WithdrawUnit,       // 보드 → 벤치 회수
        SwapUnits,          // 두 유닛 위치 교환
        BuyUnit,            // 상점에서 구매
        SellUnit,           // 유닛 판매
        RerollShop,         // 상점 리롤
        LockShop,           // 상점 잠금/해제
        BuyXP,              // XP 구매
        Ready,              // 준비 완료
        UseCommanderSkill,  // 커맨더 스킬 사용
        EquipItem,          // 아이템 장착
        UnequipItem,        // 아이템 해제
        SpawnTutorialEnemy, // 튜토리얼 적 스폰
        SetSynergyPrepTarget, // Param0=traitId, Param1=targetEntityId
    }

    /// <summary>군중제어(CC) 타입</summary>
    public enum CrowdControlType : byte
    {
        None,
        Stun,        // 기절 (행동 불가)
        Silence,     // 침묵 (스킬 사용 불가)
        Airborne,    // 에어본 (이동+행동 불가)
        Knockback,   // 넉백 (강제 이동)
        Slow,        // 슬로우 (이동/공속 감소)
        Freeze,      // 빙결 (행동 불가)
        Taunt,       // 도발 (강제 타겟)
    }

    /// <summary>버프/디버프 효과 타입</summary>
    public enum StatModifierType : byte
    {
        FlatAdd,       // 고정값 추가
        PercentAdd,    // 퍼센트 추가 (기본값 기준)
        PercentMult,   // 퍼센트 곱연산
    }

    /// <summary>스킬 헬퍼용 스탯 수정 대상</summary>
    public enum StatModType : byte
    {
        None,               // 스탯 무관 (비-스탯 이펙트용)
        Attack,
        Def,                // DEF (최종 데미지 감산)
        AttackSpeed,
        ManaRegenRate,  // 마나 리젠 속도 % 보너스
        MaxMana,        // 최대 마나 증감
        DodgeChance,    // 회피율 증감
        
        AtkPierce,      // 물리 관통 (0-100)
        ResPierce,      // 마법 관통 (0-100)
        
        CritRate,       // 크리 확률 (0-100)
        CritPower,      // 크리 배율 (150 = 1.5x)
        AdReduce,       // 물리 저항률 (정수 퍼센트)
        ApReduce,       // 마법 저항률 (정수 퍼센트)
        HealPower,      // 힐파워 (정수 퍼센트)
        LifeSteal,      // 생명력 흡수 (퍼센트)

        HitChance,      // 명중률 (퍼센트)
        MaxHP,          // 최대 HP 증감
        MoveSpeed,      // 이동속도 증감
    }

    /// <summary>상태효과 타입 (통합 StatusEffect 시스템)</summary>
    public enum StatusEffectType : byte
    {
        Shield,          // 보호막 (Value=쉴드량, 데미지 흡수)
        DamageOverTime,  // 도트 데미지 (Value=틱당 데미지)
        HealOverTime,    // 지속 회복 (Value=틱당 회복량)
        StatBuff,        // 스탯 증가 (만료 시 자동 역산)
        StatDebuff,      // 스탯 감소 (만료 시 자동 역산)
        CCImmunity,      // CC 면역 (모든 CC 차단)
        DOTImmunity,     // DOT 면역 (DamageOverTime 차단)
        DebuffImmunity,  // 디버프 면역 (StatDebuff 차단)
        SkillMarker,     // 범용 스킬 마커 (개별 타이머, Value=(int)SkillMarkerType으로 식별)
        HealReduction,   // 회복량 감소 (Value=감소 퍼센트)
        Silence,         // 침묵 (스킬 사용 불가, CC가 아닌 디버프로 처리)
        Slow,            // 슬로우 (공속 감소, Value=감소량, 만료 시 역산)
        Taunt,           // 도발 (강제 타겟, Value=도발자 CombatId)
        TargetImpossible, // 지정불가 (적이 타겟으로 선택 불가)
    }

    // ═══════════════════════════════════════════════
    //  시너지 시스템
    // ═══════════════════════════════════════════════

    /// <summary>
    /// 특성 카테고리.
    /// Origin = 속성 (FIRE, WIND, LIGHTNING, EARTH, WATER 등 원소 시너지, SynergyType 1-6)
    /// Class  = 성군 (NOBLESSE, TROUBLESHOOTER, SUPERNOVA 등 Asterism 시너지, SynergyType 7+)
    /// </summary>
    public enum TraitCategory : byte
    {
        Origin,  // 속성 — 원소 시너지 (스탯 버프)
        Class,   // 성군 — Asterism 시너지 (CombatTraitBase 행동)
    }

    /// <summary>시너지 효과 대상</summary>
    public enum SynergyTarget : byte
    {
        TraitUnits,  // 해당 특성을 가진 유닛만
        AllAllies,   // 모든 아군
        AllEnemies,  // 모든 적군 (디버프용)
        PrepTarget,  // PrepBehavior가 지정한 단일 유닛
    }

    /// <summary>시너지 효과 타입</summary>
    public enum SynergyEffectType : byte
    {
        // 스탯 보너스 (고정값)
        BonusDef,
        BonusAdReduce,
        BonusApReduce,
        BonusAttack,
        BonusHP,
        BonusAttackSpeed,
        BonusMana,
        BonusCritChance,
        BonusCritMultiplier,
        // 스탯 보너스 (퍼센트)
        BonusAttackPercent,
        BonusHPPercent,
        BonusAttackSpeedPercent,
        BonusDefPercent,
        BonusAdReducePercent,
        BonusApReducePercent,
        BonusMoveSpeedPercent,
        BonusPiercePercent,     // 물리+마법 관통 동시 적용
        // 특수 효과
        StartingMana,
        SpellDamagePercent,
        LifeSteal,
        DodgeChance,
        BacklineJump,
        ShieldOnCombatStart,
        // 디버프 (적군 대상)
        ReduceDef,
        ReduceAdReduce,
        ReduceApReduce,
    }

    // ═══════════════════════════════════════════════
    //  아이템 시스템
    // ═══════════════════════════════════════════════

    /// <summary>스킬 실행 패턴 (base에서 IsChanneling/GetCastFrames/OnChannelTick 자동 판별)</summary>
    public enum SkillExecutionType : byte
    {
        Instant,        // Execute() 즉시 완결, 채널링 없음 찍
        DelayedApply,   // SkillHitFrames[0] 후 ApplySkillEffect() 1회 애님키프레임이벤트를 꼭 받아야한다면
        Channeling,     // Execute() 초기화 + OnChannelTick() 매 프레임 관리를 해야한다면.. ex 미노가 프로젝타일을 날려서 데미지주는 타이밍계산
    }

    /// <summary>시뮬레이션 스킬 아키타입</summary>
    public enum SimSkillArchetype : byte
    {
        SingleDamage,
        AoEDamage,
        LineDamage,
        DamageCC,
        ConeDamage,
        PatternDamage,
        MultiHit,
        Heal,
        MultiTargetHeal,
        TeleportStrike,
        Buff,
        Debuff,
        Stun,
        DiamondAoE,
        Custom,
    }

    /// <summary>스킬 전용 마커 타입 (StatusEffect.SkillMarker의 Value로 사용)</summary>
    public enum SkillMarkerType : int
    {
        None = 0,
        RukidaFoxfire = 1,
        MisaRestraint = 2,
        MarieAracne = 3,
        OdetteCold = 4,
        PiliaSkillCast = 5,   // 필리아 스킬 데미지 판별용
    }

    /// <summary>아이템 위치</summary>
    public enum ItemLocation : byte
    {
        Inventory,  // 인벤토리 (미장착)
        Equipped,   // 유닛에 장착됨
    }

    /// <summary>전투 VFX 트리거 타입 (버프/디버프/CC). 명시적 값 고정 — SO 직렬화 안전.</summary>
    public enum CombatVfxType : byte
    {
        None = 0,
        // ── 버프 계열 ──
        StatBuff            = 1,   // 스탯 버프 (StatModType으로 세분화)
        ContinuousHeal      = 5,
        CCImmunity          = 6,
        DOTImmunity         = 7,
        DebuffImmunity      = 8,
        // ── 디버프 계열 ──
        StatDebuff          = 9,   // 스탯 디버프 (StatModType으로 세분화)
        ContinuousDamage    = 13,
        // ── CC 계열 (기존 직렬화 값 유지) ──
        CC_Stun     = 14,
        CC_Silence  = 15,
        CC_Slow     = 16,
        CC_Freeze   = 17,
        CC_Taunt    = 18,
        CC_Airborne = 19,
        CC_KnockBack = 20,
        CC_TargetImpossible = 21,
        // ── 추가 ──
        HealAmountDown = 30,
        Shield = 31,
    }

    /// <summary>아이템 특수 효과 타입</summary>
    public enum ItemEffectType : byte
    {
        None,
        LifeSteal,          // 물리 흡혈
        SpellVamp,          // 마법 흡혈
        ReflectDamage,      // 피격 시 반사
        OnHitMagicDamage,   // 적중 시 추가 마법 데미지
        ShieldOnLowHP,      // 저HP 보호막
        ManaRefund,         // 스킬 사용 시 마나 환불
        BurnOnHit,          // 적중 시 화상
        AntiHeal,           // 회복 감소
        ExtraAttack,        // N회 공격마다 추가 타격
        CCImmunity,         // CC 면역 (1회)
        DodgeChance,        // 회피 확률
        Cleave,             // 광역 공격
    }
}
