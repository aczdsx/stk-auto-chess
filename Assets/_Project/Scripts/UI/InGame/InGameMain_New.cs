using System.Collections.Generic;
using CookApps.AutoChess;
using CookApps.AutoChess.View;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public class InGameMain_New : UILayer
    {
        private AutoChessViewRoot _viewRoot;
        private InGameMainParams _inGameParams;
        private int _initialBoardUnitCount;
        private CombatFrameRecorder _frameRecorder;
        private ReplayController _replayController;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            if (param is InGameMainParams inGameParams)
            {
                StartAutoChess(inGameParams).Forget();
            }
            else
            {
                Debug.LogError($"[InGameMain_New] Invalid param type: {param?.GetType().Name ?? "null"}");
            }
        }

        private async UniTaskVoid StartAutoChess(InGameMainParams inGameParams)
        {
            _inGameParams = inGameParams;

            // 1. 보드 크기 결정
            int boardWidth = 7, boardHeight = 4;
            ParseBoardSize(inGameParams.StageId, ref boardWidth, ref boardHeight);

            // 2. 테스트 모드: config 로드 + 보드/디버그 오버라이드
            var testResult = await ApplyTestConfig(inGameParams, boardWidth, boardHeight);
            var testConfig = testResult.config;
            boardWidth = testResult.boardWidth;
            boardHeight = testResult.boardHeight;

            // 3. 시뮬레이션 초기화
            var config = CreateConfigForMode(inGameParams, boardWidth, boardHeight);
            _viewRoot = await CreateViewRoot(inGameParams, config);
            _viewRoot.Runner.RandomSeed = (ulong)inGameParams.RandomSeed;
            _viewRoot.Runner.StartSimulation(config);
            _viewRoot.ViewBridge.Initialize(localPlayerIndex: 0);

            // 4. 유닛 생성 + 테스트 모드 후처리 (레코더, 테스트 유닛)
            var world = _viewRoot.Runner.GetWorld();
            SetupUnitsAndTestFeatures(world, inGameParams, testConfig);
            SynergySystem.Recalculate(world, 0);

            // 5. UI / 입력 / 카메라
            SetupUIAndInput(boardWidth);

            // 6. 시작
            _viewRoot.Runner.OnGameOver += HandleGameOver;
            _viewRoot.Runner.OnPhaseChanged += HandlePhaseChanged;
            Debug.Log("[InGameMain_New] AutoChess started.");

            await _viewRoot.ViewBridge.WaitForAllViewsReady();
            _initialBoardUnitCount = _viewRoot.Runner.GetWorld().Boards[0].UnitCount;
            SceneTransition.FadeOutAsync();
        }

        protected override void OnPostExit()
        {
            LocalSimulationRunner.SpeedMultiplier = 1f;
            CleanupTestMode();

            if (_viewRoot != null)
            {
                _viewRoot.Runner.OnGameOver -= HandleGameOver;
                _viewRoot.Runner.OnPhaseChanged -= HandlePhaseChanged;
                _viewRoot.Cleanup();
            }
            base.OnPostExit();
        }

        // ══════════════════════════════════════════
        // 초기화 헬퍼
        // ══════════════════════════════════════════

        private static void ParseBoardSize(int stageId, ref int boardWidth, ref int boardHeight)
        {
            var stageInfo = SpecDataManager.Instance.GetStageData(stageId);
            if (stageInfo != null && !string.IsNullOrEmpty(stageInfo.map_size))
            {
                var parts = stageInfo.map_size.Split(',');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out boardWidth);
                    int.TryParse(parts[1], out boardHeight);
                }
            }
        }

        private static async UniTask<AutoChessViewRoot> CreateViewRoot(InGameMainParams inGameParams, GameConfig config)
        {
            var rootObj = new GameObject("AutoChessRoot");
            var viewRoot = rootObj.AddComponent<AutoChessViewRoot>();
            await viewRoot.LoadResources(inGameParams.StageId, config.GameMode);
            await viewRoot.Initialize();
            return viewRoot;
        }

        private void SetupUIAndInput(int boardWidth)
        {
            var boardInputObj = new GameObject("BoardInputHandler");
            boardInputObj.transform.SetParent(_viewRoot.transform);
            var boardInput = boardInputObj.AddComponent<BoardInputHandler>();

            AutoChessUIBase autoChessUI = null;
            if (_viewRoot.AutoChessUIPrefab != null)
            {
                var uiObj = Instantiate(_viewRoot.AutoChessUIPrefab, transform);
                autoChessUI = uiObj.GetComponent<AutoChessUIBase>();
            }

            if (autoChessUI != null)
            {
                autoChessUI.Initialize(_viewRoot.ViewBridge, boardInput);
                _viewRoot.ViewBridge.SetAutoChessUI(autoChessUI);
            }

            var inGameCamera = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera);
            var mode = boardWidth > 5
                ? InGameCamera.CameraPositionMode.LargeSize
                : InGameCamera.CameraPositionMode.Default;
            inGameCamera.SetCameraPositionMode(mode);
            inGameCamera.SetForceCameraRotation(new Vector3(30f, 45f, 0f));

            boardInput.Initialize(
                inGameCamera.MainCamera,
                _viewRoot.ViewBridge,
                _viewRoot.UnitViewManager,
                _viewRoot.TileEffectManager,
                _viewRoot.TargetLineManager,
                screenPos => autoChessUI != null && autoChessUI.IsPointInScrollRect(screenPos));
            _viewRoot.ViewBridge.SetBoardInputHandler(boardInput);
        }

        private GameConfig CreateConfigForMode(InGameMainParams inGameParams, int boardWidth, int boardHeight)
        {
            GameConfig config;
            switch (inGameParams.InGameType)
            {
                case InGameType.STAGE:
                case InGameType.TRIAL:
                case InGameType.TRIAL_BOSS:
                case InGameType.TEST:
                    config = GameConfig.ClassicBattle();
                    break;
                case InGameType.PVP:
                    config = GameConfig.Competitive();
                    break;
                default:
                    config = GameConfig.ClassicBattle();
                    break;
            }
            config.BoardWidth = boardWidth;
            config.BoardHeight = boardHeight;
            return config;
        }

        // ══════════════════════════════════════════
        // 테스트 모드 (InGameTestConfig 기반)
        // ══════════════════════════════════════════

        /// <summary>
        /// 테스트 모드 전처리: config 로드 → 보드 크기 오버라이드 → 디버그 플래그 적용.
        /// 테스트 모드가 아니면 null 반환.
        /// </summary>
        private static async UniTask<(InGameTestConfig config, int boardWidth, int boardHeight)> ApplyTestConfig(
            InGameMainParams inGameParams, int boardWidth, int boardHeight)
        {
            // 디버그 플래그 초기화 (비테스트 모드)
            DamageSystem.PlayerInvincible = false;
            DamageSystem.EnemyInvincible = false;

            if (inGameParams.InGameType != InGameType.TEST)
                return (null, boardWidth, boardHeight);

            var testConfig = await Addressables.LoadAssetAsync<InGameTestConfig>("Data/InGameTestConfig.asset");
            if (testConfig == null) return (null, boardWidth, boardHeight);

            // 보드 크기 오버라이드
            if (testConfig.Mode == TestMode.Custom)
            {
                boardWidth = testConfig.GridWidth;
                boardHeight = testConfig.GridHeight;
            }

            // 디버그 플래그
            DamageSystem.PlayerInvincible = testConfig.PlayerInvincible;
            DamageSystem.EnemyInvincible = testConfig.EnemyInvincible;

            return (testConfig, boardWidth, boardHeight);
        }

        /// <summary>
        /// 테스트 모드 후처리: 유닛 생성 + 프레임 레코더 + 디버그 UI.
        /// testConfig가 null이면 일반 모드로 동작.
        /// </summary>
        private void SetupUnitsAndTestFeatures(GameWorld world, InGameMainParams inGameParams, InGameTestConfig testConfig)
        {
            bool isTestCustom = testConfig != null && testConfig.Mode == TestMode.Custom;

            // 유닛 생성
            if (isTestCustom)
            {
                CreateUnitsFromTestConfig(world, testConfig);
                CreateUnitsFromDeck(world);
                PrepareEnemiesFromTestConfig(world, testConfig);
            }
            else
            {
                CreateUnitsFromDeck(world);
                PrepareEnemyData(world, inGameParams.StageId);
            }

            // 프레임 레코더 + 디버그 UI
            if (testConfig != null && testConfig.EnableFrameRecorder
                && _viewRoot.Runner is LocalSimulationRunner localRunner)
            {
                _frameRecorder = new CombatFrameRecorder();
                _frameRecorder.StartRecording(testConfig.RecordStartFrame, testConfig.RecordEndFrame);
                localRunner.SetFrameRecorder(_frameRecorder);

                // 리플레이 컨트롤러 생성 + 연결
                _replayController = new ReplayController();
                _replayController.SetWorld(world);
                _replayController.SetViewBridge(_viewRoot.ViewBridge);
                localRunner.SetReplayController(_replayController);

                if (InGameTestDebugUI.Instance == null)
                    InGameTestDebugUI.Create();
                InGameTestDebugUI.Instance.SetFrameDebugger(localRunner, _frameRecorder);
            }
        }

        private void CleanupTestMode()
        {
            if (_frameRecorder != null)
            {
                _frameRecorder.StopRecording();
                InGameTestDebugUI.Instance?.ClearFrameDebugger();
                _frameRecorder = null;
            }
            _replayController = null;
        }

        private static void CreateUnitsFromTestConfig(GameWorld world, InGameTestConfig testConfig)
        {
            foreach (var character in testConfig.PlayerCharacters)
            {
                byte starLevel = (byte)Mathf.Max(1, character.Level);
                int entityId = BoardSystem.CreateUnit(world, 0, character.CharacterId, starLevel);
                if (entityId == UnitData.InvalidId) continue;

                if (character.MultipleAtk > 0f || character.MultipleHp > 0f)
                {
                    int unitIndex = world.FindUnitIndex(entityId);
                    if (unitIndex >= 0)
                    {
                        if (character.MultipleAtk > 0f)
                            world.Units[unitIndex].Attack = (int)(world.Units[unitIndex].Attack * character.MultipleAtk);
                        if (character.MultipleHp > 0f)
                            world.Units[unitIndex].MaxHP = (int)(world.Units[unitIndex].MaxHP * character.MultipleHp);
                    }
                }

                BoardSystem.PlaceUnit(world, 0, entityId, (byte)character.GridX, (byte)character.GridY);
            }
        }

        private static void PrepareEnemiesFromTestConfig(GameWorld world, InGameTestConfig testConfig)
        {
            foreach (var character in testConfig.EnemyCharacters)
            {
                if (world.PvEEnemyCount >= GameWorld.MaxPvEEnemies) break;

                var spec = SpecDataManager.Instance.GetSpecCharacter(character.CharacterId);
                if (spec == null)
                {
                    Debug.LogWarning($"[InGameMain_New] Spec not found for test enemy id={character.CharacterId}");
                    continue;
                }

                ref var enemy = ref world.PvEEnemies[world.PvEEnemyCount++];
                enemy.ChampionSpecId = character.CharacterId;
                enemy.PrefabId = spec.prefab_id;
                enemy.GridCol = (byte)character.GridX;
                enemy.GridRow = (byte)character.GridY;
                enemy.SizeW = 1;
                enemy.SizeH = 1;

                enemy.MaxHP = (int)(spec.stat_hp * character.MultipleHp);
                enemy.Attack = (int)(spec.stat_atk * character.MultipleAtk);
                enemy.Def = spec.stat_def;
                enemy.AdReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ad_reduce);
                enemy.ApReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ap_reduce);

                enemy.AttackSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                enemy.AttackRange = spec.atk_range > 0 ? spec.atk_range : 1;
                enemy.MoveSpeed = Mathf.Max(1, (int)(spec.move_speed * 100));
                enemy.MaxMana = 100;
                enemy.TraitFlags = 0;
                enemy.SkillSpecId = (spec.skill_ids != null && spec.skill_ids.Length > 0)
                    ? spec.skill_ids[0] : 0;
                enemy.AtkPierce = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                enemy.ResPierce = Mathf.Clamp((int)(spec.stat_res_pierce * 100), 0, 100);
                enemy.CritRate = Mathf.Max(0, (int)(spec.crit_rate * 100));
                enemy.CritPower = Mathf.Max(0, (int)(spec.crit_power * 100));
                enemy.HealPower = (int)(spec.heal_power * 100);
                enemy.ImmuneType = (int)spec.immune_type;
            }

            Debug.Log($"[InGameMain_New] Test enemy data prepared: {world.PvEEnemyCount} enemies");
        }

        // ══════════════════════════════════════════
        // 유닛 생성 (공통)
        // ══════════════════════════════════════════

        private static void CreateUnitsFromDeck(GameWorld world)
        {
            using var _ = ListPool<int>.Get(out var characterIds);
            ServerDataManager.Instance.Character.GetAllCharacterIds(characterIds);

            var deckData = ServerDataManager.Instance.Deck.GetDeck(InGameType.STAGE);

            using var __ = HashSetPool<int>.Get(out var placedIds);

            if (deckData != null && deckData.CharacterPlacements.Count > 0)
            {
                for (var i = 0; i < deckData.CharacterPlacements.Count; i++)
                {
                    var placement = deckData.CharacterPlacements[i];
                    int charId = (int)placement.CharacterId;
                    var charData = ServerDataManager.Instance.Character.GetCharacter(placement.CharacterId);
                    if (charData == null)
                        continue;

                    int entityId = BoardSystem.CreateUnit(world, 0, charId, 1); // TODO: 성급 매핑 확정 후 수정
                    if (entityId == UnitData.InvalidId) continue;

                    BoardSystem.PlaceUnit(world, 0, entityId, (byte)placement.GridX, (byte)placement.GridY);
                    placedIds.Add(charId);
                }
            }

            foreach (var charId in characterIds)
            {
                if (placedIds.Contains(charId))
                    continue;

                BoardSystem.CreateUnit(world, 0, charId, 1);
            }
        }

        private static void PrepareEnemyData(GameWorld world, int stageId)
        {
            var stageInfo = SpecDataManager.Instance.GetStageData(stageId);
            if (stageInfo == null)
            {
                Debug.LogWarning($"[InGameMain_New] StageInfo not found for stageId={stageId}");
                return;
            }

            var monsters = SpecDataManager.Instance.GetStageMonsterList(
                stageInfo.chapter_id, stageInfo.stage_number, stageInfo.difficulty_type);
            if (monsters == null || monsters.Count == 0)
            {
                Debug.LogWarning($"[InGameMain_New] StageMonster not found for chapter={stageInfo.chapter_id}, stage={stageInfo.stage_number}");
                return;
            }

            foreach (var monster in monsters)
            {
                if (world.PvEEnemyCount >= GameWorld.MaxPvEEnemies) break;

                var parts = monster.coordinate.Split(',');
                if (parts.Length < 2) continue;
                int.TryParse(parts[0], out int col);
                int.TryParse(parts[1], out int row);

                var spec = SpecDataManager.Instance.GetSpecCharacter(monster.monster_id);
                if (spec == null)
                {
                    Debug.LogWarning($"[InGameMain_New] Spec not found for monster_id={monster.monster_id}");
                    continue;
                }

                ref var enemy = ref world.PvEEnemies[world.PvEEnemyCount++];
                enemy.ChampionSpecId = monster.monster_id;
                enemy.PrefabId = spec.prefab_id;
                enemy.GridCol = (byte)col;
                enemy.GridRow = (byte)row;
                enemy.SizeW = 1;
                enemy.SizeH = 1;

                enemy.MaxHP = (int)(spec.stat_hp * monster.multiple_hp);
                enemy.Attack = (int)(spec.stat_atk * monster.multiple_atk);
                enemy.Def = spec.stat_def;
                enemy.AdReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ad_reduce);
                enemy.ApReduce = AutoChessSpecAdapter.ReduceToIntPercent(spec.ap_reduce);

                enemy.AttackSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                enemy.AttackRange = spec.atk_range > 0 ? spec.atk_range : 1;
                enemy.MoveSpeed = Mathf.Max(1, (int)(spec.move_speed * 100));
                enemy.MaxMana = 100;
                enemy.TraitFlags = 0;
                enemy.SkillSpecId = (spec.skill_ids != null && spec.skill_ids.Length > 0)
                    ? spec.skill_ids[0] : 0;
                enemy.AtkPierce = Mathf.Clamp((int)(spec.stat_atk_pierce * 100), 0, 100);
                enemy.ResPierce = Mathf.Clamp((int)(spec.stat_res_pierce * 100), 0, 100);
                enemy.CritRate = Mathf.Max(0, (int)(spec.crit_rate * 100));
                enemy.CritPower = Mathf.Max(0, (int)(spec.crit_power * 100));
                enemy.HealPower = (int)(spec.heal_power * 100);
                enemy.ImmuneType = (int)spec.immune_type;
            }

            Debug.Log($"[InGameMain_New] PvE enemy data prepared: {world.PvEEnemyCount} from stage {stageId}");
        }

        // ══════════════════════════════════════════
        // 배치 상태 저장
        // ══════════════════════════════════════════

        private void HandlePhaseChanged(GamePhase prevPhase, GamePhase newPhase)
        {
            if (prevPhase == GamePhase.Preparation && newPhase == GamePhase.Combat)
            {
                SaveCurrentDeckAsync().Forget();
            }
        }

        private List<DeckCharacterPlacement> CollectBoardPlacements()
        {
            var world = _viewRoot.Runner.GetWorld();
            var placements = new List<DeckCharacterPlacement>();

            for (int i = 0; i < GameWorld.MaxUnits; i++)
            {
                ref var unit = ref world.Units[i];
                if (!unit.IsValid) continue;
                if (unit.OwnerIndex != 0) continue;
                if (unit.Location != UnitLocation.Board) continue;

                placements.Add(new DeckCharacterPlacement
                {
                    CharacterId = (uint)unit.ChampionSpecId,
                    GridX = unit.BoardCol,
                    GridY = unit.BoardRow
                });
            }

            return placements;
        }

        private async UniTaskVoid SaveCurrentDeckAsync()
        {
            var characterPlacements = CollectBoardPlacements();

            try
            {
                await NetManager.Instance.Deck.SaveAsync(
                    (uint)_inGameParams.InGameType,
                    string.Empty,
                    characterPlacements);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InGameMain_New] Deck.SaveAsync failed: {e.Message}");
            }
        }

        // ══════════════════════════════════════════
        // 게임 오버 처리
        // ══════════════════════════════════════════

        private void HandleGameOver(GameWorld world)
        {
            if (_inGameParams.InGameType == InGameType.TEST) return;
            HandleGameOverAsync(world).Forget();
        }

        private async UniTaskVoid HandleGameOverAsync(GameWorld world)
        {
            bool isVictory = world.Matches[0].Winner == 0;

            float combatSeconds = world.LastCombatDurationFrames / (float)world.TickRate;
            bool isStarTime = isVictory && combatSeconds <= 30f;

            bool isStarNoDeath = false;
            if (isVictory)
            {
                var matchState = world.CombatMatchStates[0];
                if (matchState != null)
                    isStarNoDeath = matchState.AliveCountA >= _initialBoardUnitCount;
            }

            CharacterInfo mvpCharacter = null;
            var boardSlots = world.BoardSlots[0];
            for (int i = 0; i < boardSlots.Length; i++)
            {
                if (boardSlots[i] != UnitData.InvalidId)
                {
                    int unitIndex = world.FindUnitIndex(boardSlots[i]);
                    if (unitIndex >= 0)
                    {
                        mvpCharacter = SpecDataManager.Instance.GetCharacterData(
                            world.Units[unitIndex].ChampionSpecId);
                        break;
                    }
                }
            }

            uint stars = 1;
            if (isStarTime) stars++;
            if (isStarNoDeath) stars++;

            IReadOnlyList<Reward> rewards = null;
            try
            {
                var battleResult = new BattleResult
                {
                    IsVictory = isVictory,
                    Stars = isVictory ? stars : 0,
                    ClearTime = isVictory ? (ulong)(combatSeconds * 1000) : 0
                };
                var resp = await NetManager.Instance.Battle.EndAsync(
                    _inGameParams.SessionId, battleResult);
                if (resp is { IsSuccess: true })
                    rewards = resp.Rewards;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InGameMain_New] Battle.EndAsync failed: {e.Message}");
            }

            // 전투 종료 연출 딜레이 (투사체 등 View 연출이 자연스럽게 마무리되도록)
            await UniTask.Delay(System.TimeSpan.FromSeconds(2.5f));

            if (_viewRoot != null)
            {
                _viewRoot.Runner.OnGameOver -= HandleGameOver;
                _viewRoot.Runner.OnPhaseChanged -= HandlePhaseChanged;
                _viewRoot.Cleanup();
                _viewRoot = null;
            }

            var popupParam = new AutoChessClassicResultPopupParam(
                isVictory, isStarTime, isStarNoDeath,
                mvpCharacter, rewards,
                _inGameParams.StageId, _inGameParams.InGameType);
            SceneUILayerManager.Instance.PushUILayerAsync<AutoChessClassicResultPopup>(popupParam).Forget();

            Debug.Log($"[InGameMain_New] Game Over - Victory={isVictory}, Stars={stars}, CombatTime={combatSeconds:F1}s");
        }
    }
}
