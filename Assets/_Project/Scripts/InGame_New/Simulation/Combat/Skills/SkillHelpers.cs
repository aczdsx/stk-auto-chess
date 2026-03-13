namespace CookApps.AutoChess
{
    /// <summary>데미지 계산 + 적용 헬퍼</summary>
    public static class SkillDamageHelper
    {
        /// <summary>단일 대상 데미지 (배율 기반)</summary>
        public static void DealDamage(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, int powerPercent, DamageType type)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];
            if (!target.IsAlive) return;

            int raw = caster.Attack * powerPercent / 100;
            int dmg = DamageSystem.CalculateDamage(raw, type, ref caster, ref target);
            DamageSystem.ApplyDamage(state, ref target, dmg);
            DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
        }

        /// <summary>고정 데미지</summary>
        public static void DealFlatDamage(CombatMatchState state, int targetCombatId,
            int flatDamage, DamageType type)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];
            if (!target.IsAlive) return;

            int dmg = DamageSystem.CalculateDamage(flatDamage, type, ref target);
            DamageSystem.ApplyDamage(state, ref target, dmg);
            DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
        }

        /// <summary>HP 회복 (HealReduction 상태효과 반영)</summary>
        public static void Heal(CombatMatchState state, ref CombatUnit target, int amount)
        {
            if (!target.IsAlive) return;

            // HealReduction 상태효과 적용
            int targetIdx = state.FindUnitIndex(target.CombatId);
            if (targetIdx >= 0)
            {
                int reduction = StatusEffectSystem.GetHealReduction(state, targetIdx);
                if (reduction > 0)
                    amount = amount * (100 - reduction) / 100;
            }
            if (amount <= 0) return;

            target.CurrentHP += amount;
            if (target.CurrentHP > target.MaxHP)
                target.CurrentHP = target.MaxHP;

            if (CombatLogger.Enabled) CombatLogger.LogHeal(target.CombatId, amount, target.CurrentHP, target.MaxHP);

            state.EventQueue?.PushUnitHealed(target.CombatId, amount);
        }
    }

    /// <summary>범위 타겟 검색 + 순회 헬퍼</summary>
    public static class SkillAreaHelper
    {
        public delegate void AreaAction(ref CombatUnit target, int targetIndex);

        /// <summary>원형 범위(체비셰프 거리) 내 적 순회</summary>
        public static void ForEachEnemyInRadius(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == casterTeam) continue;

                int dist = BoardHelper.MinChebyshevDistance(centerCol, centerRow, 1, 1,
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1);
                if (dist <= radius)
                {
                    action(ref unit, i);
                }
            }
        }

        /// <summary>다이아몬드 범위(맨해튼 거리) 내 적 순회</summary>
        public static void ForEachEnemyInDiamond(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == casterTeam) continue;

                int dist = BoardHelper.MinManhattanDistance(centerCol, centerRow, 1, 1,
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1);
                if (dist <= radius)
                {
                    action(ref unit, i);
                }
            }
        }

        /// <summary>원형 범위 내 아군 순회</summary>
        public static void ForEachAllyInRadius(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex != casterTeam) continue;

                int dist = BoardHelper.MinChebyshevDistance(centerCol, centerRow, 1, 1,
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1);
                if (dist <= radius)
                {
                    action(ref unit, i);
                }
            }
        }

        /// <summary>직선 범위 내 적 순회</summary>
        public static void ForEachEnemyInLine(CombatMatchState state, byte casterTeam,
            int startCol, int startRow, int dirCol, int dirRow, int length, AreaAction action)
        {
            int col = startCol;
            int row = startRow;

            for (int step = 0; step < length; step++)
            {
                col += dirCol;
                row += dirRow;

                if (!BoardHelper.IsValidCombatPosition(col, row)) break;

                int combatId = state.GetUnitAtGrid(col, row);
                if (combatId == CombatUnit.InvalidId) continue;

                int idx = state.FindUnitIndex(combatId);
                if (idx < 0) continue;

                ref var unit = ref state.Units[idx];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == casterTeam) continue;

                action(ref unit, idx);
            }
        }

        /// <summary>최적 AoE 타겟 찾기 (가장 많은 적을 포함하는 적 유닛의 CombatId)</summary>
        public static int FindBestAoETarget(CombatMatchState state, ref CombatUnit caster, int radius)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestCount = 0;
            int bestDist = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsAlive) continue;
                if (candidate.TeamIndex == caster.TeamIndex) continue;

                int count = CountEnemiesInRadius(state, caster.TeamIndex,
                    candidate.GridCol, candidate.GridRow, radius);

                int dist = BoardHelper.MinManhattanDistance(
                    caster.GridCol, caster.GridRow,
                    caster.SizeW > 0 ? caster.SizeW : (byte)1,
                    caster.SizeH > 0 ? caster.SizeH : (byte)1,
                    candidate.GridCol, candidate.GridRow,
                    candidate.SizeW > 0 ? candidate.SizeW : (byte)1,
                    candidate.SizeH > 0 ? candidate.SizeH : (byte)1);

                if (count > bestCount || (count == bestCount && dist < bestDist))
                {
                    bestTarget = candidate.CombatId;
                    bestCount = count;
                    bestDist = dist;
                }
            }

            return bestTarget;
        }

        /// <summary>최저 HP 아군 찾기 (CombatId 반환)</summary>
        public static int FindLowestHPAlly(CombatMatchState state, byte teamIndex)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestHP = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex != teamIndex) continue;

                if (unit.CurrentHP < bestHP)
                {
                    bestTarget = unit.CombatId;
                    bestHP = unit.CurrentHP;
                }
            }

            return bestTarget;
        }

        /// <summary>HP가 가장 낮은 아군 N명의 CombatId 배열 반환. 실제 찾은 수 반환.</summary>
        public static int FindLowestHPAllies(CombatMatchState state, byte teamIndex, int count, int[] resultBuffer)
        {
            int found = 0;
            for (int c = 0; c < count; c++)
            {
                int bestIdx = -1;
                int bestHP = int.MaxValue;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.TeamIndex != teamIndex || !u.IsAlive) continue;

                    bool alreadySelected = false;
                    for (int j = 0; j < found; j++)
                    {
                        if (resultBuffer[j] == u.CombatId) { alreadySelected = true; break; }
                    }
                    if (alreadySelected) continue;

                    if (u.CurrentHP < bestHP)
                    {
                        bestHP = u.CurrentHP;
                        bestIdx = i;
                    }
                }
                if (bestIdx < 0) break;
                resultBuffer[found++] = state.Units[bestIdx].CombatId;
            }
            return found;
        }

        /// <summary>범위 내 적 수 카운트</summary>
        public static int CountEnemiesInRadius(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius)
        {
            int count = 0;
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == casterTeam) continue;

                int dist = BoardHelper.MinChebyshevDistance(centerCol, centerRow, 1, 1,
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1);
                if (dist <= radius)
                    count++;
            }
            return count;
        }
    }

    /// <summary>CC 적용 헬퍼</summary>
    public static class SkillCCHelper
    {
        /// <summary>행동 불능(Immobilizing) CC인지 판별. Stun/Freeze/Airborne만 해당.</summary>
        public static bool IsImmobilizing(CrowdControlType type)
            => type == CrowdControlType.Stun
            || type == CrowdControlType.Freeze
            || type == CrowdControlType.Airborne;

        /// <summary>CC 적용. 카테고리별 자동 분기:
        /// Immobilizing(Stun/Freeze/Airborne) → CrowdControlled 상태,
        /// NonImmobilizing(Silence/Slow/Taunt) → StatusEffect 디버프,
        /// Knockback → 별도 Knockback() 메서드 사용.</summary>
        public static void ApplyCC(CombatMatchState state, ref CombatUnit target,
            CrowdControlType type, int durationFrames, int value = 0)
        {
            if (!target.IsAlive) return;
            if (target.State == CombatState.Dead) return;

            // CC 면역 체크
            int idx = state.FindUnitIndex(target.CombatId);
            if (idx >= 0 && StatusEffectSystem.HasImmunity(state, idx, StatusEffectType.CCImmunity))
                return;

            // ── 비행동불능 CC → StatusEffect 디버프 ──
            if (!IsImmobilizing(type))
            {
                if (idx < 0) return;

                switch (type)
                {
                    case CrowdControlType.Silence:
                        StatusEffectSystem.RemoveEffectsByType(state, idx, StatusEffectType.Silence);
                        StatusEffectSystem.AddEffect(state, idx, StatusEffectType.Silence, 0, durationFrames);
                        break;
                    case CrowdControlType.Slow:
                        StatusEffectSystem.RemoveEffectsByType(state, idx, StatusEffectType.Slow);
                        StatusEffectSystem.AddEffect(state, idx, StatusEffectType.Slow, value, durationFrames);
                        break;
                    case CrowdControlType.Taunt:
                        StatusEffectSystem.RemoveEffectsByType(state, idx, StatusEffectType.Taunt);
                        StatusEffectSystem.AddEffect(state, idx, StatusEffectType.Taunt, value, durationFrames);
                        break;
                }

                if (CombatLogger.Enabled) CombatLogger.LogCC(target.CombatId, type, durationFrames);
                return;
            }

            // ── 행동불능 CC → ActiveCC + CrowdControlled 상태 ──
            // 기존 CC보다 긴 경우에만 적용
            if (target.ActiveCC != CrowdControlType.None && target.CCRemainingFrames >= durationFrames)
                return;

            // 기존 CC가 있으면 VFX 제거 이벤트
            if (target.ActiveCC != CrowdControlType.None)
            {
                var oldVfx = StatusEffectSystem.CCToVfxType(target.ActiveCC);
                if (oldVfx != CombatVfxType.None)
                    state.EventQueue?.PushCCRemoved(target.CombatId, oldVfx);
            }

            target.ActiveCC = type;
            target.CCRemainingFrames = durationFrames;
            target.State = CombatState.CrowdControlled;

            if (CombatLogger.Enabled) CombatLogger.LogCC(target.CombatId, type, durationFrames);

            // CC VFX 이벤트 발행
            var vfxType = StatusEffectSystem.CCToVfxType(type);
            if (vfxType != CombatVfxType.None)
                state.EventQueue?.PushCCAdded(target.CombatId, vfxType, durationFrames);
        }

        /// <summary>
        /// 넉백 (지정 방향으로 N칸, 빈 칸까지만). Multi-tile 대응.
        /// 이동 거리가 distance보다 짧으면(벽/유닛 충돌) 자동으로 1초 스턴 적용.
        /// 실제 이동 칸 수 반환.
        /// </summary>
        /// <param name="worldTickRate">GameWorld.TickRate — 모드별 상이하므로 반드시 외부에서 전달</param>
        public static int Knockback(CombatMatchState state, ref CombatUnit target,
            int dirCol, int dirRow, int distance, int worldTickRate)
        {
            if (!target.IsAlive) return 0;

            // CC 면역 체크
            int immuneIdx = state.FindUnitIndex(target.CombatId);
            if (immuneIdx >= 0 && StatusEffectSystem.HasImmunity(state, immuneIdx, StatusEffectType.CCImmunity))
                return 0;

            byte sizeW = target.SizeW > 0 ? target.SizeW : (byte)1;
            byte sizeH = target.SizeH > 0 ? target.SizeH : (byte)1;

            int col = target.GridCol;
            int row = target.GridRow;
            int actualMoved = 0;

            for (int step = 0; step < distance; step++)
            {
                int nextCol = col + dirCol;
                int nextRow = row + dirRow;

                if (!BoardHelper.IsValidCombatFootprint(nextCol, nextRow, sizeW, sizeH)) break;
                if (!state.IsFootprintClear(nextCol, nextRow, sizeW, sizeH, target.CombatId)) break;

                col = nextCol;
                row = nextRow;
                actualMoved++;
            }

            // 실제 이동 처리 (View Lerp 보간용 파라미터 설정)
            if (col != target.GridCol || row != target.GridRow)
            {
                target.MoveFromCol = target.GridCol;
                target.MoveFromRow = target.GridRow;

                state.ClearGridMulti(target.GridCol, target.GridRow, sizeW, sizeH);
                target.GridCol = (byte)col;
                target.GridRow = (byte)row;
                state.SetGridMulti(col, row, sizeW, sizeH, target.CombatId);

                // 넉백 이동 시간: 거리 비례 (0.3초 기본 + 거리당 0.1초)
                int knockbackFrames = (int)((0.3f + actualMoved * 0.1f) * worldTickRate + 0.5f);
                target.MoveDuration = knockbackFrames;
                target.MoveTimer = knockbackFrames;
                target.IsKnockbackMoving = true;

                state.EventQueue?.PushUnitMoved(target.SourceEntityId, (byte)col, (byte)row);
            }

            // 충돌 시 스턴 (1초 고정 — worldTickRate 프레임)
            if (actualMoved < distance)
            {
                ApplyCC(state, ref target, CrowdControlType.Stun, worldTickRate);
            }

            return actualMoved;
        }
    }

    /// <summary>버프/디버프 헬퍼</summary>
    public static class SkillBuffHelper
    {
        /// <summary>스탯 증감 (고정값 가산, 즉시 영구 적용)</summary>
        public static void ModifyStat(ref CombatUnit target, StatModType stat, int value)
        {
            if (!target.IsAlive) return;

            switch (stat)
            {
                case StatModType.Attack:
                    target.Attack += value;
                    if (target.Attack < 0) target.Attack = 0;
                    break;
                case StatModType.Armor:
                    target.Armor += value;
                    if (target.Armor < 0) target.Armor = 0;
                    break;
                case StatModType.MagicResist:
                    target.MagicResist += value;
                    if (target.MagicResist < 0) target.MagicResist = 0;
                    break;
                case StatModType.AttackSpeed:
                    target.AttackSpeed += value;
                    if (target.AttackSpeed < 1) target.AttackSpeed = 1;
                    break;
                case StatModType.ManaRegenRate:
                    target.ManaRegenRateBonus += value;
                    break;
                case StatModType.MaxMana:
                    target.MaxMana += value;
                    if (target.MaxMana < 1) target.MaxMana = 1;
                    if (target.CurrentMana > target.MaxMana)
                        target.CurrentMana = target.MaxMana;
                    break;
            }
        }

        /// <summary>지속시간 있는 스탯 버프 (만료 시 자동 역산)</summary>
        public static void ApplyTimedBuff(CombatMatchState state, int unitIndex,
            StatModType stat, int value, int durationFrames)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.StatBuff,
                value, durationFrames, statType: stat);
        }

        /// <summary>지속시간 있는 스탯 디버프 (만료 시 자동 역산)</summary>
        public static void ApplyTimedDebuff(CombatMatchState state, int unitIndex,
            StatModType stat, int value, int durationFrames)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.StatDebuff,
                value, durationFrames, statType: stat);
        }

        /// <summary>쉴드 부여 (지속시간 기반, 스태킹)</summary>
        public static void AddShield(CombatMatchState state, int unitIndex,
            int amount, int durationFrames)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.Shield,
                amount, durationFrames);
        }

        /// <summary>DOT(지속 데미지) 부여</summary>
        public static void ApplyDOT(CombatMatchState state, int unitIndex,
            int damagePerTick, int durationFrames, int tickInterval, DamageType dmgType)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.DamageOverTime,
                damagePerTick, durationFrames, tickInterval, dmgType: dmgType);
        }

        /// <summary>HOT(지속 회복) 부여</summary>
        public static void ApplyHOT(CombatMatchState state, int unitIndex,
            int healPerTick, int durationFrames, int tickInterval)
        {
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.HealOverTime,
                healPerTick, durationFrames, tickInterval);
        }

        /// <summary>CC 면역 부여 (기존 CC 즉시 해제)</summary>
        public static void ApplyCCImmunity(CombatMatchState state, int unitIndex, int durationFrames)
        {
            StatusEffectSystem.RemoveCC(state, unitIndex);
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.CCImmunity, 0, durationFrames);
        }

        /// <summary>DOT 면역 부여 (기존 DOT 즉시 제거)</summary>
        public static void ApplyDOTImmunity(CombatMatchState state, int unitIndex, int durationFrames)
        {
            StatusEffectSystem.RemoveEffectsByType(state, unitIndex, StatusEffectType.DamageOverTime);
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.DOTImmunity, 0, durationFrames);
        }

        /// <summary>디버프(스탯감소) 면역 부여 (기존 StatDebuff 즉시 제거, 스탯 역산 포함)</summary>
        public static void ApplyDebuffImmunity(CombatMatchState state, int unitIndex, int durationFrames)
        {
            StatusEffectSystem.RemoveEffectsByType(state, unitIndex, StatusEffectType.StatDebuff);
            StatusEffectSystem.AddEffect(state, unitIndex, StatusEffectType.DebuffImmunity, 0, durationFrames);
        }
    }
}
