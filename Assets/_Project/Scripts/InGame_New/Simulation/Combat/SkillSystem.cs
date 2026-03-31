namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 오케스트레이션 시스템.
    /// 매치 시작 시 유닛별 스킬 인스턴스를 생성하고,
    /// 마나 풀 시 스킬 시전 → 시전 시간 경과 → 효과 적용 흐름을 관리.
    /// 스킬 설정은 CombatMatchState.SkillConfigs[], 런타임 상태는 SkillStates[]에 저장.
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

                state.SkillConfigs[i] = SkillFactory.Create(unit.SkillSpecId);
                ref var config = ref state.SkillConfigs[i];
                ref var skillState = ref state.SkillStates[i];
                skillState.Reset();

                if (SkillFactory.TryGetParams(unit.SkillSpecId, out var skillParams))
                {
                    SkillFactory.TryGetSpecList(unit.SkillSpecId, out var specList);
                    SkillDispatcher.InitializeFromSpec(ref config, ref skillState, skillParams, specList, world.TickRate);
                    unit.MaxMana = (int)System.Math.Ceiling(skillParams.CooldownSeconds * world.Config.DefaultManaRegenPerSec);
                }
                else
                {
                    config.InitializeBase(new SkillParams
                    {
                        SkillId = unit.SkillSpecId,
                        PowerPercent = 200,
                        DamageType = DamageType.Magical,
                    });
                }
            }

            RegisterCharacterTraits(state);
        }

        /// <summary>캐릭터 고유 패시브 등록 (SOA 방식)</summary>
        private static void RegisterCharacterTraits(CombatMatchState state)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                // 필리아 (215532401): 스킬 킬 → 마나 리셋
                if (unit.ChampionSpecId == 215532401)
                {
                    state.SkillKillManaPassives[i] = new SkillKillManaData
                    {
                        Active = true,
                        MarkerType = (int)SkillMarkerType.PiliaSkillCast,
                    };
                }
            }
        }

        /// <summary>마나 풀 시 스킬 시전 시도. 성공 시 true.</summary>
        public static bool TryCast(CombatMatchState state, ref CombatUnit unit, int unitIndex,
            ref DeterministicRNG rng)
        {
            if (unit.CurrentMana < unit.MaxMana || unit.MaxMana <= 0)
                return false;

            // Silence 디버프: 스킬 시전 불가
            if (StatusEffectSystem.HasSilence(state, unitIndex))
                return false;

            ref var config = ref state.SkillConfigs[unitIndex];
            if (!config.IsInitialized)
            {
                // 스킬이 없으면 마나만 리셋
                unit.CurrentMana = 0;
                return false;
            }

            int targetId = SkillDispatcher.SelectTarget(ref config, state, ref unit);
            if (targetId == CombatUnit.InvalidId)
                return false;

            int castFrames = config.GetCastFrames();
            int actionLockFrames = config.GetActionLockFrames();
            unit.CurrentMana = 0;
            unit.HasPushedManaFull = false;

            if (CombatLogger.Enabled) CombatLogger.LogSkillCast(unit.CombatId, targetId, unit.SkillSpecId, castFrames <= 0);

            // 이벤트 즉시 발행 → View가 SKL 애니메이션 시작
            state.EventQueue?.PushUnitCastSkill(
                unit.CombatId,
                targetId,
                unit.SkillSpecId,
                config.IsChanneling,
                config.HasProjectile);

            ref var skillState = ref state.SkillStates[unitIndex];

            if (castFrames > 0)
            {
                // 키프레임 타이밍까지 대기 후 Execute
                unit.State = CombatState.CastingSkill;
                unit.SkillCastTimer = castFrames;
                unit.ActionLockTimer = actionLockFrames;
                if (config.FaceTarget)
                    unit.CurrentTargetId = targetId;
            }
            else
            {
                // 방향 전환을 Execute 전에 설정 (View가 첫 프레임부터 올바른 방향 사용)
                if (config.FaceTarget)
                    unit.CurrentTargetId = targetId;

                // 즉시 시전
                SkillDispatcher.Execute(ref config, ref skillState, state, ref unit, targetId, ref rng);

                if (config.IsChanneling)
                {
                    unit.State = CombatState.CastingSkill;
                    unit.SkillCastTimer = 0; // 채널링 경과 카운터 초기화
                    unit.ActionLockTimer = actionLockFrames;
                }
                else
                {
                    unit.State = CombatState.CastingSkill;
                    unit.SkillCastTimer = -1; // Execute 완료 후 모션 락 대기
                    unit.ActionLockTimer = actionLockFrames;
                    if (unit.ActionLockTimer <= 0)
                    {
                        unit.State = CombatState.Idle;
                        unit.CurrentTargetId = CombatUnit.InvalidId;
                        unit.SkillCastTimer = 0;
                    }
                }
            }

            return true;
        }

        /// <summary>시전 중 틱 처리. 시전 완료 시 효과 적용.</summary>
        public static void TickCasting(CombatMatchState state, ref CombatUnit unit, int unitIndex,
            ref DeterministicRNG rng)
        {
            if (unit.State != CombatState.CastingSkill) return;

            ref var config = ref state.SkillConfigs[unitIndex];
            ref var skillState = ref state.SkillStates[unitIndex];

            // Execute 완료 후 남은 스킬 모션 락 유지
            if (unit.SkillCastTimer < 0)
            {
                if (unit.ActionLockTimer > 0)
                    return;

                unit.State = CombatState.Idle;
                unit.CurrentTargetId = CombatUnit.InvalidId;
                unit.SkillCastTimer = 0;
                return;
            }

            // 채널링 스킬: 매 틱마다 OnChannelTick 호출
            if (config.IsInitialized && config.IsChanneling)
            {
                unit.SkillCastTimer++; // 채널링 경과 프레임 (첫 효과 발동 시점 판단용)
                bool continuing = SkillDispatcher.OnChannelTick(ref config, ref skillState, state, ref unit, ref rng);
                if (!continuing)
                {
                    if (unit.ActionLockTimer > 0)
                    {
                        unit.SkillCastTimer = -1;
                    }
                    else
                    {
                        unit.State = CombatState.Idle;
                        unit.CurrentTargetId = CombatUnit.InvalidId;
                        unit.SkillCastTimer = 0;
                    }
                }
                return;
            }

            unit.SkillCastTimer--;
            if (unit.SkillCastTimer > 0) return;

            // 시전 완료: 효과 적용
            if (config.IsInitialized)
            {
                int targetId = unit.CurrentTargetId;

                // 타겟 유효성 재검증
                if (!TargetingSystem.IsTargetValid(state, targetId))
                {
                    targetId = SkillDispatcher.SelectTarget(ref config, state, ref unit);
                }

                if (targetId != CombatUnit.InvalidId)
                {
                    if (CombatLogger.Enabled) CombatLogger.LogSkillExecute(unit.CombatId, targetId, unit.SkillSpecId);

                    SkillDispatcher.Execute(ref config, ref skillState, state, ref unit, targetId, ref rng);

                    // Execute 후 채널링이 시작됐으면 CastingSkill 유지 → 다음 틱부터 OnChannelTick 호출
                    if (config.IsChanneling)
                    {
                        return;
                    }
                }
            }

            if (unit.ActionLockTimer > 0)
            {
                unit.SkillCastTimer = -1;
                return;
            }

            unit.State = CombatState.Idle;
            unit.CurrentTargetId = CombatUnit.InvalidId;
            unit.SkillCastTimer = 0;
        }

        /// <summary>매치 종료 시 정리</summary>
        public static void Cleanup(CombatMatchState state)
        {
            if (state?.SkillConfigs == null) return;
            for (int i = 0; i < CombatMatchState.MaxCombatUnits; i++)
            {
                if (state.SkillConfigs[i].IsInitialized)
                {
                    state.SkillStates[i].Reset();
                    state.SkillConfigs[i] = default;
                }
            }
        }
    }
}
