namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 오케스트레이션 시스템.
    /// 매치 시작 시 유닛별 스킬 인스턴스를 생성하고,
    /// 마나 풀 시 스킬 시전 → 시전 시간 경과 → 효과 적용 흐름을 관리.
    /// 스킬 인스턴스는 CombatMatchState.Skills[]에 매치별로 저장.
    /// </summary>
    public static class SkillSystem
    {
        /// <summary>매치 시작 시 유닛별 스킬 인스턴스 생성</summary>
        public static void SetupSkills(CombatMatchState state, GameWorld world)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (unit.SkillSpecId <= 0) continue;

                var skill = SkillFactory.Create(unit.SkillSpecId);
                if (skill == null) continue;

                // 스킬 파라미터 생성 (추후 SkillActive 테이블 기반)
                var skillParams = new SkillParams
                {
                    SkillId = unit.SkillSpecId,
                    PowerPercent = 200, // 기본 배율 (추후 SkillActive 테이블에서)
                    DamageType = DamageType.Magical,
                    CastFrames = 0,
                };
                skill.Initialize(skillParams);
                state.Skills[i] = skill;
            }
        }

        /// <summary>마나 풀 시 스킬 시전 시도. 성공 시 true.</summary>
        public static bool TryCast(CombatMatchState state, ref CombatUnit unit, int unitIndex,
            ref DeterministicRNG rng)
        {
            if (unit.CurrentMana < unit.MaxMana || unit.MaxMana <= 0)
                return false;

            var skill = state.Skills[unitIndex];
            if (skill == null)
            {
                // 스킬이 없으면 마나만 리셋
                unit.CurrentMana = 0;
                return false;
            }

            if (!skill.CanCast(state, ref unit))
                return false;

            int targetId = skill.SelectTarget(state, ref unit);
            if (targetId == CombatUnit.InvalidId)
                return false;

            int castFrames = skill.GetCastFrames();
            unit.CurrentMana = 0;

            if (CombatLogger.Enabled) CombatLogger.LogSkillCast(unit.CombatId, targetId, unit.SkillSpecId, castFrames <= 0);

            if (castFrames > 0)
            {
                // 시전 시간이 있는 스킬: CastingSkill 상태로 전환
                unit.State = CombatState.CastingSkill;
                unit.SkillCastTimer = castFrames;
                unit.CurrentTargetId = targetId;
            }
            else
            {
                // 즉시 시전 스킬
                unit.State = CombatState.CastingSkill;
                skill.Execute(state, ref unit, targetId, ref rng);

                // 이벤트 발행
                state.EventQueue?.PushUnitCastSkill(
                    unit.SourceEntityId,
                    targetId != CombatUnit.InvalidId ? GetSourceEntityId(state, targetId) : CombatUnit.InvalidId,
                    unit.SkillSpecId);
            }

            return true;
        }

        /// <summary>시전 중 틱 처리. 시전 완료 시 효과 적용.</summary>
        public static void TickCasting(CombatMatchState state, ref CombatUnit unit, int unitIndex,
            ref DeterministicRNG rng)
        {
            if (unit.State != CombatState.CastingSkill) return;

            unit.SkillCastTimer--;
            if (unit.SkillCastTimer > 0) return;

            // 시전 완료: 효과 적용
            var skill = state.Skills[unitIndex];
            if (skill != null)
            {
                int targetId = unit.CurrentTargetId;

                // 타겟 유효성 재검증
                if (!TargetingSystem.IsTargetValid(state, targetId))
                {
                    targetId = skill.SelectTarget(state, ref unit);
                }

                if (targetId != CombatUnit.InvalidId)
                {
                    if (CombatLogger.Enabled) CombatLogger.LogSkillExecute(unit.CombatId, targetId, unit.SkillSpecId);

                    skill.Execute(state, ref unit, targetId, ref rng);

                    state.EventQueue?.PushUnitCastSkill(
                        unit.SourceEntityId,
                        GetSourceEntityId(state, targetId),
                        unit.SkillSpecId);
                }
            }

            unit.State = CombatState.Idle;
            unit.CurrentTargetId = CombatUnit.InvalidId;
        }

        /// <summary>매치 종료 시 정리</summary>
        public static void Cleanup(CombatMatchState state)
        {
            if (state?.Skills == null) return;
            for (int i = 0; i < CombatMatchState.MaxCombatUnits; i++)
            {
                if (state.Skills[i] != null)
                {
                    state.Skills[i].Reset();
                    state.Skills[i] = null;
                }
            }
        }

        private static int GetSourceEntityId(CombatMatchState state, int combatId)
        {
            int idx = state.FindUnitIndex(combatId);
            if (idx < 0) return CombatUnit.InvalidId;
            return state.Units[idx].SourceEntityId;
        }
    }
}