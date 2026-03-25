namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투 특성(Trait) 추상 베이스 클래스.
    /// 레거시 EffectCode 시스템의 60+ 콜백을 핵심 10개로 정리.
    /// 유닛별 최대 MaxTraitsPerUnit개 부착 가능.
    /// </summary>
    public abstract class CombatTraitBase
    {
        public const int MaxTraitsPerUnit = 8;

        // 시너지에서 생성된 trait 식별용 (-1이면 시너지 아님)
        public int SynergyTraitId = -1;
        public int PrepTargetEntityId = -1;
        public int PrepParam0;
        public int PrepParam1;

        /// <summary>전투 시작 시 1회 호출</summary>
        public virtual void OnCombatStart(CombatMatchState state, ref CombatUnit owner) { }

        /// <summary>매 전투 틱마다 호출</summary>
        public virtual void OnTick(CombatMatchState state, ref CombatUnit owner, int tickRate) { }

        /// <summary>공격자 측: 나가는 데미지 보정. 데미지 값 반환.</summary>
        public virtual int ModifyOutgoingDamage(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target, int damage, DamageType damageType) => damage;

        /// <summary>피격자 측: 들어오는 데미지 보정. 데미지 값 반환.</summary>
        public virtual int ModifyIncomingDamage(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target, int damage, DamageType damageType, bool isBasicAttack = false) => damage;

        /// <summary>데미지를 받은 후 호출 (피격자 기준)</summary>
        public virtual void OnDamageTaken(CombatMatchState state, ref CombatUnit owner,
            ref CombatUnit attacker, int damage) { }

        /// <summary>적 처치 시 호출 (공격자 기준)</summary>
        public virtual void OnKill(CombatMatchState state, ref CombatUnit killer,
            ref CombatUnit victim) { }

        /// <summary>사망 시 호출 (사망자 기준)</summary>
        public virtual void OnDeath(CombatMatchState state, ref CombatUnit owner,
            ref CombatUnit killer) { }

        /// <summary>기본 공격 전 호출</summary>
        public virtual void OnPreAttack(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target) { }

        /// <summary>기본 공격 후 호출</summary>
        public virtual void OnPostAttack(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target) { }

        /// <summary>크리티컬 발동 시 호출</summary>
        public virtual void OnCritical(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target, int damage) { }

        /// <summary>힐 받을 때 호출</summary>
        public virtual void OnHeal(CombatMatchState state, ref CombatUnit owner, int healAmount) { }

        /// <summary>풀 반환 시 상태 초기화</summary>
        public virtual void Reset() { }
    }
}
