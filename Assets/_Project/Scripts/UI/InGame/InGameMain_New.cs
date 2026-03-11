using System.Collections.Generic;
using CookApps.AutoChess;
using CookApps.AutoChess.View;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    public class InGameMain_New : UILayer
    {
        private AutoChessViewRoot _viewRoot;
        private InGameMainParams _inGameParams;
        private int _initialBoardUnitCount;

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
            var stageInfo = SpecDataManager.Instance.GetStageData(inGameParams.StageId);

            // 스테이지 map_size 파싱
            int boardWidth = 7, boardHeight = 4;
            if (stageInfo != null && !string.IsNullOrEmpty(stageInfo.map_size))
            {
                var parts = stageInfo.map_size.Split(',');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out boardWidth);
                    int.TryParse(parts[1], out boardHeight);
                }
            }

            // Config 생성 (모드별 분기 + 보드 크기 덮어쓰기)
            var config = CreateConfigForMode(inGameParams, boardWidth, boardHeight);

            // ViewRoot 생성 → 리소스 로드 → 초기화
            var rootObj = new GameObject("AutoChessRoot");
            _viewRoot = rootObj.AddComponent<AutoChessViewRoot>();
            await _viewRoot.LoadResources(inGameParams.StageId, config.GameMode);
            await _viewRoot.Initialize();

            // 시드 설정 + 시뮬레이션 시작 (config 전달)
            _viewRoot.Runner.RandomSeed = (ulong)inGameParams.RandomSeed;
            _viewRoot.Runner.StartSimulation(config);

            // View 브릿지 초기화
            _viewRoot.ViewBridge.Initialize(localPlayerIndex: 0);

            // 덱 유닛을 벤치에 생성 + 적 데이터 준비
            var world = _viewRoot.Runner.GetWorld();
            CreateUnitsForClassicMode(world);
            SynergySystem.Recalculate(world, 0);
            PrepareEnemyData(world, inGameParams.StageId);

            // BoardInputHandler 생성
            var boardInputObj = new GameObject("BoardInputHandler");
            boardInputObj.transform.SetParent(_viewRoot.transform);
            var boardInput = boardInputObj.AddComponent<BoardInputHandler>();

            // AutoChessUI 동적 생성 (모드별 프리팹)
            AutoChessUIBase autoChessUI = null;
            if (_viewRoot.AutoChessUIPrefab != null)
            {
                var uiObj = Instantiate(_viewRoot.AutoChessUIPrefab, transform);
                autoChessUI = uiObj.GetComponent<AutoChessUIBase>();
            }

            if (autoChessUI != null)
            {
                autoChessUI.Initialize(
                    _viewRoot.ViewBridge,
                    boardInput);
                _viewRoot.ViewBridge.SetAutoChessUI(autoChessUI);
            }

            // 카메라 설정
            var inGameCamera = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera);
            var mode = boardWidth > 5
                ? InGameCamera.CameraPositionMode.LargeSize
                : InGameCamera.CameraPositionMode.Default;
            inGameCamera.SetCameraPositionMode(mode);
            inGameCamera.SetForceCameraRotation(new Vector3(30f, 45f, 0f));

            // BoardInputHandler 초기화
            boardInput.Initialize(
                inGameCamera.MainCamera,
                _viewRoot.ViewBridge,
                _viewRoot.UnitViewManager,
                _viewRoot.TileEffectManager,
                _viewRoot.TargetLineManager,
                screenPos => autoChessUI != null && autoChessUI.IsPointInScrollRect(screenPos));
            _viewRoot.ViewBridge.SetBoardInputHandler(boardInput);

            // 게임 오버 이벤트 구독
            _viewRoot.Runner.OnGameOver += HandleGameOver;

            Debug.Log("[InGameMain_New] AutoChess started.");

            // 캐릭터 비주얼 로딩 완료 대기 (첫 틱에서 SyncBoardUnits → View 생성 → 로딩 완료 이벤트)
            await _viewRoot.ViewBridge.WaitForAllViewsReady();

            // 전투 시작 전 보드 유닛 수 기록 (별 조건 판정용)
            var worldAfterReady = _viewRoot.Runner.GetWorld();
            _initialBoardUnitCount = worldAfterReady.Boards[0].UnitCount;

            SceneTransition.FadeOutAsync();
        }

        protected override void OnPostExit()
        {
            LocalSimulationRunner.SpeedMultiplier = 1f;
            if (_viewRoot != null)
            {
                _viewRoot.Runner.OnGameOver -= HandleGameOver;
                _viewRoot.Cleanup();
            }
            base.OnPostExit();
        }

        // ── 게임 오버 처리 ──

        private void HandleGameOver(GameWorld world)
        {
            // 테스트 모드에서는 결과 처리 스킵
            if (_inGameParams.InGameType == InGameType.TEST) return;

            HandleGameOverAsync(world).Forget();
        }

        private async UniTaskVoid HandleGameOverAsync(GameWorld world)
        {
            // 1. 승패 판정 (PvE: Winner == 0 → 플레이어 승리)
            bool isVictory = world.Matches[0].Winner == 0;

            // 2. 별 조건 계산
            float combatSeconds = world.LastCombatDurationFrames / (float)world.TickRate;
            bool isStarTime = isVictory && combatSeconds <= 30f;

            bool isStarNoDeath = false;
            if (isVictory)
            {
                var matchState = world.CombatMatchStates[0];
                if (matchState != null)
                {
                    isStarNoDeath = matchState.AliveCountA >= _initialBoardUnitCount;
                }
            }

            // 3. MVP 캐릭터 결정 (보드에 배치된 첫 번째 유닛)
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

            // 4. 서버에 결과 전송
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
                {
                    rewards = resp.Rewards;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InGameMain_New] Battle.EndAsync failed: {e.Message}");
            }

            // 5. AutoChess 리소스 정리
            if (_viewRoot != null)
            {
                _viewRoot.Runner.OnGameOver -= HandleGameOver;
                _viewRoot.Cleanup();
                _viewRoot = null;
            }

            // 6. 결과 팝업 표시
            var popupParam = new AutoChessClassicResultPopupParam(
                isVictory, isStarTime, isStarNoDeath,
                mvpCharacter, rewards,
                _inGameParams.StageId, _inGameParams.InGameType);
            SceneUILayerManager.Instance.PushUILayerAsync<AutoChessClassicResultPopup>(popupParam).Forget();

            Debug.Log($"[InGameMain_New] Game Over - Victory={isVictory}, Stars={stars}, CombatTime={combatSeconds:F1}s");
        }

        // ── 모드별 Config 생성 ──

        private GameConfig CreateConfigForMode(InGameMainParams inGameParams, int boardWidth, int boardHeight)
        {
            GameConfig config;
            switch (inGameParams.InGameType)
            {
                case InGameType.STAGE:
                case InGameType.TRIAL:
                case InGameType.TRIAL_BOSS:
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

        // ── 덱 유닛을 벤치에 생성 ──

        private void CreateUnitsForClassicMode(GameWorld world)
        {
            // 서버 덱 데이터에서 캐릭터 목록 가져오기
            using var _ = ListPool<int>.Get(out var characterIds);
            ServerDataManager.Instance.Character.GetAllCharacterIds(characterIds);

            var deckData = ServerDataManager.Instance.Deck.GetDeck(InGameType.STAGE);

            // 덱에 배치된 캐릭터 ID 수집 (중복 배치 방지용)
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

                    byte starLevel = 1; // TODO: 성급 매핑 확정 후 수정
                    int entityId = BoardSystem.CreateUnit(world, 0, charId, starLevel);
                    if (entityId == UnitData.InvalidId)
                        continue;

                    BoardSystem.PlaceUnit(world, 0, entityId, (byte)placement.GridX, (byte)placement.GridY);
                    placedIds.Add(charId);
                }
            }

            // 덱에 없는 캐릭터 → 벤치에 배치
            foreach (var charId in characterIds)
            {
                if (placedIds.Contains(charId))
                    continue;
                BoardSystem.CreateUnit(world, 0, charId, 1);
            }
        }

        // ── PvE 적 데이터 준비 (전투 시작 시 CombatUnit으로 변환됨) ──

        private void PrepareEnemyData(GameWorld world, int stageId)
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

                // 기본 스탯 + 스테이지 배율 적용
                enemy.MaxHP = (int)(spec.stat_hp * monster.multiple_hp);
                enemy.Attack = (int)(spec.stat_atk * monster.multiple_atk);
                enemy.Armor = spec.stat_def;
                enemy.MagicResist = (int)spec.ap_reduce;
                enemy.AttackSpeed = Mathf.Max(1, (int)(spec.atk_speed * 100));
                enemy.AttackRange = spec.atk_range > 0 ? spec.atk_range : 1;
                enemy.MoveSpeed = Mathf.Max(1, (int)(spec.move_speed * 100));
                enemy.MaxMana = 100;
                enemy.TraitFlags = 0;
                enemy.SkillSpecId = (spec.skill_ids != null && spec.skill_ids.Length > 0)
                    ? spec.skill_ids[0] : 0;
            }

            Debug.Log($"[InGameMain_New] PvE enemy data prepared: {world.PvEEnemyCount} from stage {stageId}");
        }
    }
}
