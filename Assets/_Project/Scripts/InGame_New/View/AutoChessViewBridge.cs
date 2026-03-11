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
        private CombatVfxManager _combatVfxManager;
        private BoardGridView _boardGridView;
        private AutoChessUIBase _autoChessUI;
        private BoardInputHandler _boardInputHandler;
        private TutorialSimBridge _tutorialBridge;

        public void Setup(
            ISimulationRunner runner,
            UnitViewManager unitViewManager,
            CombatViewManager combatViewManager,
            BoardGridView boardGridView,
            CombatVfxManager combatVfxManager = null)
        {
            _runner = runner;
            _unitViewManager = unitViewManager;
            _combatViewManager = combatViewManager;
            _boardGridView = boardGridView;
            _combatVfxManager = combatVfxManager;
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
            // 이벤트 큐 먼저 처리 (ATK/ATK2/CRIT 애니메이션 타입 결정 → 상태 동기화에서 사용)
            ProcessEvents(world);

            // 페이즈별 동기화 (상태 → 애니메이션 반영)
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
            _tutorialBridge?.OnPhaseChanged(prevPhase, newPhase);
            _lastPhase = newPhase;

            switch (newPhase)
            {
                case GamePhase.Preparation:
                    _unitViewManager.OnCombatEnd();
                    _combatViewManager.OnCombatEnd();
                    _combatVfxManager?.OnCombatEnd();
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
            _tutorialBridge?.OnSimEvent(ref evt, world);

            switch (evt.Type)
            {
                case SimEventType.UnitAttacked:
                {
                    bool isProjectile = (evt.Value1 & 1) != 0;
                    bool isPreTimed = (evt.Value1 & 2) != 0;
                    _combatViewManager.OnUnitAttacked(evt.EntityId, evt.TargetEntityId, evt.Value0, evt.Flag0, isProjectile, isPreTimed);
                    break;
                }

                case SimEventType.UnitDamaged:
                    _combatViewManager.OnUnitDamaged(evt.EntityId, evt.Value0, (DamageType)evt.Value1, evt.Flag0);
                    break;

                case SimEventType.UnitDied:
                    _combatViewManager.OnUnitDied(evt.EntityId);
                    break;

                case SimEventType.UnitCastSkill:
                {
                    var element = ResolveElementFromCaster(world, evt.EntityId);
                    _combatViewManager.OnUnitCastSkill(evt.EntityId, evt.TargetEntityId, evt.Value0, element, evt.Flag0, evt.Flag1);
                    break;
                }

                case SimEventType.ProjectileSpawned:
                {
                    int champSpecId = ResolveChampSpecId(world, evt.EntityId);
                    int projectileId = evt.Value0;
                    int skillSpecId = evt.Value1;
                    _combatViewManager.OnProjectileSpawned(
                        evt.EntityId, evt.TargetEntityId, evt.ProjType,
                        evt.Col, evt.Row, (sbyte)evt.DirCol, (sbyte)evt.DirRow, champSpecId, projectileId, skillSpecId);
                    break;
                }

                case SimEventType.ProjectileMoved:
                {
                    int projectileId = evt.Value0;
                    _combatViewManager.OnProjectileMoved(projectileId, evt.Col, evt.Row);

                    // 이동한 타일에 원소 타일 이펙트 표시 (width에 따라 3칸 폭)
                    var element = ResolveElementFromCaster(world, evt.EntityId);
                    if (element != SynergyType.NONE && _combatViewManager != null)
                    {
                        var castType = TileEffectManager.SynergyToAreaType(element);
                        int width = evt.Radius > 1 ? evt.Radius : 1;
                        int halfW = width / 2;
                        sbyte dirCol = (sbyte)evt.DirCol;
                        sbyte dirRow = (sbyte)evt.DirRow;

                        for (int offset = -halfW; offset <= halfW; offset++)
                        {
                            int tileCol = evt.Col;
                            int tileRow = evt.Row;

                            if (offset != 0)
                            {
                                // 진행 방향 수직으로 오프셋
                                if (dirCol == 0)
                                    tileCol += offset;
                                else if (dirRow == 0)
                                    tileRow += offset;
                                else
                                {
                                    // 대각선: col + row 양쪽 확장 (중심 제외 2개씩)
                                    int diagCol = evt.Col + offset;
                                    if (BoardHelper.IsValidCombatPosition(diagCol, evt.Row))
                                    {
                                        var posA = BoardWorldHelper.CombatGridToWorld(0, diagCol, evt.Row);
                                        _combatViewManager.ShowTileEffectAt(castType, posA);
                                    }
                                    tileCol = evt.Col;
                                    tileRow = evt.Row + offset;
                                }
                            }

                            if (!BoardHelper.IsValidCombatPosition(tileCol, tileRow)) continue;
                            var worldPos = BoardWorldHelper.CombatGridToWorld(0, tileCol, tileRow);
                            _combatViewManager.ShowTileEffectAt(castType, worldPos);
                        }
                    }
                    break;
                }

                case SimEventType.ProjectileExpired:
                {
                    int projectileId = evt.Value0;
                    _combatViewManager.OnProjectileExpired(projectileId);
                    break;
                }

                case SimEventType.ProjectileExploded:
                {
                    var element = ResolveElementFromSkill(evt.Value0);
                    _combatViewManager.OnProjectileExploded(evt.Col, evt.Row, evt.Radius, element);
                    break;
                }

                case SimEventType.SkillPhaseVfx:
                {
                    int casterId = evt.EntityId;
                    int skillSpecId = evt.Value0;
                    byte vfxIndex = (byte)evt.Value1;
                    sbyte dirCol = (sbyte)evt.DirCol;
                    sbyte dirRow = (sbyte)evt.DirRow;
                    _combatViewManager.OnSkillPhaseVfx(casterId, skillSpecId, vfxIndex, dirCol, dirRow);
                    break;
                }

                case SimEventType.SkillRectAreaEffect:
                {
                    var element = ResolveElementFromCaster(world, evt.EntityId);
                    sbyte dirCol = (sbyte)evt.DirCol;
                    sbyte dirRow = (sbyte)evt.DirRow;
                    _combatViewManager.OnSkillRectAreaEffect(evt.Col, evt.Row, dirCol, dirRow, element);
                    break;
                }

                case SimEventType.SkillAreaEffect:
                {
                    var element = ResolveElementFromCaster(world, evt.EntityId);
                    bool isBox = evt.Value1 != 0;
                    _combatViewManager.OnSkillAreaEffect(evt.Col, evt.Row, evt.Radius, element, evt.Flag0, isBox);
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

                case SimEventType.UnitMissed:
                    _combatViewManager.OnUnitMissed(evt.EntityId, evt.TargetEntityId);
                    break;

                case SimEventType.UnitHealed:
                    _combatViewManager.OnUnitHealed(evt.EntityId, evt.Value0);
                    break;

                case SimEventType.SynergyUpdated:
                    _autoChessUI?.OnSynergyUpdated(world);
                    break;

                case SimEventType.StatusEffectAdded:
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                case SimEventType.StatusEffectRemoved:
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                case SimEventType.CCAdded:
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;

                case SimEventType.CCRemoved:
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0);
                    break;
            }
        }

        // ── 원소 타입 조회 ──

        /// <summary>시전자 entityId → 캐릭터 원소 타입 (CombatId 또는 SourceEntityId 매칭)</summary>
        private SynergyType ResolveElementFromCaster(GameWorld world, int casterId)
        {
            // CombatMatchState에서 유닛 찾기
            for (int m = 0; m < GameWorld.MaxCombatMatches; m++)
            {
                var matchState = world.CombatMatchStates[m];
                if (matchState == null) continue;
                for (int u = 0; u < matchState.UnitCount; u++)
                {
                    if (matchState.Units[u].CombatId == casterId ||
                        matchState.Units[u].SourceEntityId == casterId)
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
        public void SetTutorialBridge(TutorialSimBridge bridge) => _tutorialBridge = bridge;
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
