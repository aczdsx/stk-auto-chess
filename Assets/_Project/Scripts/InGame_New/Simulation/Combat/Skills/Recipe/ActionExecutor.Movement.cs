namespace CookApps.AutoChess
{
    /// <summary>
    /// ActionExecutor — 이동/텔레포트/VFX.
    /// SpawnVfx, Teleport, TeleportReturn, Dash, DashReturn, Retarget, TileEffect.
    /// </summary>
    public static partial class ActionExecutor
    {
        private static void SpawnVfx(ref SkillAction action, SkillExecuteContext ctx)
        {
            var eq = ctx.State.EventQueue;
            if (eq == null) return;

            switch (action.VfxAt)
            {
                case SkillVfxPlacement.AtCaster:
                    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                        vfxDirOffset: action.VfxDirOffset);
                    break;
                case SkillVfxPlacement.AtTarget:
                    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                        targetId: ctx.TargetCombatId, vfxDirOffset: action.VfxDirOffset);
                    break;
                case SkillVfxPlacement.AtGridPos:
                {
                    int idx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    if (idx >= 0)
                    {
                        ref var t = ref ctx.State.Units[idx];
                        eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                            col: (byte)t.GridCol, row: (byte)t.GridRow, useGridPos: true,
                            vfxDirOffset: action.VfxDirOffset);
                    }
                    break;
                }
                case SkillVfxPlacement.AtCasterToSaved:
                {
                    // 현재 위치에 VFX, 방향은 저장 위치(스킬 시작 위치)를 향함
                    ref var c2 = ref ctx.GetCaster();
                    int dcS = ctx.SavedGridCol - c2.GridCol;
                    int drS = ctx.SavedGridRow - c2.GridRow;
                    sbyte dirCS = (sbyte)(dcS > 0 ? 1 : (dcS < 0 ? -1 : 0));
                    sbyte dirRS = (sbyte)(drS > 0 ? 1 : (drS < 0 ? -1 : 0));
                    eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                        dirCol: dirCS, dirRow: dirRS, vfxDirOffset: action.VfxDirOffset);
                    break;
                }
                case SkillVfxPlacement.AtTargetWithDir:
                {
                    // 타겟 위치 + 시전자→타겟 방향 회전 (빅마우스 포탈 등)
                    int tIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    if (tIdx >= 0)
                    {
                        ref var tgt = ref ctx.State.Units[tIdx];
                        ref var src = ref ctx.GetCaster();
                        int dc = tgt.GridCol - src.GridCol;
                        int dr = tgt.GridRow - src.GridRow;
                        sbyte dC = (sbyte)(dc > 0 ? 1 : (dc < 0 ? -1 : 0));
                        sbyte dR = (sbyte)(dr > 0 ? 1 : (dr < 0 ? -1 : 0));
                        eq.PushSkillPhaseVfx(ctx.CasterCombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                            col: tgt.GridCol, row: tgt.GridRow, useGridPos: true,
                            dirCol: dC, dirRow: dR, vfxDirOffset: action.VfxDirOffset);
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
                        dirCol: dirCol, dirRow: dirRow, vfxDirOffset: action.VfxDirOffset);
                    break;
                }
                case SkillVfxPlacement.AreaEffect:
                {
                    int centerCol, centerRow;
                    GetAreaCenter(ctx, out centerCol, out centerRow);
                    eq.PushSkillAreaEffect(ctx.CasterCombatId,
                        (byte)centerCol, (byte)centerRow, action.AreaRange, isBox: action.IsBoxArea);
                    break;
                }
                case SkillVfxPlacement.RectAreaEffect:
                {
                    ref var caster = ref ctx.GetCaster();
                    int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                    sbyte dirCol = 0, dirRow = 0;
                    if (targetIdx >= 0)
                    {
                        ref var t = ref ctx.State.Units[targetIdx];
                        int dc = t.GridCol - caster.GridCol;
                        int dr = t.GridRow - caster.GridRow;
                        dirCol = (sbyte)(dc > 0 ? 1 : (dc < 0 ? -1 : 0));
                        dirRow = (sbyte)(dr > 0 ? 1 : (dr < 0 ? -1 : 0));
                    }
                    eq.PushSkillRectAreaEffect(ctx.CasterCombatId,
                        (byte)caster.GridCol, (byte)caster.GridRow, dirCol, dirRow);
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

        private static void ExecuteTeleport(ref SkillAction action, SkillExecuteContext ctx)
        {
            ref var caster = ref ctx.GetCaster();

            if (action.TeleportDistance > 0)
            {
                // 전방 N칸 (오데트)
                int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                int dirCol, dirRow;
                if (targetIdx >= 0)
                {
                    ref var t = ref ctx.State.Units[targetIdx];
                    int dc = t.GridCol - caster.GridCol;
                    int dr = t.GridRow - caster.GridRow;
                    dirCol = dc > 0 ? 1 : dc < 0 ? -1 : 0;
                    dirRow = dr > 0 ? 1 : dr < 0 ? -1 : 0;
                }
                else
                {
                    dirCol = 0;
                    dirRow = caster.TeamIndex == 0 ? 1 : -1;
                }

                int destCol = caster.GridCol + dirCol * action.TeleportDistance;
                int destRow = caster.GridRow + dirRow * action.TeleportDistance;
                TryTeleport(ctx.State, ref caster, destCol, destRow);
            }
            else
            {
                // 타겟 뒤로 (마리에/시라유키)
                int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (targetIdx >= 0)
                    TeleportBehindTarget(ctx.State, ref caster, ref ctx.State.Units[targetIdx]);
            }
        }

        private static void TeleportBehindTarget(CombatMatchState state, ref CombatUnit caster, ref CombatUnit target)
        {
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (dirCol != 0) dirCol = dirCol > 0 ? 1 : -1;
            if (dirRow != 0) dirRow = dirRow > 0 ? 1 : -1;

            int behindCol = target.GridCol + dirCol;
            int behindRow = target.GridRow + dirRow;
            if (TryTeleport(state, ref caster, behindCol, behindRow)) return;

            for (int d = 1; d <= 2; d++)
                for (int dc = -d; dc <= d; dc++)
                    for (int dr = -d; dr <= d; dr++)
                    {
                        if (dc == 0 && dr == 0) continue;
                        if (TryTeleport(state, ref caster, target.GridCol + dc, target.GridRow + dr))
                            return;
                    }
        }

        private static bool TryTeleport(CombatMatchState state, ref CombatUnit caster, int col, int row)
        {
            if (!BoardHelper.IsValidCombatPosition(col, row)) return false;
            if (state.GetUnitAtGrid(col, row) != CombatUnit.InvalidId) return false;
            state.ClearGrid(caster.GridCol, caster.GridRow);
            caster.GridCol = (byte)col;
            caster.GridRow = (byte)row;
            state.SetGrid(col, row, caster.CombatId);
            state.EventQueue?.PushUnitMoved(caster.CombatId, (byte)col, (byte)row);
            return true;
        }

        private static void ExecuteRetarget(ref SkillAction action, SkillExecuteContext ctx)
        {
            ref var caster = ref ctx.GetCaster();
            byte enemyTeam = (byte)(1 - ctx.CasterTeam);
            int newTarget = CombatUnit.InvalidId;

            if (action.TargetFilter == SkillTargetFilter.NearestEnemy)
            {
                // 가장 가까운 미피격 적 (베인 바운스)
                int bestDist = int.MaxValue;
                int refCol = 0, refRow = 0;
                int curIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                if (curIdx >= 0) { refCol = ctx.State.Units[curIdx].GridCol; refRow = ctx.State.Units[curIdx].GridRow; }

                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var u = ref ctx.State.Units[i];
                    if (!u.IsAlive || u.TeamIndex != enemyTeam) continue;
                    if (action.ExcludeHit && IsInHitList(u.CombatId, ctx.HitIds, ctx.HitIdCount)) continue;
                    int dist = System.Math.Abs(u.GridCol - refCol) + System.Math.Abs(u.GridRow - refRow);
                    if (dist < bestDist) { bestDist = dist; newTarget = u.CombatId; }
                }
            }
            else
            {
                // 최저HP 적 (미노/시라유키)
                int bestHp = int.MaxValue;
                for (int i = 0; i < ctx.State.UnitCount; i++)
                {
                    ref var u = ref ctx.State.Units[i];
                    if (!u.IsAlive || u.TeamIndex != enemyTeam) continue;
                    if (action.ExcludeHit && IsInHitList(u.CombatId, ctx.HitIds, ctx.HitIdCount)) continue;
                    if (u.CurrentHP < bestHp) { bestHp = u.CurrentHP; newTarget = u.CombatId; }
                }
            }

            if (newTarget != CombatUnit.InvalidId)
                ctx.TargetCombatId = newTarget;
        }

        private static bool IsInHitList(int combatId, int[] hitIds, int hitIdCount)
        {
            if (hitIds == null) return false;
            for (int i = 0; i < hitIdCount; i++)
                if (hitIds[i] == combatId) return true;
            return false;
        }

        /// <summary>타일 이펙트 발행 — 기존 EventQueue의 PushSkillAreaEffect/PushSkillRectAreaEffect 호출</summary>
        private static void ExecuteTileEffect(ref SkillAction action, SkillExecuteContext ctx)
        {
            var eq = ctx.State.EventQueue;
            if (eq == null) return;

            if (action.AreaShape == SkillAreaShape.Rect)
            {
                // ㄷ자형 타일 이펙트 (오데트 Phase1)
                ref var caster = ref ctx.GetCaster();
                int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
                sbyte dirCol = 0, dirRow = 0;
                if (targetIdx >= 0)
                {
                    ref var t = ref ctx.State.Units[targetIdx];
                    int dc = t.GridCol - caster.GridCol;
                    int dr = t.GridRow - caster.GridRow;
                    dirCol = (sbyte)(dc > 0 ? 1 : (dc < 0 ? -1 : 0));
                    dirRow = (sbyte)(dr > 0 ? 1 : (dr < 0 ? -1 : 0));
                }
                if (dirCol == 0 && dirRow == 0)
                    dirRow = (sbyte)(caster.TeamIndex == 0 ? 1 : -1);

                eq.PushSkillRectAreaEffect(ctx.CasterCombatId,
                    (byte)caster.GridCol, (byte)caster.GridRow, dirCol, dirRow);
            }
            else
            {
                // 일반 범위 타일 이펙트 — TargetFilter로 위치 기준 결정
                int centerCol, centerRow;
                if (action.TargetFilter == SkillTargetFilter.Self)
                {
                    ref var caster = ref ctx.GetCaster();
                    centerCol = caster.GridCol;
                    centerRow = caster.GridRow;
                }
                else
                {
                    GetAreaCenter(ctx, out centerCol, out centerRow);
                }
                eq.PushSkillAreaEffect(ctx.CasterCombatId,
                    (byte)centerCol, (byte)centerRow, action.AreaRange, isBox: action.IsBoxArea);
            }
        }

        /// <summary>스킬 시전 시작 위치로 즉시 복귀</summary>
        private static void ExecuteTeleportReturn(ref SkillAction action, SkillExecuteContext ctx)
        {
            ref var caster = ref ctx.GetCaster();
            TryTeleport(ctx.State, ref caster, ctx.SavedGridCol, ctx.SavedGridRow);
        }

        /// <summary>타겟 인접으로 Lerp 이동 (넉백 시스템 재사용)</summary>
        private static void ExecuteDash(ref SkillAction action, SkillExecuteContext ctx)
        {
            ref var caster = ref ctx.GetCaster();
            int targetIdx = ctx.State.FindUnitIndex(ctx.TargetCombatId);
            if (targetIdx < 0) return;

            ref var target = ref ctx.State.Units[targetIdx];

            // 타겟 뒤 또는 인접 빈 타일 탐색
            int destCol, destRow;
            if (!FindDashDestination(ctx.State, ref caster, ref target, out destCol, out destRow))
                return;

            int distance = System.Math.Abs(destCol - caster.GridCol) + System.Math.Abs(destRow - caster.GridRow);
            if (distance <= 0) return;

            // 넉백 시스템의 Lerp 보간 설정
            caster.MoveFromCol = caster.GridCol;
            caster.MoveFromRow = caster.GridRow;

            ctx.State.ClearGrid(caster.GridCol, caster.GridRow);
            caster.GridCol = (byte)destCol;
            caster.GridRow = (byte)destRow;
            ctx.State.SetGrid(destCol, destRow, caster.CombatId);

            // 대쉬 이동 시간: 0.3초 + 거리당 0.05초
            int dashFrames = (int)((0.3f + distance * 0.05f) * ctx.WorldTickRate + 0.5f);
            caster.MoveDuration = dashFrames;
            caster.MoveTimer = dashFrames;
            caster.IsKnockbackMoving = true;

            ctx.State.EventQueue?.PushUnitMoved(caster.CombatId, (byte)destCol, (byte)destRow);
        }

        /// <summary>스킬 시전 시작 위치로 Lerp 복귀</summary>
        private static void ExecuteDashReturn(ref SkillAction action, SkillExecuteContext ctx)
        {
            ref var caster = ref ctx.GetCaster();
            int savedCol = ctx.SavedGridCol;
            int savedRow = ctx.SavedGridRow;

            if (caster.GridCol == savedCol && caster.GridRow == savedRow) return;

            int distance = System.Math.Abs(savedCol - caster.GridCol) + System.Math.Abs(savedRow - caster.GridRow);

            caster.MoveFromCol = caster.GridCol;
            caster.MoveFromRow = caster.GridRow;

            ctx.State.ClearGrid(caster.GridCol, caster.GridRow);
            caster.GridCol = (byte)savedCol;
            caster.GridRow = (byte)savedRow;
            ctx.State.SetGrid(savedCol, savedRow, caster.CombatId);

            int dashFrames = (int)((0.1f + distance * 0.03f) * ctx.WorldTickRate + 0.5f);
            caster.MoveDuration = dashFrames;
            caster.MoveTimer = dashFrames;
            caster.IsKnockbackMoving = true;

            ctx.State.EventQueue?.PushUnitMoved(caster.CombatId, (byte)savedCol, (byte)savedRow);
        }

        /// <summary>대쉬 목적지 찾기 (타겟 인접 빈 타일)</summary>
        private static bool FindDashDestination(CombatMatchState state, ref CombatUnit caster,
            ref CombatUnit target, out int destCol, out int destRow)
        {
            // 타겟 뒤쪽 먼저
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (dirCol != 0) dirCol = dirCol > 0 ? 1 : -1;
            if (dirRow != 0) dirRow = dirRow > 0 ? 1 : -1;

            destCol = target.GridCol + dirCol;
            destRow = target.GridRow + dirRow;
            if (BoardHelper.IsValidCombatPosition(destCol, destRow)
                && state.GetUnitAtGrid(destCol, destRow) == CombatUnit.InvalidId)
                return true;

            // 인접 빈 타일 탐색
            for (int d = 1; d <= 2; d++)
                for (int dc = -d; dc <= d; dc++)
                    for (int dr = -d; dr <= d; dr++)
                    {
                        if (dc == 0 && dr == 0) continue;
                        destCol = target.GridCol + dc;
                        destRow = target.GridRow + dr;
                        if (BoardHelper.IsValidCombatPosition(destCol, destRow)
                            && state.GetUnitAtGrid(destCol, destRow) == CombatUnit.InvalidId)
                            return true;
                    }

            destCol = caster.GridCol;
            destRow = caster.GridRow;
            return false;
        }

        // DashForward는 SimSkillGeneric.ExecuteActionWithSpecialHandling에서 직접 처리
    }
}
