namespace CookApps.AutoChess
{
    /// <summary>
    /// 대쉬 시스템. 단일 페이즈 실행 + 완료 신호.
    /// 페이즈 순서(Rush→Overshoot→Return)는 레시피가 Execute1/2/3으로 정의.
    /// DashSystem은 내부 페이즈 전환 없이 하나의 페이즈만 실행.
    /// </summary>
    public static class DashSystem
    {
        // ══════════════════════════════
        // 페이즈 시작 (SimSkillGeneric에서 호출)
        // ══════════════════════════════

        public static void StartPhase(CombatMatchState state, ref CombatUnit caster,
            ref SkillState skillState, ref SkillAction action, SkillExecuteContext ctx, int tickRate)
        {
            switch (action.DashPhaseType)
            {
                case DashPhase.Rush:
                    StartRush(state, ref caster, ref skillState, ref action, ctx, tickRate);
                    break;
                case DashPhase.Overshoot:
                    StartOvershoot(state, ref caster, ref skillState, ref action, ctx, tickRate);
                    break;
                case DashPhase.Return:
                    StartReturn(state, ref caster, ref skillState, ref action, tickRate);
                    break;
            }
        }

        // ══════════════════════════════
        // 이동 완료 (채널링 틱에서 호출)
        // ══════════════════════════════

        /// <summary>MoveTimer=0 도달 시 호출. true=아직 이동 중(Rush 타일 남음), false=페이즈 완료.</summary>
        public static bool OnMoveComplete(CombatMatchState state, ref CombatUnit unit, ref SkillState skillState)
        {
            if (unit.DashPhase == DashPhase.Rush)
            {
                ref var dash = ref skillState.Custom.Dash;
                ApplyHitOnCurrentTile(state, ref unit, ref dash);
                dash.DashTilesRemaining--;

                if (dash.DashTilesRemaining > 0)
                {
                    int nc = unit.GridCol + dash.DashDirCol;
                    int nr = unit.GridRow + dash.DashDirRow;
                    if (BoardHelper.IsValidCombatPosition(nc, nr))
                    {
                        MoveToNextTile(state, ref unit, ref dash);
                        return true; // Rush 진행 중
                    }
                }
            }

            // 페이즈 완료 (Rush 끝 or Overshoot/Return 끝)
            unit.DashPhase = DashPhase.None;
            unit.DashEase = MoveEaseType.None;
            return false;
        }

        public static bool IsActive(ref CombatUnit unit) => unit.DashPhase != DashPhase.None;

        // ══════════════════════════════
        // Rush: 타겟 방향 N타일 돌진
        // ══════════════════════════════

        private static void StartRush(CombatMatchState state, ref CombatUnit caster,
            ref SkillState skillState, ref SkillAction action, SkillExecuteContext ctx, int tickRate)
        {
            int targetIdx = state.FindUnitIndex(ctx.TargetCombatId);
            if (targetIdx < 0) return;
            ref var target = ref state.Units[targetIdx];

            int maxDist = action.AreaRange > 0 ? action.AreaRange : 3;

            // 타겟 방향 (단일 축, row 우선)
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (dirCol != 0) dirCol = dirCol > 0 ? 1 : -1;
            if (dirRow != 0) dirRow = dirRow > 0 ? 1 : -1;
            if (dirCol != 0 && dirRow != 0) dirCol = 0;
            if (dirCol == 0 && dirRow == 0) return;

            // 이동 가능 거리
            int actualDist = 0;
            for (int step = 1; step <= maxDist; step++)
            {
                int nc = caster.GridCol + dirCol * step;
                int nr = caster.GridRow + dirRow * step;
                if (!BoardHelper.IsValidCombatPosition(nc, nr)) break;
                int occ = state.GetUnitAtGrid(nc, nr);
                if (occ != CombatUnit.InvalidId)
                {
                    int oi = state.FindUnitIndex(occ);
                    if (oi >= 0 && state.Units[oi].TeamIndex == caster.TeamIndex) break;
                }
                actualDist = step;
            }
            if (actualDist <= 0) return;

            // 타이밍
            int durationMs = action.DashDurationMs > 0 ? action.DashDurationMs : 500;
            int totalFrames = MsToFrames(durationMs, tickRate);
            int framesPerTile = totalFrames / actualDist;
            if (framesPerTile < 1) framesPerTile = 1;

            // 데미지 + 스턴
            int power = ctx.GetParamValue(action.ParamIndex);
            int damage = caster.Attack * power / 100;
            if (damage < 1) damage = 1;
            short stunFrames = action.CCType != CrowdControlType.None
                ? (short)ctx.GetParamValue(action.SecondaryParamIndex) : (short)0;

            // DashState 설정
            ref var dash = ref skillState.Custom.Dash;
            dash.DashTilesRemaining = (byte)actualDist;
            dash.DashDirCol = (sbyte)dirCol;
            dash.DashDirRow = (sbyte)dirRow;
            dash.DashHitDamage = damage;
            dash.DashStunFrames = stunFrames;
            dash.DashFramesPerTile = framesPerTile;
            dash.DashHitFrameIndex = action.HitFrameIndex;

            // 뷰 상태
            caster.DashPhase = DashPhase.Rush;
            caster.DashEase = action.DashEaseType != MoveEaseType.None ? action.DashEaseType : MoveEaseType.OutQuad;

            // Rush VFX: 목적지에 전방 포탈 스폰
            if (action.VfxIndex >= 0)
            {
                int destCol = caster.GridCol + dirCol * actualDist;
                int destRow = caster.GridRow + dirRow * actualDist;
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                    (sbyte)dirCol, (sbyte)dirRow,
                    col: (byte)destCol, row: (byte)destRow, useGridPos: true);
            }

            MoveToNextTile(state, ref caster, ref dash);
        }

        // ══════════════════════════════
        // Overshoot: 비주얼 오프셋
        // ══════════════════════════════

        private static void StartOvershoot(CombatMatchState state, ref CombatUnit caster,
            ref SkillState skillState, ref SkillAction action, SkillExecuteContext ctx, int tickRate)
        {
            ref var dash = ref skillState.Custom.Dash;
            int durationMs = action.DashDurationMs > 0 ? action.DashDurationMs : 300;

            caster.DashPhase = DashPhase.Overshoot;
            caster.DashEase = action.DashEaseType != MoveEaseType.None ? action.DashEaseType : MoveEaseType.Linear;
            caster.MoveFromCol = (byte)(caster.GridCol - dash.DashDirCol);
            caster.MoveFromRow = (byte)(caster.GridRow - dash.DashDirRow);
            caster.MoveDuration = MsToFrames(durationMs, tickRate);
            caster.MoveTimer = caster.MoveDuration;

            // Overshoot VFX: 원위치에 복귀 포탈 스폰 (돌진 반대 방향 + offset)
            if (action.VfxIndex >= 0)
            {
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, ctx.SkillSpecId, (byte)action.VfxIndex,
                    (sbyte)(-dash.DashDirCol), (sbyte)(-dash.DashDirRow),
                    col: skillState.SavedGridCol, row: skillState.SavedGridRow, useGridPos: true,
                    vfxDirOffset: action.VfxDirOffset);
            }

            dash.DashHitFrameIndex = action.HitFrameIndex;
        }

        // ══════════════════════════════
        // Return: 텔레포트 + 착지
        // ══════════════════════════════

        private static void StartReturn(CombatMatchState state, ref CombatUnit caster,
            ref SkillState skillState, ref SkillAction action, int tickRate)
        {
            ref var dash = ref skillState.Custom.Dash;
            int durationMs = action.DashDurationMs > 0 ? action.DashDurationMs : 100;

            // 즉시 텔레포트
            state.ClearGrid(caster.GridCol, caster.GridRow);
            caster.GridCol = skillState.SavedGridCol;
            caster.GridRow = skillState.SavedGridRow;
            int occupant = state.GetUnitAtGrid(skillState.SavedGridCol, skillState.SavedGridRow);
            if (occupant == CombatUnit.InvalidId)
                state.SetGrid(skillState.SavedGridCol, skillState.SavedGridRow, caster.CombatId);

            // 착지 슬라이드 (원위치 1칸 뒤 → 원위치)
            caster.DashPhase = DashPhase.Return;
            caster.DashEase = action.DashEaseType != MoveEaseType.None ? action.DashEaseType : MoveEaseType.InExpo;
            caster.MoveFromCol = (byte)(skillState.SavedGridCol - dash.DashDirCol);
            caster.MoveFromRow = (byte)(skillState.SavedGridRow - dash.DashDirRow);
            caster.MoveDuration = MsToFrames(durationMs, tickRate);
            caster.MoveTimer = caster.MoveDuration;

            state.EventQueue?.PushUnitMoved(caster.CombatId, skillState.SavedGridCol, skillState.SavedGridRow);

            dash.DashHitFrameIndex = action.HitFrameIndex;
        }

        // ══════════════════════════════
        // 유틸리티
        // ══════════════════════════════

        private static void MoveToNextTile(CombatMatchState state, ref CombatUnit unit, ref DashState dash)
        {
            int nextCol = unit.GridCol + dash.DashDirCol;
            int nextRow = unit.GridRow + dash.DashDirRow;

            unit.MoveFromCol = unit.GridCol;
            unit.MoveFromRow = unit.GridRow;

            int occupant = state.GetUnitAtGrid(nextCol, nextRow);
            state.ClearGrid(unit.GridCol, unit.GridRow);
            unit.GridCol = (byte)nextCol;
            unit.GridRow = (byte)nextRow;
            if (occupant == CombatUnit.InvalidId)
                state.SetGrid(nextCol, nextRow, unit.CombatId);

            unit.MoveDuration = dash.DashFramesPerTile;
            unit.MoveTimer = dash.DashFramesPerTile;

            state.EventQueue?.PushUnitMoved(unit.CombatId, (byte)nextCol, (byte)nextRow);
        }

        private static void ApplyHitOnCurrentTile(CombatMatchState state, ref CombatUnit unit, ref DashState dash)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var other = ref state.Units[i];
                if (!other.IsAlive) continue;
                if (other.TeamIndex == unit.TeamIndex) continue;
                if (other.GridCol != unit.GridCol || other.GridRow != unit.GridRow) continue;

                if (dash.DashHitDamage > 0)
                {
                    int attackerIdx = state.FindUnitIndex(unit.CombatId);
                    DamageSystem.ApplyDamage(state, ref other, dash.DashHitDamage, attackerIdx);
                }

                if (dash.DashStunFrames > 0)
                    SkillCCHelper.ApplyCC(state, ref other, CrowdControlType.Stun, dash.DashStunFrames);
            }
        }

        private static int MsToFrames(int ms, int tickRate)
            => (int)(ms * 0.001f * tickRate + 0.5f);
    }
}
