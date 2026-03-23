namespace CookApps.AutoChess
{
    /// <summary>
    /// 통합 상태효과 시스템. 쉴드/DOT/버프/디버프의 지속시간과 스태킹을 관리.
    /// CombatMatchState.StatusEffects[] 배열을 사용하여 GC 없이 동작.
    /// </summary>
    public static partial class StatusEffectSystem
    {
        // 마커 만료 수집용 pre-allocated 버퍼 (GC-free)
        private static readonly int[] MarkerExpUnits = new int[32];
        private static readonly int[] MarkerExpValues = new int[32];

        /// <summary>상태효과 추가. StatBuff/Debuff는 추가 시 스탯 즉시 적용.</summary>
        public static void AddEffect(CombatMatchState state, int unitIndex,
            StatusEffectType type, int value, int durationFrames,
            int tickInterval = 0, StatModType statType = default, DamageType dmgType = default,
            int sourceSkillId = 0)
        {
            if (unitIndex < 0 || unitIndex >= state.UnitCount) return;

            // 면역 체크: 해당 면역이 활성화되어 있으면 효과 적용 차단
            if (type == StatusEffectType.StatDebuff && HasImmunity(state, unitIndex, StatusEffectType.DebuffImmunity))
                return;
            if (type == StatusEffectType.DamageOverTime && HasImmunity(state, unitIndex, StatusEffectType.DOTImmunity))
                return;

            // sourceSkillId 갱신 로직: 동일 유닛 + 동일 sourceSkillId 기존 효과를 찾아 갱신
            if (sourceSkillId > 0 && (type == StatusEffectType.StatBuff || type == StatusEffectType.StatDebuff))
            {
                for (int i = 0; i < state.StatusEffectCount; i++)
                {
                    ref var existing = ref state.StatusEffects[i];
                    if (!existing.IsActive) continue;
                    if (existing.OwnerUnitIndex != unitIndex) continue;
                    if (existing.SourceSkillId != sourceSkillId) continue;
                    if (existing.Type != type) continue;
                    if (existing.StatType != statType) continue;

                    // 기존 효과의 스탯 역산 → 새 값으로 재적용
                    ref var renewUnit = ref state.Units[unitIndex];
                    if (type == StatusEffectType.StatBuff)
                        SkillBuffHelper.ModifyStat(ref renewUnit, statType, -existing.Value);
                    else
                        SkillBuffHelper.ModifyStat(ref renewUnit, statType, existing.Value);

                    // 새 값/지속시간으로 갱신
                    existing.Value = value;
                    existing.RemainingFrames = durationFrames;

                    // 새 스탯 적용
                    if (type == StatusEffectType.StatBuff)
                        SkillBuffHelper.ModifyStat(ref renewUnit, statType, value);
                    else
                        SkillBuffHelper.ModifyStat(ref renewUnit, statType, -value);

                    return; // 갱신 완료, 새로 추가하지 않음
                }
            }

            int slotIndex = FindWritableEffectSlot(state);
            if (slotIndex < 0) return;

            ref var effect = ref state.StatusEffects[slotIndex];
            effect.OwnerUnitIndex = unitIndex;
            effect.Type = type;
            effect.Value = value;
            effect.RemainingFrames = durationFrames;
            effect.TickInterval = tickInterval;
            effect.TickTimer = tickInterval;
            effect.StatType = statType;
            effect.DmgType = dmgType;
            effect.IsActive = true;
            effect.SourceSkillId = sourceSkillId;

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
                case StatusEffectType.Slow:
                    SkillBuffHelper.ModifyStat(ref unit, StatModType.AttackSpeed, -value);
                    break;
                case StatusEffectType.TargetImpossible:
                    unit.IsUntargetable = true;
                    break;
            }

            // VFX 이벤트 발행
            var vfxType = ToVfxType(type, statType);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushStatusEffectAdded(unit.CombatId, vfxType, durationFrames, statType);

            // SkillMarker 아이콘 이벤트
            if (type == StatusEffectType.SkillMarker)
                state.EventQueue?.PushSkillMarkerAdded(unit.CombatId, value, durationFrames);
        }

        private static int FindWritableEffectSlot(CombatMatchState state)
        {
            for (int i = 0; i < state.StatusEffectCount; i++)
            {
                if (!state.StatusEffects[i].IsActive)
                    return i;
            }

            if (state.StatusEffectCount >= CombatMatchState.MaxStatusEffects)
                return -1;

            return state.StatusEffectCount++;
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
                            if (markerExpCount < 32)
                            {
                                MarkerExpUnits[markerExpCount] = effect.OwnerUnitIndex;
                                MarkerExpValues[markerExpCount] = effect.Value;
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
                    int uIdx = MarkerExpUnits[e];
                    int mVal = MarkerExpValues[e];

                    // 이미 처리한 쌍인지 확인
                    bool duplicate = false;
                    for (int prev = 0; prev < e; prev++)
                    {
                        if (MarkerExpUnits[prev] == uIdx && MarkerExpValues[prev] == mVal)
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
            RemoveEffectsByType(state, unitIndex, StatusEffectType.Silence);
            RemoveEffectsByType(state, unitIndex, StatusEffectType.Slow);
            RemoveEffectsByType(state, unitIndex, StatusEffectType.Taunt);
            RemoveEffectsByType(state, unitIndex, StatusEffectType.TargetImpossible);
            RemoveCC(state, unitIndex);
        }
    }
}
