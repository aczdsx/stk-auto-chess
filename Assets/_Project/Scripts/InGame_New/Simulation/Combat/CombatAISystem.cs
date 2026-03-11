namespace CookApps.AutoChess
{
    /// <summary>
    /// 전투 AI 시스템. 유닛 상태머신을 구동하고 전투 틱을 처리.
    /// 상태: Idle → Moving/Attacking → CastingSkill → Dead
    /// 매 전투 틱마다 Tick()을 호출하여 모든 유닛을 업데이트.
    /// </summary>
    public static class CombatAISystem
    {
        /// <summary>
        /// 전투 매치 1틱 실행. 모든 유닛 상태머신 + 투사체 처리.
        /// 반환값: 전투 종료 여부.
        /// </summary>
        public static bool Tick(CombatMatchState state, ref DeterministicRNG rng, int tickRate)
        {
            if (state.IsFinished) return true;

            if (CombatLogger.Enabled) CombatLogger.NextFrame();

            // 0. 전투 시작 시 Trait OnCombatStart (첫 프레임에서 1회)
            if (!state._traitCombatStartDone)
            {
                TraitSystem.InvokeCombatStart(state);
                state._traitCombatStartDone = true;
            }

            // 1. 전투 첫 프레임: 백라인 점프 처리
            ProcessBacklineJumps(state, tickRate);

            // 2. 유닛 상태머신 업데이트
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;

                // Trait: 매 틱 콜백
                TraitSystem.InvokeOnTick(state, i, tickRate);

                UpdateUnit(state, ref unit, ref rng, tickRate);
            }

            // 3. 투사체 처리
            ProjectileSystem.ProcessAll(state, ref rng);

            // 3.5. 상태효과 틱 (쉴드 만료, DOT, 버프 지속시간)
            StatusEffectSystem.Tick(state);

            // 4. 종료 조건 체크
            if (CheckEndCondition(state))
            {
                state.IsFinished = true;
                return true;
            }

            return false;
        }

        /// <summary>백라인 점프 (전투 시작 시 1회)</summary>
        private static void ProcessBacklineJumps(CombatMatchState state, int tickRate)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (!unit.HasBacklineJump || unit.BacklineJumpDone) continue;

                MovementSystem.TryBacklineJump(state, ref unit, tickRate);
            }
        }

        /// <summary>개별 유닛 상태머신 업데이트</summary>
        private static void UpdateUnit(CombatMatchState state, ref CombatUnit unit,
            ref DeterministicRNG rng, int tickRate)
        {
            // 범위 공격 진행 중 (키프레임 타이밍 기반 멀티히트)
            if (unit.IsAreaAttacking)
            {
                DamageSystem.TickAreaAttack(state, ref unit, tickRate);
                return;
            }

            // CC 상태 처리
            if (unit.State == CombatState.CrowdControlled)
            {
                unit.CCRemainingFrames--;
                if (unit.CCRemainingFrames <= 0)
                {
                    unit.ActiveCC = CrowdControlType.None;
                    unit.State = CombatState.Idle;
                }
                else
                {
                    return; // CC 중에는 행동 불가
                }
            }

            // 이동 중 처리 (MoveTimer 기반)
            if (unit.IsMoving)
            {
                unit.MoveTimer--;
                if (unit.MoveTimer <= 0)
                {
                    // 이동 완료
                    unit.State = CombatState.Idle;
                    unit.IsBacklineJumping = false;
                }
                else
                {
                    return; // 이동 중에는 행동 불가
                }
            }

            // 스킬 시전 중
            if (unit.State == CombatState.CastingSkill)
            {
                SkillSystem.TickCasting(state, ref unit, FindUnitSlotIndex(state, ref unit), ref rng);
                if (unit.State == CombatState.CastingSkill)
                    return; // 아직 시전 중
            }

            // 대기 중인 근접 공격 히트 처리
            if (unit.PendingAtkTimer > 0)
            {
                unit.PendingAtkTimer--;
                if (unit.PendingAtkTimer <= 0)
                {
                    DamageSystem.ExecutePendingMeleeHit(state, ref unit, ref rng, tickRate);
                }
                return; // 공격 애니메이션 중
            }

            // 타겟 갱신
            TargetingSystem.RefreshTarget(state, ref unit);

            // 타겟 없으면 Idle
            if (unit.CurrentTargetId == CombatUnit.InvalidId)
            {
                unit.State = CombatState.Idle;
                return;
            }

            int targetIdx = state.FindUnitIndex(unit.CurrentTargetId);
            if (targetIdx < 0)
            {
                unit.CurrentTargetId = CombatUnit.InvalidId;
                unit.State = CombatState.Idle;
                return;
            }

            ref var target = ref state.Units[targetIdx];

            // 스킬 준비 체크 (마나 ≥ MaxMana)
            if (unit.CurrentMana >= unit.MaxMana && unit.MaxMana > 0)
            {
                int unitSlot = FindUnitSlotIndex(state, ref unit);
                if (SkillSystem.TryCast(state, ref unit, unitSlot, ref rng))
                    return; // 스킬 시전 시작
            }

            // 사거리 내인지 체크
            bool inRange = TargetingSystem.IsTargetInRange(ref unit, ref target);

            if (inRange)
            {
                // 공격 쿨다운 감소
                unit.AttackCooldown--;

                if (unit.AttackCooldown <= 0)
                {
                    // 회피 판정
                    if (DamageSystem.TryDodge(ref target, ref rng))
                    {
                        // 회피 성공: 데미지 없이 쿨다운만 재설정
                        if (CombatLogger.Enabled) CombatLogger.LogDodge(unit.CombatId, target.CombatId);
                        unit.AttackCooldown = unit.GetAttackInterval(tickRate);
                    }
                    else
                    {
                        // 공격 실행
                        unit.State = CombatState.Attacking;
                        if (unit.AttackRange <= 1 && !unit.HasAreaAttack)
                        {
                            // 근접: ATK 키프레임까지 데미지 지연
                            unit.PendingAtkTargetId = target.CombatId;
                            unit.PendingAtkTimer = unit.AtkHitDelay;
                            unit.AttackCooldown = unit.GetAttackInterval(tickRate);

                            // 이벤트 발행 (View가 ATK 애니메이션 시작)
                            state.EventQueue?.PushUnitAttacked(
                                unit.SourceEntityId, target.SourceEntityId, 0, false, false, isPreTimed: true);
                        }
                        else
                        {
                            // 원거리/범위: 기존 즉시 실행 (투사체가 알아서 지연)
                            DamageSystem.ExecuteBasicAttack(state, ref unit, ref target, ref rng, tickRate);
                        }
                    }
                }
                else
                {
                    unit.State = CombatState.Idle;
                }
            }
            else
            {
                // 이동 시도
                unit.State = CombatState.Moving;
                bool moved = MovementSystem.TryMoveToward(state, ref unit, ref target, tickRate);

                if (!moved)
                {
                    // 이동 실패 (막힘): Idle로 복귀
                    unit.State = CombatState.Idle;
                }
            }
        }

        /// <summary>전투 종료 조건 체크 (한 팀 전멸)</summary>
        private static bool CheckEndCondition(CombatMatchState state)
        {
            if (state.AliveCountA <= 0 || state.AliveCountB <= 0)
            {
                DetermineWinner(state);
                return true;
            }
            return false;
        }

        /// <summary>유닛의 배열 인덱스 조회 (ref 비교)</summary>
        private static int FindUnitSlotIndex(CombatMatchState state, ref CombatUnit unit)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                if (state.Units[i].CombatId == unit.CombatId)
                    return i;
            }
            return -1;
        }

        /// <summary>승자 결정</summary>
        public static void DetermineWinner(CombatMatchState state)
        {
            if (state.AliveCountA <= 0 && state.AliveCountB <= 0)
            {
                state.Winner = 0xFF; // 무승부
            }
            else if (state.AliveCountB <= 0)
            {
                state.Winner = 0; // TeamA 승리
            }
            else if (state.AliveCountA <= 0)
            {
                state.Winner = 1; // TeamB 승리
            }
            else
            {
                // 타임아웃: 남은 HP 합산 비교
                int hpA = 0, hpB = 0;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    if (!state.Units[i].IsAlive) continue;
                    if (state.Units[i].TeamIndex == 0)
                        hpA += state.Units[i].CurrentHP;
                    else
                        hpB += state.Units[i].CurrentHP;
                }

                if (hpA > hpB) state.Winner = 0;
                else if (hpB > hpA) state.Winner = 1;
                else state.Winner = 0xFF; // HP 동일 → 무승부
            }
        }
    }
}
