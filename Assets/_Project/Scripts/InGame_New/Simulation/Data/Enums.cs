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
        Attack,
        Armor,
        MagicResist,
        AttackSpeed,
    }

    /// <summary>상태효과 타입 (통합 StatusEffect 시스템)</summary>
    public enum StatusEffectType : byte
    {
        Shield,          // 보호막 (Value=쉴드량, 데미지 흡수)
        DamageOverTime,  // 도트 데미지 (Value=틱당 데미지)
        HealOverTime,    // 지속 회복 (Value=틱당 회복량)
        StatBuff,        // 스탯 증가 (만료 시 자동 역산)
        StatDebuff,      // 스탯 감소 (만료 시 자동 역산)
    }

    // ═══════════════════════════════════════════════
    //  시너지 시스템
    // ═══════════════════════════════════════════════

    /// <summary>특성 카테고리</summary>
    public enum TraitCategory : byte
    {
        Origin,  // 출신 (Human, Elf, Dragon, ...)
        Class,   // 직업 (Warrior, Mage, Assassin, ...)
    }

    /// <summary>시너지 효과 대상</summary>
    public enum SynergyTarget : byte
    {
        TraitUnits,  // 해당 특성을 가진 유닛만
        AllAllies,   // 모든 아군
        AllEnemies,  // 모든 적군 (디버프용)
    }

    /// <summary>시너지 효과 타입</summary>
    public enum SynergyEffectType : byte
    {
        // 스탯 보너스 (고정값)
        BonusArmor,
        BonusMagicResist,
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
        // 특수 효과
        StartingMana,
        SpellDamagePercent,
        LifeSteal,
        DodgeChance,
        BacklineJump,
        ShieldOnCombatStart,
        DamageReduction,
        // 디버프 (적군 대상)
        ReduceArmor,
        ReduceMagicResist,
    }

    // ═══════════════════════════════════════════════
    //  아이템 시스템
    // ═══════════════════════════════════════════════

    /// <summary>아이템 위치</summary>
    public enum ItemLocation : byte
    {
        Inventory,  // 인벤토리 (미장착)
        Equipped,   // 유닛에 장착됨
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
