namespace CookApps.AutoChess
{
    /// <summary>
    /// 게임 루프 시스템. 페이즈 전환, 타이머 관리, 시스템 실행 순서를 제어.
    /// 매 프레임 Tick()이 호출되어 시뮬레이션을 진행.
    /// </summary>
    public static class GameLoopSystem
    {
        /// <summary>GameConfig로 게임 초기화</summary>
        public static void Initialize(GameWorld world, GameConfig config)
        {
            world.Config = config;
            world.GameMode = config.GameMode;
            world.TickRate = config.TickRate;
            world.CurrentPhase = GamePhase.Preparation;
            world.CurrentStage = 1;
            world.CurrentRound = 1;
            world.FrameCount = 0;
            world.IsGameOver = false;

            // 보드 크기 동기화 (Config 기준 단일 소스)
            BoardHelper.Setup(config.BoardWidth, config.BoardHeight, config.CombatGridHeight);

            // 스킬 팩토리 초기화 (스펙 테이블 → 아키타입 자동 등록)
            SkillFactory.Clear();
            SkillFactory.Initialize(config.TickRate);

            // 범위 기본공격 패턴 레지스트리 초기화
            AreaAttackRegistry.Initialize();

            // 첫 페이즈 타이머 설정
            SetPhaseTimer(world, GamePhase.Preparation);
        }

        /// <summary>
        /// 메인 틱. 매 시뮬레이션 프레임마다 호출.
        /// 순서: 커맨드 처리 → 페이즈 업데이트 → 타이머 진행 → 전환 체크
        /// </summary>
        public static void Tick(GameWorld world, GameCommand[] commands, int commandCount)
        {
            if (world.IsGameOver) return;

            world.FrameCount++;

            // 1. 커맨드 처리
            if (commandCount > 0)
            {
                CommandProcessor.ProcessCommands(world, commands, commandCount);
            }

            // 2. 컷씬 재생 중이면 전투 틱 건너뛰기
            if (world.IsCutscenePlaying)
            {
                TickCutscene(world);
                return;
            }

            // 3. 페이즈별 업데이트
            switch (world.CurrentPhase)
            {
                case GamePhase.Preparation:
                    UpdatePreparation(world);
                    break;
                case GamePhase.Combat:
                    UpdateCombat(world);
                    break;
                case GamePhase.Result:
                    UpdateResult(world);
                    break;
                case GamePhase.SharedDraft:
                    UpdateSharedDraft(world);
                    break;
            }

            // 4. 타이머 진행 & 전환 체크
            world.PhaseElapsedFrames++;
            if (world.PhaseTimerFrames > 0)
            {
                world.PhaseTimerFrames--;
                if (world.PhaseTimerFrames <= 0)
                {
                    OnPhaseTimeout(world);
                }
            }
        }

        // ── 페이즈별 업데이트 ──

        private static void UpdatePreparation(GameWorld world)
        {
            // 모든 플레이어가 Ready면 즉시 전투로 전환
            if (AllPlayersReady(world))
            {
                TransitionToPhase(world, GamePhase.Combat);
            }
        }

        private static void UpdateCombat(GameWorld world)
        {
            if (!world.IsCombatActive) return;

            // 각 매치의 전투 틱 실행
            bool allFinished = true;
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
                if (world.Matches[i].IsFinished) continue;

                var matchState = world.CombatMatchStates[i];
                if (matchState == null) continue;

                bool finished = CombatAISystem.Tick(matchState, ref world.RNG, world.TickRate);
                if (finished)
                {
                    world.Matches[i].IsFinished = true;
                    world.Matches[i].Winner = matchState.Winner;
                }
                else
                {
                    allFinished = false;
                }
            }

            if (allFinished)
            {
                CombatLogger.End();
                CombatLogger.Flush("[CombatLog]");
                TransitionToPhase(world, GamePhase.Result);
            }
        }

        private static void UpdateResult(GameWorld world)
        {
            // 결과 페이즈는 타이머로 자동 전환 (OnPhaseTimeout에서 처리)
        }

        private static void UpdateSharedDraft(GameWorld world)
        {
            // TODO: 공유 드래프트 로직
        }

        // ── 페이즈 전환 ──

        private static void TransitionToPhase(GameWorld world, GamePhase newPhase)
        {
            var prevPhase = world.CurrentPhase;
            OnPhaseExit(world, prevPhase);
            world.CurrentPhase = newPhase;
            world.PhaseElapsedFrames = 0;
            SetPhaseTimer(world, newPhase);
            OnPhaseEnter(world, newPhase);

            world.EventQueue?.PushPhaseChanged(prevPhase, newPhase);
        }

        private static void OnPhaseEnter(GameWorld world, GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Preparation:
                    OnEnterPreparation(world);
                    break;
                case GamePhase.Combat:
                    OnEnterCombat(world);
                    break;
                case GamePhase.Result:
                    OnEnterResult(world);
                    break;
            }
        }

        private static void OnPhaseExit(GameWorld world, GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Preparation:
                    // Ready 플래그 리셋
                    for (int i = 0; i < world.MaxPlayers; i++)
                        world.Players[i].IsReady = false;
                    break;
                case GamePhase.Combat:
                    world.LastCombatDurationFrames = world.PhaseElapsedFrames;
                    world.IsCombatActive = false;
                    for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
                    {
                        if (world.CombatMatchStates[i] != null)
                            SkillSystem.Cleanup(world.CombatMatchStates[i]);
                    }
                    break;
            }
        }

        private static void OnEnterPreparation(GameWorld world)
        {
            // 첫 라운드는 수입/XP 없음
            if (world.CurrentRound > 1)
            {
                // 수입 지급 (기본 수입 + 이자 + 연승/연패 보너스)
                EconomySystem.GrantRoundIncome(world);

                // 자동 XP 지급
                EconomySystem.GrantRoundXP(world);
            }

            // 상점 갱신 (잠금된 상점은 스킵)
            ShopSystem.RefreshAllShops(world);
        }

        private static void OnEnterCombat(GameWorld world)
        {
            world.IsCombatActive = true;
            CombatLogger.Begin();

            // 시너지 재계산 (전투 시작 전 최종 확정)
            for (int p = 0; p < world.Config.PlayerCount; p++)
            {
                if (world.Players[p].IsAlive)
                    SynergySystem.Recalculate(world, (byte)p);
            }

            bool isPvE = world.PvEEnemyCount > 0;

            if (isPvE)
            {
                // PvE: 단일 매치 (player 0 vs 스테이지 적)
                world.Matches[0] = new CombatMatch
                {
                    PlayerA = 0,
                    PlayerB = 0xFF,
                    Winner = 0xFF
                };
                world.Matches[1] = CombatMatch.CreateEmpty();
                world.Matches[1].IsFinished = true;

                world.CombatMatchStates[0] = CombatSetupSystem.SetupPvEMatch(world, 0, 0);

                var matchState = world.CombatMatchStates[0];
                if (matchState != null)
                {
                    SynergySystem.ApplyEffects(world, matchState, 0, 0);
                    SkillSystem.SetupSkills(matchState, world);
                }
            }
            else
            {
                // PvP: 매치메이킹 + 다중 매치
                CombatSetupSystem.AssignMatches(world);

                for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
                {
                    ref var match = ref world.Matches[i];
                    if (match.IsFinished) continue;

                    world.CombatMatchStates[i] = CombatSetupSystem.SetupMatch(
                        world, (byte)i, match.PlayerA, match.PlayerB);

                    var matchState = world.CombatMatchStates[i];
                    if (matchState != null)
                    {
                        SynergySystem.ApplyEffects(world, matchState, match.PlayerA, 0);
                        SynergySystem.ApplyEffects(world, matchState, match.PlayerB, 1);
                        SkillSystem.SetupSkills(matchState, world);
                    }
                }
            }
        }

        private static void OnEnterResult(GameWorld world)
        {
            // 각 매치 결과 처리 (패배 데미지 + 탈락)
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
                if (!world.Matches[i].IsFinished) continue;
                if (world.CombatMatchStates[i] == null) continue;

                PlayerDamageSystem.ProcessMatchResult(world, i);

                // 연승/연패 업데이트 + 승리 보너스 (실제 플레이어만)
                ref var match = ref world.Matches[i];
                if (match.Winner != 0xFF)
                {
                    byte winnerPlayer = match.Winner == 0 ? match.PlayerA : match.PlayerB;
                    byte loserPlayer = match.Winner == 0 ? match.PlayerB : match.PlayerA;

                    if (winnerPlayer < world.MaxPlayers)
                    {
                        PlayerDamageSystem.UpdateStreaks(world, winnerPlayer, true);
                        EconomySystem.GrantVictoryBonus(world, winnerPlayer);
                    }
                    if (loserPlayer < world.MaxPlayers)
                        PlayerDamageSystem.UpdateStreaks(world, loserPlayer, false);
                }
            }

            // 게임 종료 체크
            if (world.AlivePlayerCount <= 1)
            {
                world.IsGameOver = true;
            }
        }

        private static void OnPhaseTimeout(GameWorld world)
        {
            switch (world.CurrentPhase)
            {
                case GamePhase.Preparation:
                    TransitionToPhase(world, GamePhase.Combat);
                    break;
                case GamePhase.Combat:
                    // 전투 타임아웃: 강제 종료 (무승부 처리)
                    ForceEndCombat(world);
                    TransitionToPhase(world, GamePhase.Result);
                    break;
                case GamePhase.Result:
                    AdvanceRound(world);
                    break;
                case GamePhase.SharedDraft:
                    TransitionToPhase(world, GamePhase.Preparation);
                    break;
            }
        }

        // ── 라운드 진행 ──

        private static void AdvanceRound(GameWorld world)
        {
            // 게임 종료 체크
            if (world.AlivePlayerCount <= 1)
            {
                world.IsGameOver = true;
                return;
            }

            // 다음 라운드
            world.CurrentRound++;
            if (world.CurrentRound > world.Config.RoundsPerStage)
            {
                world.CurrentStage++;
                world.CurrentRound = 1;
            }

            // TODO: 공유 드래프트 라운드 판별

            TransitionToPhase(world, GamePhase.Preparation);
        }

        // ── 유틸리티 ──

        private static void SetPhaseTimer(GameWorld world, GamePhase phase)
        {
            var config = world.Config;
            int seconds = phase switch
            {
                GamePhase.Preparation => config.PreparationDuration,
                GamePhase.Combat => config.CombatTimeout,
                GamePhase.Result => config.ResultDuration,
                GamePhase.SharedDraft => config.SharedDraftDuration,
                _ => 0,
            };
            world.PhaseTimerFrames = seconds * world.TickRate;
        }

        private static bool AllPlayersReady(GameWorld world)
        {
            for (int i = 0; i < world.Config.PlayerCount; i++)
            {
                if (world.Players[i].IsAlive && !world.Players[i].IsReady)
                    return false;
            }
            return true;
        }

        private static void ForceEndCombat(GameWorld world)
        {
            CombatLogger.End();
            CombatLogger.Flush("[CombatLog] TIMEOUT");
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
                if (!world.Matches[i].IsFinished)
                {
                    var matchState = world.CombatMatchStates[i];
                    if (matchState != null)
                    {
                        // 타임아웃: HP 합산 비교로 승자 결정
                        CombatAISystem.DetermineWinner(matchState);
                        matchState.IsFinished = true;
                        world.Matches[i].Winner = matchState.Winner;
                    }
                    else
                    {
                        world.Matches[i].Winner = 0xFF;
                    }
                    world.Matches[i].IsFinished = true;
                }
            }
        }

        // ── 컷씬 처리 ──

        private static void TickCutscene(GameWorld world)
        {
            if (world.CutsceneCurrentIndex >= world.CutsceneCount)
            {
                // 모든 컷씬 완료
                world.IsCutscenePlaying = false;
                world.CutsceneCount = 0;
                world.CutsceneCurrentIndex = 0;
                return;
            }

            ref var current = ref world.CutsceneQueue[world.CutsceneCurrentIndex];
            world.CutsceneElapsedFrames++;

            if (world.CutsceneElapsedFrames >= current.DurationFrames)
            {
                // 현재 컷씬 완료, 다음으로
                world.CutsceneCurrentIndex++;
                world.CutsceneElapsedFrames = 0;

                if (world.CutsceneCurrentIndex >= world.CutsceneCount)
                {
                    world.IsCutscenePlaying = false;
                    world.CutsceneCount = 0;
                    world.CutsceneCurrentIndex = 0;
                }
            }
        }

        /// <summary>컷씬 큐에 추가 (전투 중 스킬 컷씬)</summary>
        public static void EnqueueCutscene(GameWorld world, in CutsceneRequest request)
        {
            if (!world.Config.EnableCutscenes) return;
            if (world.CutsceneCount >= GameWorld.MaxCutsceneQueue) return;

            world.CutsceneQueue[world.CutsceneCount] = request;
            world.CutsceneQueue[world.CutsceneCount].IsActive = true;
            world.CutsceneCount++;

            if (!world.IsCutscenePlaying)
            {
                world.IsCutscenePlaying = true;
                world.CutsceneCurrentIndex = 0;
                world.CutsceneElapsedFrames = 0;
            }
        }
    }
}
