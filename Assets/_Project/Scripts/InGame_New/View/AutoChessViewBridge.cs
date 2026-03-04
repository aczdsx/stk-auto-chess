using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 시뮬레이션 ↔ 뷰 브릿지.
    /// ISimulationRunner에 구독하여 매 틱마다 View 매니저들에 상태를 전달.
    /// </summary>
    public class AutoChessViewBridge : MonoBehaviour
    {
        private ISimulationRunner _runner;
        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private BoardGridView _boardGridView;
        private AutoChessUIBase _autoChessUI;
        private BoardInputHandler _boardInputHandler;

        public void Setup(
            ISimulationRunner runner,
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
                {
                    // 전투→결과 전환 시 마지막 상태 동기화 (마지막 유닛 사망 애니메이션 + HP 갱신)
                    var world = _runner.GetWorld();
                    for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
                    {
                        var matchState = world.CombatMatchStates[i];
                        if (matchState == null) continue;
                        int boardIndex = FindBoardIndexForMatch(world, i);
                        _unitViewManager.SyncCombatUnits(matchState, boardIndex);
                    }
                    // 살아있는 유닛 강제 Idle (공격 애니메이션 정지 방지)
                    _unitViewManager.ForceAllCombatViewsIdle();
                    _autoChessUI?.OnPhaseChanged(newPhase);
                    break;
                }
            }
        }

        // ── 전투 뷰 동기화 ──

        private void SyncCombatViews(GameWorld world)
        {
            for (int i = 0; i < GameWorld.MaxCombatMatches; i++)
            {
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
                DispatchEvent(ref evt, world);
            }
            queue.Clear();
        }

        private void DispatchEvent(ref SimEvent evt, GameWorld world)
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
                {
                    var element = ResolveElementFromCaster(world, evt.EntityId);
                    _combatViewManager.OnUnitCastSkill(evt.EntityId, evt.Value0, element);
                    break;
                }

                case SimEventType.ProjectileSpawned:
                {
                    int champSpecId = ResolveChampSpecId(world, evt.EntityId);
                    _combatViewManager.OnProjectileSpawned(
                        evt.EntityId, evt.TargetEntityId, evt.ProjType,
                        evt.Col, evt.Row, (sbyte)evt.DirCol, (sbyte)evt.DirRow, champSpecId);
                    break;
                }

                case SimEventType.ProjectileExploded:
                {
                    var element = ResolveElementFromSkill(evt.Value0);
                    _combatViewManager.OnProjectileExploded(evt.Col, evt.Row, evt.Radius, element);
                    break;
                }

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

                case SimEventType.SynergyUpdated:
                    _autoChessUI?.OnSynergyUpdated(world);
                    break;
            }
        }

        // ── 원소 타입 조회 ──

        /// <summary>시전자 entityId → 캐릭터 원소 타입</summary>
        private SynergyType ResolveElementFromCaster(GameWorld world, int casterId)
        {
            // CombatMatchState에서 유닛 찾기
            for (int m = 0; m < GameWorld.MaxCombatMatches; m++)
            {
                var matchState = world.CombatMatchStates[m];
                if (matchState == null) continue;
                for (int u = 0; u < matchState.UnitCount; u++)
                {
                    if (matchState.Units[u].CombatId == casterId)
                    {
                        int champId = matchState.Units[u].ChampionSpecId;
                        return GetElementFromCharacterId(champId);
                    }
                }
            }
            return SynergyType.NONE;
        }

        /// <summary>skillSpecId → 원소 타입 (스킬 → 캐릭터 역추적)</summary>
        private SynergyType ResolveElementFromSkill(int skillSpecId)
        {
            if (skillSpecId <= 0) return SynergyType.NONE;

            // SkillActive → character_id 역추적이 복잡하므로
            // Pool에서 SkillId로 캐릭터 찾기
            var world = _runner.GetWorld();
            if (world?.Pool == null) return SynergyType.NONE;

            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].SkillId == skillSpecId)
                {
                    int champId = world.Pool.Specs[i].ChampionId;
                    return GetElementFromCharacterId(champId);
                }
            }
            return SynergyType.NONE;
        }

        /// <summary>combatId → ChampionSpecId</summary>
        private int ResolveChampSpecId(GameWorld world, int combatId)
        {
            for (int m = 0; m < GameWorld.MaxCombatMatches; m++)
            {
                var matchState = world.CombatMatchStates[m];
                if (matchState == null) continue;
                for (int u = 0; u < matchState.UnitCount; u++)
                {
                    if (matchState.Units[u].CombatId == combatId)
                        return matchState.Units[u].ChampionSpecId;
                }
            }
            return 0;
        }

        private static SynergyType GetElementFromCharacterId(int champId)
        {
            var charInfo = SpecDataManager.Instance.GetCharacterData(champId);
            return charInfo?.character_element_type ?? SynergyType.NONE;
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
        public GameWorld GetWorld() => _runner.GetWorld();

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

        // ── 게임 종료 ──

        public void ExitGame()
        {
            _runner?.StopSimulation();
        }
    }
}
