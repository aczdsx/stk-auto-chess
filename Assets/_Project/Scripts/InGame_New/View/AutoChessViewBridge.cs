using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 시뮬레이션 ↔ 뷰 브릿지.
    /// LocalSimulationRunner에 구독하여 매 틱마다 View 매니저들에 상태를 전달.
    /// </summary>
    public class AutoChessViewBridge : MonoBehaviour
    {
        private LocalSimulationRunner _runner;
        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private BoardGridView _boardGridView;
        private AutoChessUIBase _autoChessUI;
        private BoardInputHandler _boardInputHandler;

        public void Setup(
            LocalSimulationRunner runner,
            UnitViewManager unitViewManager,
            CombatViewManager combatViewManager,
            BoardGridView boardGridView)
        {
            _runner = runner;
            _unitViewManager = unitViewManager;
            _combatViewManager = combatViewManager;
            _boardGridView = boardGridView;
        }

        private GamePhase _lastPhase;
        private int _localPlayerIndex;  // 로컬 플레이어 보드 인덱스
        private UniTaskCompletionSource _viewsReadySource;

        // ── 초기화 ──

        public void Initialize(int localPlayerIndex = 0)
        {
            _localPlayerIndex = localPlayerIndex;

            _unitViewManager.Initialize();
            _combatViewManager.Initialize();
            _boardGridView.Initialize();

            _unitViewManager.SetActiveBoard(_localPlayerIndex);

            // View 로딩 완료 이벤트 구독
            _viewsReadySource = new UniTaskCompletionSource();
            _unitViewManager.OnAllBoardViewsReady += () => _viewsReadySource.TrySetResult();

            // 시뮬레이션 이벤트 구독
            _runner.OnTick += HandleTick;
            _runner.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.OnTick -= HandleTick;
                _runner.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        // ── 틱 핸들러 ──

        private void HandleTick(GameWorld world)
        {
            // 이벤트 큐 처리
            ProcessEvents(world);

            // 페이즈별 동기화
            if (world.IsCombatActive)
            {
                SyncCombatViews(world);
            }
            else
            {
                _unitViewManager.SyncBoardUnits(world);
            }

            // UI 갱신 (벤치 + HUD 통합)
            _autoChessUI?.SyncState(world);
        }

        private void HandlePhaseChanged(GamePhase prevPhase, GamePhase newPhase)
        {
            _lastPhase = newPhase;

            switch (newPhase)
            {
                case GamePhase.Preparation:
                    _unitViewManager.OnCombatEnd();
                    _combatViewManager.OnCombatEnd();
                    _boardGridView.OnPreparation();
                    _autoChessUI?.OnPhaseChanged(newPhase);
                    if (_autoChessUI != null) _autoChessUI.gameObject.SetActive(true);
                    _boardInputHandler?.SetEnabled(true);
                    break;

                case GamePhase.Combat:
                    _unitViewManager.OnCombatStart();
                    _combatViewManager.OnCombatStart();
                    _boardGridView.OnCombatStart();
                    _autoChessUI?.OnPhaseChanged(newPhase);
                    if (_autoChessUI != null) _autoChessUI.gameObject.SetActive(false);
                    _boardInputHandler?.SetEnabled(false);
                    break;

                case GamePhase.Result:
                    _autoChessUI?.OnPhaseChanged(newPhase);
                    break;
            }
        }

        // ── 전투 뷰 동기화 ──

        private void SyncCombatViews(GameWorld world)
        {
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
                if (world.Matches[i].IsFinished) continue;

                var matchState = world.CombatMatchStates[i];
                if (matchState == null) continue;

                // 로컬 플레이어가 참여하는 매치를 찾아서 해당 보드에 표시
                int boardIndex = FindBoardIndexForMatch(world, i);
                _unitViewManager.SyncCombatUnits(matchState, boardIndex);
            }
        }

        private int FindBoardIndexForMatch(GameWorld world, int matchIndex)
        {
            var match = world.Matches[matchIndex];
            if (match.PlayerA == _localPlayerIndex)
                return _localPlayerIndex;
            if (match.PlayerB == _localPlayerIndex)
                return _localPlayerIndex;
            return match.PlayerA; // 관전 시 첫 플레이어 보드
        }

        // ── 이벤트 처리 ──

        private void ProcessEvents(GameWorld world)
        {
            var queue = world.EventQueue;
            for (int i = 0; i < queue.Count; i++)
            {
                ref var evt = ref queue.Events[i];
                DispatchEvent(ref evt);
            }
            queue.Clear();
        }

        private void DispatchEvent(ref SimEvent evt)
        {
            switch (evt.Type)
            {
                case SimEventType.UnitAttacked:
                    _combatViewManager.OnUnitAttacked(evt.EntityId, evt.TargetEntityId, evt.Value0, evt.Flag0);
                    break;

                case SimEventType.UnitDamaged:
                    _combatViewManager.OnUnitDamaged(evt.EntityId, evt.Value0, (DamageType)evt.Value1);
                    break;

                case SimEventType.UnitDied:
                    _combatViewManager.OnUnitDied(evt.EntityId);
                    break;

                case SimEventType.UnitCastSkill:
                    _combatViewManager.OnUnitCastSkill(evt.EntityId, evt.Value0);
                    break;

                case SimEventType.ProjectileSpawned:
                    _combatViewManager.OnProjectileSpawned(evt.EntityId, evt.TargetEntityId, evt.ProjType);
                    break;

                case SimEventType.ProjectileExploded:
                    _combatViewManager.OnProjectileExploded(evt.Col, evt.Row, evt.Radius);
                    break;

                case SimEventType.GoldChanged:
                    _autoChessUI?.OnGoldChanged(evt.PlayerIndex, evt.Value0, evt.Value1);
                    break;

                case SimEventType.LevelUp:
                    _autoChessUI?.OnLevelUp(evt.PlayerIndex, evt.Value0);
                    break;

                case SimEventType.PlayerEliminated:
                    _autoChessUI?.OnPlayerEliminated(evt.PlayerIndex, evt.Value0);
                    break;

                case SimEventType.CombatResult:
                    _autoChessUI?.OnCombatResult(evt.PlayerIndex, evt.Value0);
                    break;
            }
        }

        // ── 로딩 대기 ──

        /// <summary>모든 보드 뷰의 캐릭터 비주얼 로딩 완료 대기</summary>
        public UniTask WaitForAllViewsReady()
        {
            return _viewsReadySource.Task;
        }

        // ── UI 연결 ──

        public void SetAutoChessUI(AutoChessUIBase ui) => _autoChessUI = ui;
        public void SetBoardInputHandler(BoardInputHandler handler) => _boardInputHandler = handler;

        // ── 관전 보드 변경 ──

        public void SetSpectateBoard(int boardIndex)
        {
            _localPlayerIndex = boardIndex;
            _unitViewManager.SetActiveBoard(boardIndex);
        }

        // ── 커맨드 전달 (View → Simulation) ──

        public void SendCommand(GameCommand command)
        {
            _runner.EnqueueCommand(command);
        }
    }
}
