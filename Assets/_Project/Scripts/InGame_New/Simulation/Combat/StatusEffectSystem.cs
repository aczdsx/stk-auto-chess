namespace CookApps.AutoChess
{
    /// <summary>
    /// 통합 상태효과 시스템. 쉴드/DOT/버프/디버프의 지속시간과 스태킹을 관리.
    /// CombatMatchState.StatusEffects[] 배열을 사용하여 GC 없이 동작.
    /// </summary>
    public static class StatusEffectSystem
    {
        /// <summary>상태효과 추가. StatBuff/Debuff는 추가 시 스탯 즉시 적용.</summary>
        public static void AddEffect(CombatMatchState state, int unitIndex,
            StatusEffectType type, int value, int durationFrames,
            int tickInterval = 0, StatModType statType = default, DamageType dmgType = default)
        {
            if (unitIndex < 0 || unitIndex >= state.UnitCount) return;
            if (state.StatusEffectCount >= CombatMatchState.MaxStatusEffects) return;

            ref var effect = ref state.StatusEffects[state.StatusEffectCount++];
            effect.OwnerUnitIndex = unitIndex;
            effect.Type = type;
            effect.Value = value;
            effect.RemainingFrames = durationFrames;
            effect.TickInterval = tickInterval;
            effect.TickTimer = tickInterval;
            effect.StatType = statType;
            effect.DmgType = dmgType;
            effect.IsActive = true;

            // 로깅
            if (CombatLogger.Enabled)
            {
                int uid = state.Units[unitIndex].CombatId;
                if (type == StatusEffectType.Shield)
                    CombatLogger.LogShieldAdd(uid, value);
            }

            // 즉시 효과 적용
            ref var unit = ref state.Units[unitIndex];
            switch (type)
            {
                case StatusEffectType.Shield:
                    RecalcShieldCache(state, unitIndex);
                    break;
                case StatusEffectType.StatBuff:
                    SkillBuffHelper.ModifyStat(ref unit, statType, value);
                    break;
                case StatusEffectType.StatDebuff:
                    SkillBuffHelper.ModifyStat(ref unit, statType, -value);
                    break;
            }
        }

        /// <summary>매 틱 호출: 지속시간 감소, 주기적 효과 적용, 만료 처리</summary>
        public static void Tick(CombatMatchState state)
        {
            // 변경된 유닛의 쉴드 재계산용 비트마스크 (최대 32유닛)
            int shieldDirtyMask = 0;

            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;

                // 소유 유닛이 죽었으면 효과 제거
                if (effect.OwnerUnitIndex >= 0 && effect.OwnerUnitIndex < state.UnitCount)
                {
                    if (!state.Units[effect.OwnerUnitIndex].IsAlive)
                    {
                        effect.IsActive = false;
                        continue;
                    }
                }

                // 영구 효과(-1)는 지속시간 감소 스킵
                if (effect.RemainingFrames > 0)
                {
                    effect.RemainingFrames--;

                    if (effect.RemainingFrames <= 0)
                    {
                        // 만료 처리
                        OnEffectExpired(state, ref effect);
                        effect.IsActive = false;

                        if (effect.Type == StatusEffectType.Shield)
                            shieldDirtyMask |= (1 << effect.OwnerUnitIndex);

                        continue;
                    }
                }

                // 주기적 효과 (DOT/HOT)
                if (effect.TickInterval > 0)
                {
                    effect.TickTimer--;
                    if (effect.TickTimer <= 0)
                    {
                        effect.TickTimer = effect.TickInterval;
                        ApplyPeriodicEffect(state, ref effect);
                    }
                }
            }

            // 변경된 유닛의 쉴드 캐시 재계산
            if (shieldDirtyMask != 0)
            {
                for (int u = 0; u < state.UnitCount; u++)
                {
                    if ((shieldDirtyMask & (1 << u)) != 0)
                        RecalcShieldCache(state, u);
                }
            }
        }

        /// <summary>쉴드 데미지 흡수 (FIFO 순서). 남은 데미지 반환.</summary>
        public static int AbsorbShieldDamage(CombatMatchState state, int unitIndex, int damage)
        {
            if (damage <= 0) return 0;

            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.Type != StatusEffectType.Shield) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;

                if (effect.Value >= damage)
                {
                    effect.Value -= damage;
                    RecalcShieldCache(state, unitIndex);
                    return 0; // 완전 흡수
                }

                damage -= effect.Value;
                effect.Value = 0;
                effect.IsActive = false;
            }

            RecalcShieldCache(state, unitIndex);
            return damage; // 남은 데미지
        }

        /// <summary>해당 유닛의 쉴드 합산 재계산 → CombatUnit.ShieldAmount 갱신</summary>
        private static void RecalcShieldCache(CombatMatchState state, int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= state.UnitCount) return;

            int total = 0;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.Type != StatusEffectType.Shield) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                total += effect.Value;
            }

            state.Units[unitIndex].ShieldAmount = total;
        }

        /// <summary>만료 시 처리 (스탯 역산 등)</summary>
        private static void OnEffectExpired(CombatMatchState state, ref StatusEffect effect)
        {
            if (effect.OwnerUnitIndex < 0 || effect.OwnerUnitIndex >= state.UnitCount) return;
            ref var unit = ref state.Units[effect.OwnerUnitIndex];
            if (!unit.IsAlive) return;

            switch (effect.Type)
            {
                case StatusEffectType.StatBuff:
                    // 역산: 추가했던 값을 빼기
                    SkillBuffHelper.ModifyStat(ref unit, effect.StatType, -effect.Value);
                    break;
                case StatusEffectType.StatDebuff:
                    // 역산: 뺐던 값을 더하기
                    SkillBuffHelper.ModifyStat(ref unit, effect.StatType, effect.Value);
                    break;
            }
        }

        /// <summary>주기적 효과 적용 (DOT/HOT)</summary>
        private static void ApplyPeriodicEffect(CombatMatchState state, ref StatusEffect effect)
        {
            if (effect.OwnerUnitIndex < 0 || effect.OwnerUnitIndex >= state.UnitCount) return;
            ref var unit = ref state.Units[effect.OwnerUnitIndex];
            if (!unit.IsAlive) return;

            switch (effect.Type)
            {
                case StatusEffectType.DamageOverTime:
                    int dmg = DamageSystem.CalculateDamage(effect.Value, effect.DmgType, ref unit);
                    DamageSystem.ApplyDamage(state, ref unit, dmg);
                    break;
                case StatusEffectType.HealOverTime:
                    SkillDamageHelper.Heal(ref unit, effect.Value);
                    break;
            }
        }
    }
}
