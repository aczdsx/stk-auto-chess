namespace CookApps.AutoChess
{
    /// <summary>
    /// ActionExecutor — 데미지 계열.
    /// Damage, MultiHit, DamageKnockbackInArea, SequentialLineDamage, SpawnProjectile, SpawnLinearProjectile.
    /// </summary>
    public static partial class ActionExecutor
    {
        private static void ExecuteDamage(ref SkillAction action, SkillExecuteContext ctx)
        {
            // DecayParamIndex가 있으면 감쇠된 CurrentPower 사용 (베인 바운스)
            int power = action.DecayParamIndex >= 0
                ? ctx.CurrentPower
                : ctx.GetParamValue(action.ParamIndex);

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
                    // 타겟 방향 계산 (팀 기본 방향 대신 실제 타겟 방향)
                    int targetIdx3 = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    int dirCol, dirRow;
                    if (targetIdx3 >= 0)
                    {
                        int dc = state.Units[targetIdx3].GridCol - caster.GridCol;
                        int dr = state.Units[targetIdx3].GridRow - caster.GridRow;
                        if (System.Math.Abs(dc) >= System.Math.Abs(dr))
                        { dirCol = dc > 0 ? 1 : -1; dirRow = 0; }
                        else
                        { dirRow = dr > 0 ? 1 : -1; dirCol = 0; }
                    }
                    else
                    {
                        dirCol = caster.TeamIndex == 0 ? 1 : -1;
                        dirRow = 0;
                    }

                    // range 0 = 타겟까지 거리 (대쉬형 스킬)
                    int range = action.AreaRange;
                    if (range == 0 && targetIdx3 >= 0)
                        range = System.Math.Abs(state.Units[targetIdx3].GridCol - caster.GridCol)
                              + System.Math.Abs(state.Units[targetIdx3].GridRow - caster.GridRow);
                    if (range <= 0) range = 1;

                    int col = caster.GridCol;
                    int row = caster.GridRow;
                    for (int step = 0; step < range; step++)
                    {
                        col += dirCol;
                        row += dirRow;
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
                else if (action.AreaShape == SkillAreaShape.Rect)
                {
                    // Rect: 방향 기반 직사각형 (시전자 전방)
                    ref var casterUnit = ref state.Units[casterIdx];
                    int targetIdx2 = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    int dirCol = 0, dirRow = 0;
                    if (targetIdx2 >= 0)
                    {
                        int dc = state.Units[targetIdx2].GridCol - casterUnit.GridCol;
                        int dr = state.Units[targetIdx2].GridRow - casterUnit.GridRow;
                        dirCol = dc > 0 ? 1 : dc < 0 ? -1 : 0;
                        dirRow = dr > 0 ? 1 : dr < 0 ? -1 : 0;
                    }
                    if (dirCol == 0 && dirRow == 0)
                        dirRow = ctx.CasterTeam == 0 ? 1 : -1;

                    int halfWidth = action.AreaRange;
                    int depth = action.RectDepth > 0 ? action.RectDepth : 1;
                    bool rowDominant = dirRow != 0;

                    for (int i = 0; i < state.UnitCount; i++)
                    {
                        ref var unit = ref state.Units[i];
                        if (!unit.IsAlive || unit.TeamIndex == ctx.CasterTeam) continue;

                        bool inRange;
                        if (rowDominant)
                        {
                            // 가로(col) halfWidth, 세로(row) 전방 depth
                            int dc = unit.GridCol - casterUnit.GridCol;
                            if (dc < -halfWidth || dc > halfWidth) continue;
                            int rd = (unit.GridRow - casterUnit.GridRow) * dirRow;
                            inRange = rd >= 0 && rd <= depth;
                        }
                        else
                        {
                            // 세로(row) halfWidth, 가로(col) 전방 depth
                            int dr = unit.GridRow - casterUnit.GridRow;
                            if (dr < -halfWidth || dr > halfWidth) continue;
                            int cd = (unit.GridCol - casterUnit.GridCol) * dirCol;
                            inRange = cd >= 0 && cd <= depth;
                        }
                        if (!inRange) continue;

                        int raw = attack * power / 100;
                        int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref unit);
                        DamageSystem.ApplyDamage(state, ref unit, dmg, casterIdx, type);
                        DamageSystem.ChargeMana(ref unit, unit.ManaGainOnHit);
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
                        if (action.ExcludePrimary && unit.CombatId == ctx.TargetCombatId) continue;
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

        private static void ExecuteSpawnProjectile(ref SkillAction action, SkillExecuteContext ctx)
        {
            int power = ctx.GetParamValue(action.ParamIndex);
            int raw = ctx.GetCaster().Attack * power / 100;

            ProjectileSystem.CreateHomingProjectile(
                ctx.State, ctx.CasterCombatId, ctx.TargetCombatId,
                raw, false, ctx.DamageType,
                action.RepeatIntervalFrames > 0 ? action.RepeatIntervalFrames : 30,
                ctx.SkillSpecId, action.VfxIndex, action.UseBezier,
                action.ArrivalVfxIndex);
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
    }
}
