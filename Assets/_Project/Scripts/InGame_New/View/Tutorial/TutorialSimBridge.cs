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

        public void EnqueueSpawnCommand(int monsterSpecId, int col, int row)
        {
            _localRunner.EnqueueCommand(GameCommand.SpawnTutorialEnemy(0, monsterSpecId, col, row));
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
