namespace CookApps.AutoChess
{
    /// <summary>
    /// Asterism 시너지 행동 추상 베이스.
    /// 원소 시너지(스탯만)와 달리 복잡한 전투 중 행동을 정의.
    /// 팀 단위로 동작 (유닛 단위 CombatTraitBase와 구분).
    /// </summary>
    public abstract class SynergyBehaviorBase
    {
        public int TraitId;
        public byte Tier;
        public byte TeamIndex;

        // 준비 페이즈에서 전달받은 데이터
        public int PrepTargetEntityId = -1;
        public int PrepParam0;
        public int PrepParam1;

        /// <summary>전투 시작 시 1회 (스탯 적용 후)</summary>
        public virtual void OnCombatStart(CombatMatchState state) { }

        /// <summary>매 전투 틱</summary>
        public virtual void OnTick(CombatMatchState state) { }

        /// <summary>아군 유닛이 기본공격 시</summary>
        public virtual void OnAllyAttack(CombatMatchState state,
            ref CombatUnit attacker, ref CombatUnit target) { }

        /// <summary>아군 유닛이 피격 시</summary>
        public virtual void OnAllyDamaged(CombatMatchState state,
            ref CombatUnit victim, ref CombatUnit attacker, int damage) { }

        /// <summary>아군 유닛이 적 처치 시</summary>
        public virtual void OnAllyKill(CombatMatchState state,
            ref CombatUnit killer, ref CombatUnit victim) { }

        /// <summary>나가는 데미지 보정 (해당 특성 유닛)</summary>
        public virtual int ModifyOutgoingDamage(CombatMatchState state,
            ref CombatUnit attacker, ref CombatUnit target,
            int damage, DamageType damageType) => damage;

        /// <summary>들어오는 데미지 보정 (해당 특성 유닛)</summary>
        public virtual int ModifyIncomingDamage(CombatMatchState state,
            ref CombatUnit attacker, ref CombatUnit target,
            int damage, DamageType damageType) => damage;

        /// <summary>리셋</summary>
        public virtual void Reset() { }

        /// <summary>해당 유닛이 이 시너지의 특성을 가지고 있는지</summary>
        protected bool HasTrait(ref CombatUnit unit)
        {
            return (unit.TraitFlags & (1 << TraitId)) != 0;
        }

        /// <summary>해당 유닛이 이 시너지의 팀인지</summary>
        protected bool IsMyTeam(ref CombatUnit unit)
        {
            return unit.TeamIndex == TeamIndex;
        }
    }
}
