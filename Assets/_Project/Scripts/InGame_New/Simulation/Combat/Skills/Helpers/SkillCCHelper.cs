namespace CookApps.AutoChess
{
    /// <summary>CC 적용 헬퍼</summary>
    public static class SkillCCHelper
    {
        /// <summary>
        /// 스킬 시전 중 CC로 중단될 때 처리.
        /// 첫 효과 키프레임 이전이면 마나 복원, 이후면 소모 유지 (채널링만 중단).
        ///
        /// ■ Instant (SkillCastTimer 카운트다운): CastTimer > 0 → Execute 전 → 마나 복원
        /// ■ Channeling/DelayedApply (SkillCastTimer 경과 카운터):
        ///   경과 프레임 < FirstEffectFrame → 효과 발동 전 → 마나 복원
        /// </summary>
        private static void CancelSkillCasting(CombatMatchState state, ref CombatUnit target)
        {
            if (target.State != CombatState.CastingSkill) return;

            bool shouldRestoreMana;
            int idx = state.FindUnitIndex(target.CombatId);
            var skill = idx >= 0 ? state.Skills[idx] : null;

            if (skill != null && skill.IsChanneling)
            {
                // 채널링: SkillCastTimer = 경과 프레임 (0부터 ++), FirstEffectFrame = 첫 효과 키프레임
                shouldRestoreMana = skill.FirstEffectFrame <= 0
                    || target.SkillCastTimer < skill.FirstEffectFrame;
            }
            else
            {
                // Instant: SkillCastTimer = 남은 프레임 (castFrames부터 --)
                shouldRestoreMana = target.SkillCastTimer > 0;
            }

            if (shouldRestoreMana)
                target.CurrentMana = target.MaxMana;

            target.SkillCastTimer = 0;
            target.State = CombatState.Idle;
            target.CurrentTargetId = CombatUnit.InvalidId;
        }

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

            // CC 면역 체크 (StatusEffect 기반)
            int idx = state.FindUnitIndex(target.CombatId);
            if (idx >= 0 && StatusEffectSystem.HasImmunity(state, idx, StatusEffectType.CCImmunity))
                return;

            // CC 면역 체크 (직업 패시브: CCImmuneCharges)
            if (target.CCImmuneCharges > 0)
            {
                target.CCImmuneCharges--;
                if (CombatLogger.Enabled) CombatLogger.LogCC(target.CombatId, type, 0); // 0 = 면역으로 무시됨
                // Striker 전용 VFX: 면역 버프 제거 + CC 방어 이펙트
                state.EventQueue?.PushStatusEffectRemoved(target.CombatId, CombatVfxType.JobStriker);
                state.EventQueue?.PushStatusEffectAdded(target.CombatId, CombatVfxType.JobStrikerBlock);
                return;
            }

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

            // 스킬 시전 중이면 마나 복원 후 취소
            CancelSkillCasting(state, ref target);

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

            // 스킬 시전 중이면 마나 복원 후 취소
            CancelSkillCasting(state, ref target);

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

                state.EventQueue?.PushUnitMoved(target.CombatId, (byte)col, (byte)row);
            }

            // 충돌 시 스턴 (1초 고정 — worldTickRate 프레임)
            if (actualMoved < distance)
            {
                ApplyCC(state, ref target, CrowdControlType.Stun, worldTickRate);
            }

            return actualMoved;
        }
    }
}
