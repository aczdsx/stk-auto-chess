namespace CookApps.AutoChess
{
    /// <summary>
    /// Recipe 액션 실행기. SkillAction의 Effect 타입에 따라 기존 Helper를 호출.
    /// GC-free: lambda/delegate 없이 직접 for-loop로 순회.
    /// Quantum 호환: switch 기반 디스패치, struct context 전달.
    /// </summary>
    public static class ActionExecutor
    {
        // LowestHpAllies용 pre-allocated 버퍼 (최대 8명)
        private static readonly int[] LowestHpBuffer = new int[8];

        /// <summary>단일 액션 실행</summary>
        public static void Execute(ref SkillAction action, SkillExecuteContext ctx)
        {
            // VFX 스폰
            if (action.VfxIndex >= 0 && action.VfxAt != SkillVfxPlacement.None)
                SpawnVfx(ref action, ctx);

            // 효과 실행
            if (action.Effect == SkillEffectType.None)
                return;

            switch (action.Effect)
            {
                case SkillEffectType.Damage:       ExecuteDamage(ref action, ctx); break;
                case SkillEffectType.Heal:          ExecuteHeal(ref action, ctx); break;
                case SkillEffectType.ApplyCC:       ExecuteCC(ref action, ctx); break;
                case SkillEffectType.Knockback:     ExecuteKnockback(ref action, ctx); break;
                case SkillEffectType.ApplyBuff:     ExecuteBuff(ref action, ctx); break;
                case SkillEffectType.ApplyDebuff:   ExecuteDebuff(ref action, ctx); break;
                case SkillEffectType.Shield:        ExecuteShield(ref action, ctx); break;
                case SkillEffectType.RemoveDebuffs: ExecuteRemoveDebuffs(ref action, ctx); break;
                case SkillEffectType.AddMarker:     ExecuteAddMarker(ref action, ctx); break;
                case SkillEffectType.SpawnProjectile: ExecuteSpawnProjectile(ref action, ctx); break;
                case SkillEffectType.MultiHit:      ExecuteMultiHit(ref action, ctx); break;
                case SkillEffectType.ModifyStat:    ExecuteModifyStat(ref action, ctx); break;
                case SkillEffectType.SpawnLinearProjectile: ExecuteSpawnLinearProjectile(ref action, ctx); break;
                case SkillEffectType.DamageKnockbackInArea: ExecuteDamageKnockbackInArea(ref action, ctx); break;
                case SkillEffectType.SequentialLineDamage: ExecuteSequentialLineDamage(ref action, ctx); break;
            }
        }

        // ══════════════════════════════
        // 개별 이펙트 실행 (모두 GC-free: 직접 for-loop, lambda 없음)
        // ══════════════════════════════

        private static void ExecuteDamage(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);

            if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                SkillDamageHelper.DealDamage(ctx.State, ref ctx.GetCaster(), ctx.TargetCombatId, power, ctx.DamageType);
            }
            else if (action.TargetFilter == SkillTargetFilter.EnemiesInArea)
            {
                int casterIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
                if (casterIdx < 0) return;

                var state = ctx.State;
                var type = ctx.DamageType;
                int attack = state.Units[casterIdx].Attack;

                // Line(Cone): 시전자 전방 직선 순회
                if (action.AreaShape == SkillAreaShape.Line)
                {
                    ref var caster = ref state.Units[casterIdx];
                    int dirCol = caster.TeamIndex == 0 ? 1 : -1;
                    int range = action.AreaRange;

                    int col = caster.GridCol;
                    int row = caster.GridRow;
                    for (int step = 0; step < range; step++)
                    {
                        col += dirCol;
                        if (!BoardHelper.IsValidCombatPosition(col, row)) break;

                        int combatId = state.GetUnitAtGrid(col, row);
                        if (combatId == CombatUnit.InvalidId) continue;

                        int ti = state.FindUnitIndex(combatId);
                        if (ti < 0) continue;
                        ref var t = ref state.Units[ti];
                        if (!t.IsAlive || t.TeamIndex == ctx.CasterTeam) continue;

                        int raw = attack * power / 100;
                        int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref t);
                        DamageSystem.ApplyDamage(state, ref t, dmg);
                        DamageSystem.ChargeMana(ref t, t.ManaGainOnHit);
                    }
                }
                else
                {
                    // Circle, Diamond, Plus: 범위 내 적 순회
                    int centerCol, centerRow;
                    GetAreaCenter(ctx, out centerCol, out centerRow);

                    for (int i = 0; i < state.UnitCount; i++)
                    {
                        ref var unit = ref state.Units[i];
                        if (!unit.IsAlive || unit.TeamIndex == ctx.CasterTeam) continue;
                        if (!SkillAreaHelper.IsInArea(action.AreaShape, centerCol, centerRow, action.AreaRange, ref unit))
                            continue;

                        int raw = attack * power / 100;
                        int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref unit);
                        DamageSystem.ApplyDamage(state, ref unit, dmg, casterIdx, type);
                        DamageSystem.ChargeMana(ref unit, unit.ManaGainOnHit);
                    }
                }
            }
        }

        private static void ExecuteHeal(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int healAmount = ctx.GetCaster().Attack * power / 100;

            if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    SkillDamageHelper.Heal(ctx.State, ref ctx.State.Units[idx], healAmount);
            }
            else if (action.TargetFilter == SkillTargetFilter.AlliesInArea)
            {
                int centerCol, centerRow;
                GetAreaCenter(ctx, out centerCol, out centerRow);

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex != ctx.CasterTeam) continue;
                    if (!SkillAreaHelper.IsInArea(action.AreaShape, centerCol, centerRow, action.AreaRange, ref unit))
                        continue;

                    SkillDamageHelper.Heal(ctx.State, ref unit, healAmount);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.LowestHpAllies)
            {
                int count = action.AreaRange > 0 ? action.AreaRange : 1;
                if (count > LowestHpBuffer.Length) count = LowestHpBuffer.Length;
                int found = SkillAreaHelper.FindLowestHPAllies(ctx.State, ctx.CasterTeam, count, LowestHpBuffer);
                for (int i = 0; i < found; i++)
                {
                    int idx = ctx.State.FindUnitIndex(LowestHpBuffer[i]);
                    if (idx >= 0)
                        SkillDamageHelper.Heal(ctx.State, ref ctx.State.Units[idx], healAmount);
                }
            }
        }

        private static void ExecuteCC(ref SkillAction action, SkillExecuteContext ctx)
        {
            int durationFrames = ctx.GetParamValue(action.SecondaryParamIndex);
            if (durationFrames <= 0) durationFrames = 60;

            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx < 0) return;
            SkillCCHelper.ApplyCC(ctx.State, ref ctx.State.Units[idx], action.CCType, durationFrames);
        }

        private static void ExecuteKnockback(ref SkillAction action, SkillExecuteContext ctx)
        {
            int distance = ctx.GetParamValue(action.SecondaryParamIndex);
            if (distance <= 0) distance = 2;

            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx < 0) return;
            ref var target = ref ctx.State.Units[idx];

            ref var caster = ref ctx.GetCaster();
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (dirCol == 0 && dirRow == 0)
                dirCol = caster.TeamIndex == 0 ? 1 : -1;
            else
            {
                dirCol = dirCol > 0 ? 1 : (dirCol < 0 ? -1 : 0);
                dirRow = dirRow > 0 ? 1 : (dirRow < 0 ? -1 : 0);
            }

            SkillCCHelper.Knockback(ctx.State, ref target, dirCol, dirRow, distance, ctx.WorldTickRate);
        }

        private static void ExecuteBuff(ref SkillAction action, SkillExecuteContext ctx)
        {
            int value = ctx.GetParamValue(action.ParamIndex);
            int duration = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;

            if (action.TargetFilter == SkillTargetFilter.Self)
            {
                int casterIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
                if (casterIdx >= 0)
                    SkillBuffHelper.ApplyPercentBuff(ctx.State, casterIdx, action.BuffStat, value, duration);
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    SkillBuffHelper.ApplyPercentBuff(ctx.State, idx, action.BuffStat, value, duration);
            }
        }

        private static void ExecuteDebuff(ref SkillAction action, SkillExecuteContext ctx)
        {
            int value = ctx.GetParamValue(action.ParamIndex);
            int duration = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;

            bool isStatusEffect = action.StatusEffect != (StatusEffectType)0;

            if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                {
                    if (isStatusEffect)
                        StatusEffectSystem.AddEffect(ctx.State, idx, action.StatusEffect, value, duration);
                    else
                        SkillBuffHelper.ApplyPercentDebuff(ctx.State, idx, action.BuffStat, value, duration);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.EnemiesInArea)
            {
                int centerCol, centerRow;
                GetAreaCenter(ctx, out centerCol, out centerRow);
                var statusEffect = action.StatusEffect;
                var buffStat = action.BuffStat;

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex == ctx.CasterTeam) continue;
                    if (!SkillAreaHelper.IsInArea(action.AreaShape, centerCol, centerRow, action.AreaRange, ref unit))
                        continue;

                    if (isStatusEffect)
                        StatusEffectSystem.AddEffect(ctx.State, i, statusEffect, value, duration);
                    else
                        SkillBuffHelper.ApplyPercentDebuff(ctx.State, i, buffStat, value, duration);
                }
            }
        }

        private static void ExecuteShield(ref SkillAction action, SkillExecuteContext ctx)
        {
            int shieldPercent = ctx.GetParamValue(action.ParamIndex);
            int duration = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 0;

            if (action.TargetFilter == SkillTargetFilter.SameRowAllies)
            {
                ref var caster = ref ctx.GetCaster();
                int row = caster.GridRow;
                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex != ctx.CasterTeam) continue;
                    if (unit.GridRow != row) continue;

                    int shieldAmount = unit.MaxHP * shieldPercent / 100;
                    SkillBuffHelper.AddShield(ctx.State, i, shieldAmount, duration);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                {
                    ref var target = ref ctx.State.Units[idx];
                    int shieldAmount = target.MaxHP * shieldPercent / 100;
                    SkillBuffHelper.AddShield(ctx.State, idx, shieldAmount, duration);
                }
            }
        }

        private static void ExecuteRemoveDebuffs(ref SkillAction action, SkillExecuteContext ctx)
        {
            if (action.TargetFilter == SkillTargetFilter.AlliesInArea)
            {
                int centerCol, centerRow;
                GetAreaCenter(ctx, out centerCol, out centerRow);

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var unit = ref ctx.State.Units[i];
                    if (!unit.IsAlive || unit.TeamIndex != ctx.CasterTeam) continue;
                    if (!SkillAreaHelper.IsInArea(action.AreaShape, centerCol, centerRow, action.AreaRange, ref unit))
                        continue;

                    StatusEffectSystem.RemoveAllDebuffs(ctx.State, i);
                }
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    StatusEffectSystem.RemoveAllDebuffs(ctx.State, idx);
            }
        }

        private static void ExecuteAddMarker(ref SkillAction action, SkillExecuteContext ctx)
        {
            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx >= 0)
            {
                StatusEffectSystem.AddEffect(ctx.State, idx,
                    StatusEffectType.SkillMarker, action.MarkerType, 1);
            }
        }

        private static void ExecuteSpawnProjectile(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int raw = ctx.GetCaster().Attack * power / 100;

            ProjectileSystem.CreateHomingProjectile(
                ctx.State, ctx.CasterCombatId, ctx.TargetCombatId,
                raw, false, ctx.DamageType,
                action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30,
                ctx.SkillSpecId, action.VfxIndex, false,
                action.SecondaryParamIndex >= 0 ? (sbyte)ctx.GetParamValue(action.SecondaryParamIndex) : (sbyte)-1);
        }

        private static void ExecuteMultiHit(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int hitCount = action.RepeatCount > 0 ? action.RepeatCount : 3;

            for (int i = 0; i < hitCount; i++)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx < 0 || !ctx.State.Units[idx].IsAlive) break;
                SkillDamageHelper.DealDamage(ctx.State, ref ctx.GetCaster(), ctx.TargetCombatId, power, ctx.DamageType);
            }
        }

        private static void ExecuteModifyStat(ref SkillAction action, SkillExecuteContext ctx)
        {
            int value = ctx.GetParamValue(action.ParamIndex);
            var stat = action.BuffStat;

            if (action.TargetFilter == SkillTargetFilter.Self)
            {
                int idx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
                if (idx >= 0)
                    SkillBuffHelper.ModifyStat(ref ctx.State.Units[idx], stat, value);
            }
            else if (action.TargetFilter == SkillTargetFilter.PrimaryTarget)
            {
                int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (idx >= 0)
                    SkillBuffHelper.ModifyStat(ref ctx.State.Units[idx], stat,
                        action.Effect == SkillEffectType.ModifyStat ? value : -value);
            }
        }

        private static void ExecuteSpawnLinearProjectile(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int raw = ctx.GetCaster().Attack * power / 100;

            ref var caster = ref ctx.GetCaster();
            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);

            int dirCol, dirRow;
            if (idx >= 0)
            {
                ref var target = ref ctx.State.Units[idx];
                int dc = target.GridCol - caster.GridCol;
                int dr = target.GridRow - caster.GridRow;
                dirCol = dc > 0 ? 1 : (dc < 0 ? -1 : 0);
                dirRow = dr > 0 ? 1 : (dr < 0 ? -1 : 0);
            }
            else
            {
                dirCol = 0;
                dirRow = 0;
            }

            if (dirCol == 0 && dirRow == 0)
                dirRow = caster.TeamIndex == 0 ? 1 : -1;

            // AreaRange=length, RepeatIntervalFrames=moveInterval, RepeatCount=width
            int length = action.AreaRange > 0 ? action.AreaRange : 4;
            int moveInterval = action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 3;
            int width = action.RepeatCount > 0 ? action.RepeatCount : 1;

            ProjectileSystem.CreateLinearProjectile(
                ctx.State, ctx.CasterCombatId,
                caster.GridCol, caster.GridRow,
                (sbyte)dirCol, (sbyte)dirRow,
                raw, false, ctx.DamageType,
                moveInterval, length, width, ctx.SkillSpecId);
        }

        /// <summary>
        /// 메이: Plus 범위 데미지 + 각 적에게 중심→바깥 방향 넉백 + 자기 버프.
        /// ParamIndex = 데미지 배율, SecondaryParamIndex = 넉백 거리 (없으면 1).
        /// SkillAction.BuffStat/ParamIndex로 자기 버프도 처리.
        /// </summary>
        private static void ExecuteDamageKnockbackInArea(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int knockDist = action.SecondaryParamIndex >= 0
                ? ctx.GetParamValue(action.SecondaryParamIndex) : 1;

            int casterIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
            if (casterIdx < 0) return;

            var state = ctx.State;
            ref var caster = ref state.Units[casterIdx];
            int casterCol = caster.GridCol;
            int casterRow = caster.GridRow;
            int attack = caster.Attack;
            var type = ctx.DamageType;
            byte team = ctx.CasterTeam;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive || unit.TeamIndex == team) continue;
                if (!SkillAreaHelper.IsInArea(action.AreaShape, casterCol, casterRow, action.AreaRange, ref unit))
                    continue;

                // 데미지
                int raw = attack * power / 100;
                int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref unit);
                DamageSystem.ApplyDamage(state, ref unit, dmg);
                DamageSystem.ChargeMana(ref unit, unit.ManaGainOnHit);

                if (!unit.IsAlive) continue;

                // 넉백: 중심 → 바깥 방향
                int dirCol = unit.GridCol - casterCol;
                int dirRow = unit.GridRow - casterRow;
                dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
                dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
                if (dirCol == 0 && dirRow == 0) dirCol = team == 0 ? 1 : -1;

                SkillCCHelper.Knockback(state, ref unit, dirCol, dirRow, knockDist, ctx.WorldTickRate);
            }
        }

        /// <summary>
        /// 보스탱커: 전방 직선 순차 타일 타격.
        /// OnTick에서 매 틱마다 호출됨. SimSkillGeneric._tickCount가 currentStep.
        /// AreaRange = lineLength, ParamIndex = 데미지 배율.
        /// 각 타일: AreaEffect VFX + VFX[0] + 카메라쉐이크 + 데미지 + 넉백.
        /// </summary>
        private static void ExecuteSequentialLineDamage(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int casterIdx = ctx.State.FindUnitIndex(ctx.CasterCombatId);
            if (casterIdx < 0) return;

            var state = ctx.State;
            ref var caster = ref state.Units[casterIdx];

            // 방향: 시전자 팀 기반 (row 방향)
            int targetIdx = ctx.TargetCombatId != CombatUnit.InvalidId
                ? state.FindUnitIndex(ctx.TargetCombatId) : -1;
            int dirCol, dirRow;
            if (targetIdx >= 0)
            {
                int dc = state.Units[targetIdx].GridCol - caster.GridCol;
                int dr = state.Units[targetIdx].GridRow - caster.GridRow;
                if (System.Math.Abs(dr) >= System.Math.Abs(dc))
                { dirRow = dr >= 0 ? 1 : -1; dirCol = 0; }
                else
                { dirCol = dc >= 0 ? 1 : -1; dirRow = 0; }
            }
            else
            {
                dirRow = caster.TeamIndex == 0 ? 1 : -1;
                dirCol = 0;
            }

            // 현재 타일 좌표 (tickCount = step, 1-based)
            int dist = ctx.TickCount;
            int tCol = caster.GridCol + dirCol * dist;
            int tRow = caster.GridRow + dirRow * dist;

            if (!BoardHelper.IsValidCombatPosition(tCol, tRow)) return;

            // 타일 VFX
            state.EventQueue?.PushSkillAreaEffect(caster.CombatId, (byte)tCol, (byte)tRow, 0);

            // 스킬 VFX[0]
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, ctx.SkillSpecId, 0,
                dirCol: (sbyte)dirCol, dirRow: (sbyte)dirRow,
                col: (byte)tCol, row: (byte)tRow, useGridPos: true);

            // 카메라 쉐이크
            state.EventQueue?.PushCameraShake(400, 15);

            // 타일에 적이 있으면 데미지 + 넉백
            int combatId = state.GetUnitAtGrid(tCol, tRow);
            if (combatId == CombatUnit.InvalidId) return;

            int ti = state.FindUnitIndex(combatId);
            if (ti < 0) return;
            ref var target = ref state.Units[ti];
            if (!target.IsAlive || target.TeamIndex == caster.TeamIndex) return;

            SkillDamageHelper.DealDamage(state, ref caster, combatId, power, ctx.DamageType);

            ti = state.FindUnitIndex(combatId);
            if (ti < 0 || !state.Units[ti].IsAlive) return;
            target = ref state.Units[ti];
            SkillCCHelper.Knockback(state, ref target, dirCol, dirRow, 1, ctx.WorldTickRate);
        }

        // ══════════════════════════════
        // VFX
        // ══════════════════════════════

        private static void SpawnVfx(ref SkillAction action, SkillExecuteContext ctx)
        {
            var eq = ctx.State.EventQueue;
            if (eq == null) return;

            switch (action.VfxAt)
            {
                case SkillVfxPlacement.AtCaster:
                    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex);
                    break;
                case SkillVfxPlacement.AtTarget:
                    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                        targetId: ctx.TargetCombatId);
                    break;
                case SkillVfxPlacement.AtGridPos:
                {
                    int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    if (idx >= 0)
                    {
                        ref var t = ref ctx.State.Units[idx];
                        eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                            col: (byte)t.GridCol, row: (byte)t.GridRow, useGridPos: true);
                    }
                    break;
                }
                case SkillVfxPlacement.AtCasterWithDir:
                {
                    ref var caster = ref ctx.GetCaster();
                    int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    sbyte dirCol = 0, dirRow = 0;
                    if (idx >= 0)
                    {
                        ref var t = ref ctx.State.Units[idx];
                        int dc = t.GridCol - caster.GridCol;
                        int dr = t.GridRow - caster.GridRow;
                        dirCol = (sbyte)(dc > 0 ? 1 : (dc < 0 ? -1 : 0));
                        dirRow = (sbyte)(dr > 0 ? 1 : (dr < 0 ? -1 : 0));
                    }
                    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                        dirCol: dirCol, dirRow: dirRow);
                    break;
                }
                case SkillVfxPlacement.AreaEffect:
                {
                    int centerCol, centerRow;
                    GetAreaCenter(ctx, out centerCol, out centerRow);
                    eq.PushSkillAreaEffect(ctx.CasterCombatId,
                        (byte)centerCol, (byte)centerRow, action.AreaRange);
                    break;
                }
                case SkillVfxPlacement.PerTileInDiamond:
                {
                    int centerCol, centerRow;
                    GetAreaCenter(ctx, out centerCol, out centerRow);
                    int range = action.AreaRange;
                    for (int r = 0; r < BoardHelper.CombatHeight; r++)
                    {
                        for (int c = 0; c < BoardHelper.CombatWidth; c++)
                        {
                            if (BoardHelper.ManhattanDistance(centerCol, centerRow, c, r) > range)
                                continue;
                            eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId,
                                (byte)action.VfxIndex, col: (byte)c, row: (byte)r, useGridPos: true);
                        }
                    }
                    break;
                }
            }
        }

        // ══════════════════════════════
        // 유틸리티
        // ══════════════════════════════

        private static void GetAreaCenter(SkillExecuteContext ctx, out int col, out int row)
        {
            int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (idx >= 0)
            {
                col = ctx.State.Units[idx].GridCol;
                row = ctx.State.Units[idx].GridRow;
            }
            else
            {
                ref var caster = ref ctx.GetCaster();
                col = caster.GridCol;
                row = caster.GridRow;
            }
        }
    }

    /// <summary>액션 실행에 필요한 컨텍스트 (GC-free struct)</summary>
    public struct SkillExecuteContext
    {
        public CombatMatchState State;
        public int CasterCombatId;
        public int TargetCombatId;
        public DamageType DamageType;
        public int SkillSpecId;
        public byte CasterTeam;
        public int WorldTickRate;
        public DeterministicRNG Rng;

        /// <summary>ParamSlots로 추출된 밸런스 수치 배열</summary>
        public int[] ParamValues;
        /// <summary>base PowerPercent (ParamIndex == -1일 때 사용)</summary>
        public int BasePowerPercent;
        /// <summary>SkillParams.CCDurationFrames 오버라이드 (SecondaryParamIndex == -2일 때 사용)</summary>
        public int CCDurationOverride;
        /// <summary>현재 채널링 틱 번호 (SequentialLineDamage 등에서 step으로 사용)</summary>
        public int TickCount;

        public ref CombatUnit GetCaster()
        {
            int idx = State.FindUnitIndex(CasterCombatId);
            if (idx < 0) idx = 0; // 캐스터 사망/제거 시 안전 폴백 (스킬 효과는 무효화됨)
            return ref State.Units[idx];
        }

        public bool IsCasterAlive()
        {
            int idx = State.FindUnitIndex(CasterCombatId);
            return idx >= 0 && State.Units[idx].CurrentHP > 0;
        }

        public int GetParamValue(int paramIndex)
        {
            if (paramIndex == -2) return CCDurationOverride; // SkillParams.CCDurationFrames
            if (paramIndex < 0) return BasePowerPercent;
            if (ParamValues != null && paramIndex < ParamValues.Length)
                return ParamValues[paramIndex];
            return BasePowerPercent;
        }
    }
}
