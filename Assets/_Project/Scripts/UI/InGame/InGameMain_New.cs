using CookApps.AutoChess;
using CookApps.AutoChess.View;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using UnityEngine;
using UnityEngine.Pool;
using UniTask = Cysharp.Threading.Tasks.UniTask;
using UniTaskExtensions = Cysharp.Threading.Tasks.UniTaskExtensions;

namespace CookApps.AutoBattler
{
    public class InGameMain_New : UILayer
    {
        private AutoChessViewRoot _viewRoot;

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            if (param is InGameMainParams inGameParams)
            {
                UniTaskExtensions.Forget((UniTask)StartAutoChess(inGameParams));
            }
            else
            {
                Debug.LogError($"[InGameMain_New] Invalid param type: {param?.GetType().Name ?? "null"}");
            }
        }

        private async UniTask StartAutoChess(InGameMainParams inGameParams)
        {
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
            _viewRoot.Initialize();

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
                screenPos => autoChessUI != null && autoChessUI.IsPointInScrollRect(screenPos));
            _viewRoot.ViewBridge.SetBoardInputHandler(boardInput);

            Debug.Log("[InGameMain_New] AutoChess started.");

            // 캐릭터 비주얼 로딩 완료 대기 (첫 틱에서 SyncBoardUnits → View 생성 → 로딩 완료 이벤트)
            await _viewRoot.ViewBridge.WaitForAllViewsReady();

            SceneTransition.FadeOutAsync();
        }

        protected override void OnPostExit()
        {
            _viewRoot?.Cleanup();
            base.OnPostExit();
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
