using System;
using CookApps.AutoBattler;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// InGame_New 튜토리얼 브릿지.
    /// 시뮬레이션 이벤트를 감지하여 TutorialManager 트리거를 발동하고,
    /// 필요 시 LocalSimulationRunner를 일시정지/재개.
    /// </summary>
    public class TutorialSimBridge : IDisposable
    {
        public static TutorialSimBridge Instance { get; private set; }

        private readonly LocalSimulationRunner _localRunner;
        private BoardGridView _boardGridView;
        private readonly TutorialNewCombatStartHandler _combatStartHandler;
        private readonly TutorialNewSkillReadyHandler _skillReadyHandler;
        private readonly TutorialNewCombatEndHandler _combatEndHandler;
        private readonly TutorialNewPhaseHandler _phaseHandler;

        public TutorialSimBridge(LocalSimulationRunner localRunner)
        {
            _localRunner = localRunner;
            Instance = this;

            _combatStartHandler = new TutorialNewCombatStartHandler(localRunner);
            _skillReadyHandler = new TutorialNewSkillReadyHandler(localRunner);
            _combatEndHandler = new TutorialNewCombatEndHandler(localRunner);
            _phaseHandler = new TutorialNewPhaseHandler(localRunner);

            // Strategy 오버라이드 등록
            TutorialController.SetStrategyOverride(TutorialActionType.SPAWN_ENEMY, new TutorialActionSpawnEnemyNew());
            TutorialController.SetStrategyOverride(TutorialActionType.CHARACTER_PLACEMENT_UI, new TutorialActionCharacterPlacementUINew());
        }

        public void OnPhaseChanged(GamePhase prev, GamePhase current)
        {
            switch (current)
            {
                case GamePhase.Preparation:
                    _phaseHandler.TryHandleTutorial(TutorialTriggerType.PREPARATION_START);
                    break;
                case GamePhase.Combat:
                    _combatStartHandler.TryHandleTutorial();
                    break;
            }
        }

        public void OnSimEvent(ref SimEvent evt, GameWorld world)
        {
            switch (evt.Type)
            {
                case SimEventType.ManaFull:
                    _skillReadyHandler.TryHandleTutorial(evt.EntityId);
                    break;
                case SimEventType.CombatResult:
                    // ENEMY_DEAD_ALL: 적 전멸(플레이어 승리) 시 먼저 처리, 미처리 시 COMBAT_END
                    bool enemyDeadAllHandled = evt.Value0 == 0
                        && _phaseHandler.TryHandleTutorial(TutorialTriggerType.ENEMY_DEAD_ALL);
                    if (!enemyDeadAllHandled)
                        _combatEndHandler.TryHandleTutorial();
                    break;
                case SimEventType.UnitPurchased:
                    _phaseHandler.TryHandleTutorial(TutorialTriggerType.SHOP_PURCHASE);
                    break;
                case SimEventType.UnitMoved:
                    _phaseHandler.TryHandleTutorial(TutorialTriggerType.UNIT_PLACED);
                    break;
                case SimEventType.SynergyUpdated:
                    _phaseHandler.TryHandleTutorial(TutorialTriggerType.SYNERGY_ACTIVATED);
                    break;
                case SimEventType.UnitDied:
                    // CHARACTER_DEAD 튜토리얼 완료 후 보류된 SKILL_READY 처리
                    _skillReadyHandler.TryProcessDeferred();
                    break;
            }
        }

        public void SetBoardGridView(BoardGridView boardGridView)
        {
            _boardGridView = boardGridView;
        }

        /// <summary>타일 월드 좌표 반환. tileIndex를 col,row로 변환 후 BoardGridView에서 조회.</summary>
        public bool TryGetTileWorldPosition(int tileIndex, out UnityEngine.Vector3 position)
        {
            position = default;
            if (_boardGridView == null) return false;

            BoardHelper.FromIndex(tileIndex, out int col, out int row);
            var tile = _boardGridView.GetTile(col, row);
            if (tile == null) return false;

            position = tile.Position;
            return true;
        }

        public void EnqueueSpawnCommand(int monsterSpecId, int col, int row)
        {
            _localRunner.EnqueueCommand(GameCommand.SpawnTutorialEnemy(0, monsterSpecId, col, row));
        }

        /// <summary>
        /// 벤치에 유닛 직접 생성. 튜토리얼 보상 캐릭터를 시뮬레이션에 추가할 때 사용.
        /// 이미 동일 champSpecId 유닛이 벤치/보드에 있으면 생성하지 않음.
        /// </summary>
        public int SpawnBenchUnit(int championSpecId, byte starLevel = 1)
        {
            var world = _localRunner.GetWorld();
            if (world == null) return UnitData.InvalidId;

            // 중복 검사: 벤치에 같은 champSpecId 유닛이 이미 있으면 스킵
            var benchSlots = world.BenchSlots[0];
            for (int i = 0; i < benchSlots.Length; i++)
            {
                if (benchSlots[i] == UnitData.InvalidId) continue;
                int unitIdx = world.FindUnitIndex(benchSlots[i]);
                if (unitIdx >= 0 && world.Units[unitIdx].ChampionSpecId == championSpecId)
                    return benchSlots[i];
            }

            // 보드에도 확인
            var boardSlots = world.BoardSlots[0];
            for (int i = 0; i < boardSlots.Length; i++)
            {
                if (boardSlots[i] == UnitData.InvalidId) continue;
                int unitIdx = world.FindUnitIndex(boardSlots[i]);
                if (unitIdx >= 0 && world.Units[unitIdx].ChampionSpecId == championSpecId)
                    return boardSlots[i];
            }

            return BoardSystem.CreateUnit(world, 0, championSpecId, starLevel);
        }

        public void Dispose()
        {
            _combatStartHandler.Dispose();
            _skillReadyHandler.Dispose();
            _combatEndHandler.Dispose();
            _phaseHandler.Dispose();

            TutorialController.ClearStrategyOverrides();

            if (Instance == this) Instance = null;
        }
    }
}
