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

            // 면역 체크: 해당 면역이 활성화되어 있으면 효과 적용 차단
            if (type == StatusEffectType.StatDebuff && HasImmunity(state, unitIndex, StatusEffectType.DebuffImmunity))
                return;
            if (type == StatusEffectType.DamageOverTime && HasImmunity(state, unitIndex, StatusEffectType.DOTImmunity))
                return;

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

            // VFX 이벤트 발행
            var vfxType = ToVfxType(type, statType);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushStatusEffectAdded(unit.CombatId, vfxType, durationFrames);

            // SkillMarker 아이콘 이벤트
            if (type == StatusEffectType.SkillMarker)
                state.EventQueue?.PushSkillMarkerAdded(unit.CombatId, value, durationFrames);
        }

        /// <summary>매 틱 호출: 지속시간 감소, 주기적 효과 적용, 만료 처리</summary>
        public static void Tick(CombatMatchState state)
        {
            // 변경된 유닛의 쉴드 재계산용 비트마스크 (최대 32유닛)
            int shieldDirtyMask = 0;
            // 마커 만료 추적: 유닛별 비트마스크 + 만료된 마커의 Value(스킬ID) 수집
            int markerDirtyMask = 0;
            // 만료된 마커의 (unitIndex, markerValue) 쌍을 수집
            int markerExpCount = 0;
            int[] markerExpUnits = null;
            int[] markerExpValues = null;

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
                        if (effect.Type == StatusEffectType.SkillMarker)
                        {
                            markerDirtyMask |= (1 << effect.OwnerUnitIndex);
                            if (markerExpUnits == null)
                            {
                                markerExpUnits = new int[32];
                                markerExpValues = new int[32];
                            }
                            if (markerExpCount < 32)
                            {
                                markerExpUnits[markerExpCount] = effect.OwnerUnitIndex;
                                markerExpValues[markerExpCount] = effect.Value;
                                markerExpCount++;
                            }
                        }

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

            // 마커 만료 시 범용 VFX 이벤트 (Value 기반 스킬ID + 남은 마커 수)
            if (markerDirtyMask != 0)
            {
                // 중복 (unitIndex, markerValue) 쌍 제거하며 이벤트 발행
                for (int e = 0; e < markerExpCount; e++)
                {
                    int uIdx = markerExpUnits[e];
                    int mVal = markerExpValues[e];

                    // 이미 처리한 쌍인지 확인
                    bool duplicate = false;
                    for (int prev = 0; prev < e; prev++)
                    {
                        if (markerExpUnits[prev] == uIdx && markerExpValues[prev] == mVal)
                        { duplicate = true; break; }
                    }
                    if (duplicate) continue;

                    int cnt = CountMarkers(state, uIdx, mVal);
                    state.EventQueue?.PushSkillMarkerRemoved(
                        state.Units[uIdx].CombatId, mVal, cnt);
                }
            }
        }

        /// <summary>쉴드 데미지 흡수 (FIFO 순서). 남은 데미지 반환.</summary>
        public static int AbsorbShieldDamage(CombatMatchState state, int unitIndex, int damage)
        {
            if (damage <= 0) return 0;

            bool hadShield = state.Units[unitIndex].ShieldAmount > 0;

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

            // 쉴드가 모두 소진되면 아이콘 제거 이벤트
            if (hadShield && state.Units[unitIndex].ShieldAmount <= 0)
                state.EventQueue?.PushStatusEffectRemoved(state.Units[unitIndex].CombatId, CombatVfxType.Shield);

            return damage; // 남은 데미지
        }

        /// <summary>유닛의 디버프 N개 제거 (먼저 추가된 것부터)</summary>
        public static void RemoveDebuffs(CombatMatchState state, int unitIndex, int count)
        {
            int removed = 0;
            for (int i = 0; i < state.StatusEffectCount && removed < count; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != StatusEffectType.StatDebuff) continue;

                // 역산 후 비활성화
                OnEffectExpired(state, ref effect);
                effect.IsActive = false;
                removed++;
            }
        }

        /// <summary>유닛의 모든 디버프 + CC 제거</summary>
        public static void RemoveAllDebuffs(CombatMatchState state, int unitIndex)
        {
            RemoveEffectsByType(state, unitIndex, StatusEffectType.StatDebuff);
            RemoveEffectsByType(state, unitIndex, StatusEffectType.HealReduction);
            RemoveCC(state, unitIndex);
        }

        /// <summary>유닛에 적용된 HealReduction 최대값 반환 (0이면 없음)</summary>
        public static int GetHealReduction(CombatMatchState state, int unitIndex)
        {
            int max = 0;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != StatusEffectType.HealReduction) continue;
                if (effect.Value > max) max = effect.Value;
            }
            return max;
        }

        /// <summary>유닛의 활성 CC 즉시 해제</summary>
        public static void RemoveCC(CombatMatchState state, int unitIndex)
        {
            if (unitIndex < 0 || unitIndex >= state.UnitCount) return;
            ref var unit = ref state.Units[unitIndex];
            if (unit.ActiveCC == CrowdControlType.None) return;

            // CC VFX 제거 이벤트
            var vfxType = CCToVfxType(unit.ActiveCC);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushCCRemoved(unit.CombatId, vfxType);

            unit.ActiveCC = CrowdControlType.None;
            unit.CCRemainingFrames = 0;
            if (unit.State == CombatState.CrowdControlled)
                unit.State = CombatState.Idle;
        }

        /// <summary>유닛의 특정 타입 StatusEffect 모두 제거 (역산 포함)</summary>
        public static void RemoveEffectsByType(CombatMatchState state, int unitIndex, StatusEffectType type)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != type) continue;
                OnEffectExpired(state, ref effect);
                effect.IsActive = false;
            }
        }

        /// <summary>유닛의 특정 markerId(Value)를 가진 SkillMarker 수 반환</summary>
        public static int CountMarkers(CombatMatchState state, int unitIndex, int markerId)
        {
            int count = 0;
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == StatusEffectType.SkillMarker && effect.Value == markerId)
                    count++;
            }
            return count;
        }

        /// <summary>유닛의 특정 markerId를 가진 가장 오래된(남은 프레임 최소) SkillMarker 제거</summary>
        public static void RemoveOldestMarker(CombatMatchState state, int unitIndex, int markerId)
        {
            int oldestIdx = -1;
            int lowestRemaining = int.MaxValue;

            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type != StatusEffectType.SkillMarker) continue;
                if (effect.Value != markerId) continue;

                if (effect.RemainingFrames < lowestRemaining)
                {
                    lowestRemaining = effect.RemainingFrames;
                    oldestIdx = i;
                }
            }

            if (oldestIdx >= 0)
            {
                state.StatusEffects[oldestIdx].IsActive = false;
            }
        }

        /// <summary>해당 유닛에 특정 면역 상태효과가 활성화되어 있는지 확인</summary>
        public static bool HasImmunity(CombatMatchState state, int unitIndex, StatusEffectType immunityType)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                ref var effect = ref state.StatusEffects[i];
                if (!effect.IsActive) continue;
                if (effect.OwnerUnitIndex != unitIndex) continue;
                if (effect.Type == immunityType) return true;
            }
            return false;
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
                    SkillBuffHelper.ModifyStat(ref unit, effect.StatType, -effect.Value);
                    break;
                case StatusEffectType.StatDebuff:
                    SkillBuffHelper.ModifyStat(ref unit, effect.StatType, effect.Value);
                    break;
            }

            // VFX 제거 이벤트 발행
            var vfxType = ToVfxType(effect.Type, effect.StatType);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushStatusEffectRemoved(unit.CombatId, vfxType);
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
                    SkillDamageHelper.Heal(state, ref unit, effect.Value);
                    break;
            }
        }

        /// <summary>StatusEffectType + StatModType → CombatVfxType 변환</summary>
        public static CombatVfxType ToVfxType(StatusEffectType type, StatModType statType = default)
        {
            switch (type)
            {
                case StatusEffectType.Shield: return CombatVfxType.Shield;
                case StatusEffectType.DamageOverTime: return CombatVfxType.ContinuousDamage;
                case StatusEffectType.HealOverTime: return CombatVfxType.ContinuousHeal;
                case StatusEffectType.CCImmunity: return CombatVfxType.CCImmunity;
                case StatusEffectType.DOTImmunity: return CombatVfxType.DOTImmunity;
                case StatusEffectType.DebuffImmunity: return CombatVfxType.DebuffImmunity;
                case StatusEffectType.StatBuff:
                    switch (statType)
                    {
                        case StatModType.Attack: return CombatVfxType.StatBuff_Attack;
                        case StatModType.Armor: return CombatVfxType.StatBuff_Armor;
                        case StatModType.MagicResist: return CombatVfxType.StatBuff_MagicResist;
                        case StatModType.AttackSpeed: return CombatVfxType.StatBuff_AttackSpeed;
                        default: return CombatVfxType.None;
                    }
                case StatusEffectType.StatDebuff:
                    switch (statType)
                    {
                        case StatModType.Attack: return CombatVfxType.StatDebuff_Attack;
                        case StatModType.Armor: return CombatVfxType.StatDebuff_Armor;
                        case StatModType.MagicResist: return CombatVfxType.StatDebuff_MagicResist;
                        case StatModType.AttackSpeed: return CombatVfxType.StatDebuff_AttackSpeed;
                        default: return CombatVfxType.None;
                    }
                case StatusEffectType.HealReduction: return CombatVfxType.HealAmountDown;
                default: return CombatVfxType.None;
            }
        }

        /// <summary>CrowdControlType → CombatVfxType 변환</summary>
        public static CombatVfxType CCToVfxType(CrowdControlType ccType)
        {
            switch (ccType)
            {
                case CrowdControlType.Stun: return CombatVfxType.CC_Stun;
                case CrowdControlType.Silence: return CombatVfxType.CC_Silence;
                case CrowdControlType.Slow: return CombatVfxType.CC_Slow;
                case CrowdControlType.Freeze: return CombatVfxType.CC_Freeze;
                case CrowdControlType.Taunt: return CombatVfxType.CC_Taunt;
                case CrowdControlType.Airborne: return CombatVfxType.CC_Airborne;
                case CrowdControlType.Knockback: return CombatVfxType.CC_KnockBack;
                default: return CombatVfxType.None;
            }
        }
    }
}
