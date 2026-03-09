namespace CookApps.AutoChess
{
    /// <summary>
    /// Trait 콜백 디스패치 시스템. 유닛에 부착된 모든 Trait의 콜백을 순회 호출.
    /// </summary>
    public static class TraitSystem
    {
        /// <summary>유닛에 Trait 부착. 성공 시 true.</summary>
        public static bool AddTrait(CombatMatchState state, int unitIndex, CombatTraitBase trait)
        {
            int count = state.TraitCounts[unitIndex];
            if (count >= CombatTraitBase.MaxTraitsPerUnit) return false;
            state.Traits[unitIndex][count] = trait;
            state.TraitCounts[unitIndex] = count + 1;
            return true;
        }

        /// <summary>전투 시작 시 모든 유닛의 OnCombatStart 호출</summary>
        public static void InvokeCombatStart(CombatMatchState state)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                int traitCount = state.TraitCounts[i];
                if (traitCount == 0) continue;
                for (int t = 0; t < traitCount; t++)
                    state.Traits[i][t].OnCombatStart(state, ref state.Units[i]);
            }
        }

        /// <summary>매 틱 호출</summary>
        public static void InvokeOnTick(CombatMatchState state, int unitIndex, int tickRate)
        {
            int traitCount = state.TraitCounts[unitIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[unitIndex][t].OnTick(state, ref state.Units[unitIndex], tickRate);
        }

        /// <summary>나가는 데미지 보정 (공격자 기준)</summary>
        public static int InvokeModifyOutgoingDamage(CombatMatchState state, int attackerIndex,
            ref CombatUnit target, int damage, DamageType damageType)
        {
            int traitCount = state.TraitCounts[attackerIndex];
            for (int t = 0; t < traitCount; t++)
                damage = state.Traits[attackerIndex][t].ModifyOutgoingDamage(
                    state, ref state.Units[attackerIndex], ref target, damage, damageType);
            return damage;
        }

        /// <summary>들어오는 데미지 보정 (피격자 기준)</summary>
        public static int InvokeModifyIncomingDamage(CombatMatchState state, ref CombatUnit attacker,
            int targetIndex, int damage, DamageType damageType)
        {
            int traitCount = state.TraitCounts[targetIndex];
            for (int t = 0; t < traitCount; t++)
                damage = state.Traits[targetIndex][t].ModifyIncomingDamage(
                    state, ref attacker, ref state.Units[targetIndex], damage, damageType);
            return damage;
        }

        /// <summary>데미지를 받은 후 (피격자 기준)</summary>
        public static void InvokeOnDamageTaken(CombatMatchState state, int targetIndex,
            ref CombatUnit attacker, int damage)
        {
            int traitCount = state.TraitCounts[targetIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[targetIndex][t].OnDamageTaken(
                    state, ref state.Units[targetIndex], ref attacker, damage);
        }

        /// <summary>적 처치 시 (공격자 기준)</summary>
        public static void InvokeOnKill(CombatMatchState state, int killerIndex, ref CombatUnit victim)
        {
            int traitCount = state.TraitCounts[killerIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[killerIndex][t].OnKill(state, ref state.Units[killerIndex], ref victim);
        }

        /// <summary>사망 시 (사망자 기준)</summary>
        public static void InvokeOnDeath(CombatMatchState state, int ownerIndex, ref CombatUnit killer)
        {
            int traitCount = state.TraitCounts[ownerIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[ownerIndex][t].OnDeath(state, ref state.Units[ownerIndex], ref killer);
        }

        /// <summary>기본 공격 전</summary>
        public static void InvokeOnPreAttack(CombatMatchState state, int attackerIndex, ref CombatUnit target)
        {
            int traitCount = state.TraitCounts[attackerIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[attackerIndex][t].OnPreAttack(state, ref state.Units[attackerIndex], ref target);
        }

        /// <summary>기본 공격 후</summary>
        public static void InvokeOnPostAttack(CombatMatchState state, int attackerIndex, ref CombatUnit target)
        {
            int traitCount = state.TraitCounts[attackerIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[attackerIndex][t].OnPostAttack(state, ref state.Units[attackerIndex], ref target);
        }

        /// <summary>크리티컬 발동 시</summary>
        public static void InvokeOnCritical(CombatMatchState state, int attackerIndex,
            ref CombatUnit target, int damage)
        {
            int traitCount = state.TraitCounts[attackerIndex];
            for (int t = 0; t < traitCount; t++)
                state.Traits[attackerIndex][t].OnCritical(
                    state, ref state.Units[attackerIndex], ref target, damage);
        }
    }
}
