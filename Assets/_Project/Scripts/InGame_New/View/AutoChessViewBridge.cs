using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 시뮬레이션 ↔ 뷰 브릿지.
    /// ISimulationRunner에 구독하여 매 틱마다 View 매니저들에 상태를 전달.
    /// </summary>
    public partial class AutoChessViewBridge : MonoBehaviour
    {
        private ISimulationRunner _runner;
        private UnitViewManager _unitViewManager;
        private CombatViewManager _combatViewManager;
        private CombatVfxManager _combatVfxManager;
        private BuffIconTracker _buffIconTracker;
        private BoardGridView _boardGridView;
        private AutoChessUIBase _autoChessUI;
        private BoardInputHandler _boardInputHandler;
        private TutorialSimBridge _tutorialBridge;
        private SynergyVfxConfigSO _synergyVfxConfig;
        private byte[] _prevSynergyTiers;

        public void Setup(
            ISimulationRunner runner,
            UnitViewManager unitViewManager,
            CombatViewManager combatViewManager,
            BoardGridView boardGridView,
            CombatVfxManager combatVfxManager = null,
            BuffIconTracker buffIconTracker = null)
        {
            _runner = runner;
            _unitViewManager = unitViewManager;
            _combatViewManager = combatViewManager;
            _boardGridView = boardGridView;
            _combatVfxManager = combatVfxManager;
            _buffIconTracker = buffIconTracker;
        }

        private GamePhase _lastPhase;
        private int _localPlayerIndex;  // 로컬 플레이어 보드 인덱스
        private UniTaskCompletionSource _viewsReadySource;

        // ── 초기화 ──

        public void Initialize(int localPlayerIndex = 0)
        {
            _localPlayerIndex = localPlayerIndex;

            _unitViewManager.Initialize();
            var world = _runner.GetWorld();
            _combatViewManager.Initialize(world != null ? world.TickRate : 60);
            _boardGridView.Initialize(world.BoardWidth, world.Config.CombatGridHeight);

            _unitViewManager.SetActiveBoard(_localPlayerIndex);

            // View 로딩 완료 이벤트 구독
            _viewsReadySource = new UniTaskCompletionSource();
            _unitViewManager.OnAllBoardViewsReady += () => _viewsReadySource.TrySetResult();

            // 시너지 VFX 스냅샷 초기화 (모드 진입 시 기존 시너지에 거짓 VFX 방지)
            _prevSynergyTiers = new byte[PlayerSynergy.MaxTraits];
            RefreshSynergySnapshot();

            // 시뮬레이션 이벤트 구독
            _runner.OnTick += HandleTick;
            _runner.OnPhaseChanged += HandlePhaseChanged;
            _unitViewManager.OnCombatViewCreated += HandleCombatViewCreated;

            // 디버거 pause → VFX 업데이트 중지
            if (_runner is LocalSimulationRunner localRunner)
                localRunner.OnDebuggerPauseChanged += OnDebuggerPauseChanged;
        }

        private void OnDestroy()
        {
            if (_runner != null)
            {
                _runner.OnTick -= HandleTick;
                _runner.OnPhaseChanged -= HandlePhaseChanged;
                _unitViewManager.OnCombatViewCreated -= HandleCombatViewCreated;
                if (_runner is LocalSimulationRunner localRunner)
                    localRunner.OnDebuggerPauseChanged -= OnDebuggerPauseChanged;
            }
            if (_boardInputHandler != null)
            {
                _boardInputHandler.OnObjectDragStarted -= HandleSupernovaDragStart;
                _boardInputHandler.OnObjectDragEnded -= HandleSupernovaDragEnd;
            }
            HandleSupernovaDragEnd(); // 잔여 VFX 정리
        }

        private void OnDebuggerPauseChanged(bool paused)
        {
            _combatViewManager?.SetPausedByDebugger(paused);
        }

        // ── 틱 핸들러 ──

        private void HandleTick(GameWorld world)
        {
            if (world.IsCombatActive)
            {
                // 전투 중: 이벤트 먼저 처리 (ATK/ATK2/CRIT 애니메이션 타입 결정 → 상태 동기화에서 사용)
                ProcessEvents(world);
                SyncCombatViews(world);
            }
            else
            {
                // 이벤트 먼저 처리 (마지막 공격 데미지 텍스트 등 전투 뷰가 필요한 이벤트 소화)
                ProcessEvents(world);
                // Result 페이즈에서는 보드 동기화 스킵 (전투 뷰 유지, 초기 위치 재생성 방지)
                if (world.CurrentPhase != GamePhase.Result)
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
                    _buffIconTracker?.OnCombatEnd();
                    ClearBattleStartVfx();
                    _boardGridView.OnPreparation();
                    _autoChessUI?.OnPhaseChanged(newPhase);
                    _autoChessUI?.PlayAnimation("SetEntry");
                    _boardInputHandler?.SetEnabled(true);
                    break;

                case GamePhase.Combat:
                    _unitViewManager.OnCombatStart();
                    _combatViewManager.OnCombatStart();
                    _boardGridView.OnCombatStart();
                    _autoChessUI?.OnPhaseChanged(newPhase);
                    _autoChessUI?.PlayAnimation("SetBattleEntry");
                    _boardInputHandler?.SetEnabled(false);
                    // 전투 시작 연출
                    PlayBattleStartSequenceAsync().Forget();
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
                    if (evt.Value0 >= 0) _combatVfxManager?.OnUnitDied(evt.Value0);
                    _combatViewManager.OnUnitDied(evt.EntityId);
                    _autoChessUI?.OnUnitDied(evt.EntityId, evt.TargetEntityId, world);
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
                        evt.Col, evt.Row, (sbyte)evt.DirCol, (sbyte)evt.DirRow, champSpecId, projectileId, skillSpecId,
                        evt.SkillVfxIndex, evt.MoveInterval, evt.Flag0, evt.ArrivalVfxIndex);
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
                    int targetId = evt.TargetEntityId;
                    int skillSpecId = evt.Value0;
                    byte vfxIndex = (byte)evt.Value1;
                    sbyte dirCol = (sbyte)evt.DirCol;
                    sbyte dirRow = (sbyte)evt.DirRow;
                    bool useGridPos = evt.Flag0;

                    // 미사 봉인: 타겟 캐릭터 숨김 (VFX는 useGridPos 경로로 공용 처리)
                    if (skillSpecId == 217323201 && targetId > 0)
                    {
                        var targetView = _unitViewManager?.FindCombatView(targetId);
                        targetView?.SetModelVisible(false);
                    }

                    _combatViewManager.OnSkillPhaseVfx(casterId, skillSpecId, vfxIndex, dirCol, dirRow,
                        targetId, useGridPos ? evt.Col : (byte)0, useGridPos ? evt.Row : (byte)0, useGridPos);
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
                    HandleSynergyVfx(world, evt.PlayerIndex);
                    break;

                case SimEventType.StatusEffectAdded:
                {
                    var vfxType = SimEventHelper.DecodeVfxType(evt.Value0);
                    var statType = SimEventHelper.DecodeStatType(evt.Value0);
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, vfxType, statType);
                    _buffIconTracker?.OnEffectAdded(evt.EntityId, vfxType, evt.Value1, statType);
                    break;
                }

                case SimEventType.StatusEffectRemoved:
                {
                    var vfxType = SimEventHelper.DecodeVfxType(evt.Value0);
                    var statType = SimEventHelper.DecodeStatType(evt.Value0);
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, vfxType, statType);
                    _buffIconTracker?.OnEffectRemoved(evt.EntityId, vfxType, statType);
                    break;
                }

                case SimEventType.CCAdded:
                    _combatVfxManager?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0, default);
                    _buffIconTracker?.OnEffectAdded(evt.EntityId, (CombatVfxType)evt.Value0, evt.Value1, default);
                    break;

                case SimEventType.CCRemoved:
                    _combatVfxManager?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0, default);
                    _buffIconTracker?.OnEffectRemoved(evt.EntityId, (CombatVfxType)evt.Value0, default);
                    // 미사 봉인 해제: 숨겨진 캐릭터 복원
                    {
                        var unitView = _unitViewManager?.FindCombatView(evt.EntityId);
                        if (unitView != null && unitView.IsModelHidden)
                            unitView.SetModelVisible(true);
                    }
                    break;

                case SimEventType.SkillMarkerAdded:
                    _buffIconTracker?.OnSkillMarkerAdded(evt.EntityId, evt.Value0, evt.Value1);
                    break;

                case SimEventType.SkillMarkerRemoved:
                    _buffIconTracker?.OnSkillMarkerRemoved(evt.EntityId, evt.Value0, evt.Value1);
                    break;

                case SimEventType.SupernovaObjectEvent:
                    if (evt.PlayerIndex == _localPlayerIndex)
                        HandleSupernovaObjectEvent(evt);
                    break;

                case SimEventType.CameraShake:
                {
                    float duration = evt.Value0 / 1000f;
                    float magnitude = evt.Value1 / 100f;
                    var cam = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera);
                    cam?.ShakeCamera(duration, magnitude);
                    break;
                }
            }
        }

        // ── 시너지 달성 VFX ──

        public void SetSynergyVfxConfig(SynergyVfxConfigSO config) => _synergyVfxConfig = config;

        /// <summary>현재 _localPlayerIndex 기준으로 시너지 티어 스냅샷 갱신 (관전 전환/초기화 시)</summary>
        private void RefreshSynergySnapshot()
        {
            if (_prevSynergyTiers == null) return;
            var world = _runner.GetWorld();
            if (world == null) return;
            var synergy = world.Synergies[_localPlayerIndex];
            for (int i = 0; i < PlayerSynergy.MaxTraits; i++)
                _prevSynergyTiers[i] = synergy.GetTraitTier(i);
        }

        private void HandleSynergyVfx(GameWorld world, byte playerIndex)
        {
            if (_synergyVfxConfig == null || _prevSynergyTiers == null) return;
            if (playerIndex != _localPlayerIndex) return;

            var synergy = world.Synergies[playerIndex];

            for (int t = 0; t < world.SynergySpecCount; t++)
            {
                ref var spec = ref world.SynergySpecs[t];
                if (!spec.IsValid) continue;

                int traitId = spec.TraitId;
                byte oldTier = _prevSynergyTiers[traitId];
                byte newTier = synergy.GetTraitTier(traitId);

                if (newTier > oldTier)
                    SpawnSynergyAchieveVfx(world, playerIndex, traitId);
            }

            // 스냅샷 갱신
            for (int i = 0; i < PlayerSynergy.MaxTraits; i++)
                _prevSynergyTiers[i] = synergy.GetTraitTier(i);
        }

        private void SpawnSynergyAchieveVfx(GameWorld world, byte playerIndex, int traitId)
        {
            if (!_synergyVfxConfig.TryGetEntry((SynergyType)traitId, out var entry)) return;
            if (entry.AchieveVfx == null || !entry.AchieveVfx.RuntimeKeyIsValid()) return;

            int traitBit = 1 << traitId;
            var boardSlots = world.BoardSlots[playerIndex];

            for (int i = 0; i < world.BoardSize; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId) continue;

                ref var unit = ref world.GetUnit(entityId);
                if ((unit.TraitFlags & traitBit) == 0) continue;

                var unitView = _unitViewManager.FindBoardView(entityId);
                if (unitView == null)
                {
                    // 뷰가 아직 생성되지 않음 (SyncBoardUnits 전) — 다음 프레임에 재시도
                    SpawnSynergyAchieveVfxDeferred(entityId, entry).Forget();
                    continue;
                }

                SpawnSynergyOneShotAsync(unitView, entry).Forget();
            }
        }

        private async UniTaskVoid SpawnSynergyAchieveVfxDeferred(int entityId, SynergyVfxConfigSO.SynergyVfxEntry entry)
        {
            await UniTask.Yield(destroyCancellationToken);
            var unitView = _unitViewManager.FindBoardView(entityId);
            if (unitView == null) return;
            SpawnSynergyOneShotAsync(unitView, entry).Forget();
        }

        private async UniTaskVoid PlayBattleStartSequenceAsync()
        {
            var world = _runner.GetWorld();
            await PlayBattleStartCutsceneAsync();

            // 컷씬 후 시뮬레이션 1초간 일시정지 유지 (VFX 연출 시간 확보)
            if (world != null && world.Config.EnableCutscenes)
            {
                GameLoopSystem.EnqueueCutscene(world, new CutsceneRequest
                {
                    DurationFrames = 1 * world.TickRate
                });
            }

            PlayPrepBattleStartVfx();
        }

        private async UniTask PlayBattleStartCutsceneAsync()
        {
            var world = _runner.GetWorld();

            // EnableCutscenes가 false면 연출 없이 바로 진행
            if (!world.Config.EnableCutscenes) return;

            // 시뮬레이션 틱 일시정지 (컷씬 큐에 긴 duration 등록)
            GameLoopSystem.EnqueueCutscene(world, new CutsceneRequest
            {
                DurationFrames = 600 * world.TickRate // 안전 타임아웃 10분
            });

            try
            {
                // 연출 Push & 완료 대기
                var cutsceneUI = await SceneUILayerManager.Instance
                    .PushUILayerAsync<BattleStartCutsceneUI>();
                if (cutsceneUI != null)
                {
                    await cutsceneUI.WaitForAnimationCompleteAsync();
                }
            }
            finally
            {
                // 컷씬 종료 → 시뮬레이션 재개 (예외 시에도 보장)
                world.IsCutscenePlaying = false;
                world.CutsceneCount = 0;
                world.CutsceneCurrentIndex = 0;
            }
        }

        private async UniTaskVoid SpawnSynergyOneShotAsync(UnitView unitView, SynergyVfxConfigSO.SynergyVfxEntry entry)
        {
            var ct = destroyCancellationToken;
            await UniTask.WaitUntil(() => unitView == null || unitView.IsReady, cancellationToken: ct);
            if (unitView == null) return;

            var posTransform = unitView.GetSkillPositionTransform(entry.AchievePosition);

            var handle = Addressables.InstantiateAsync(entry.AchieveVfx);
            var go = await handle;
            if (go == null || !handle.IsValid()) return;

            var pos = posTransform != null ? posTransform.position : unitView.transform.position;
            go.transform.position = pos;

            var canceled = await UniTask.Delay(3000, cancellationToken: ct).SuppressCancellationThrow();
            if (handle.IsValid()) Addressables.ReleaseInstance(handle);
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
        public void SetBoardInputHandler(BoardInputHandler handler)
        {
            if (_boardInputHandler != null)
            {
                _boardInputHandler.OnObjectDragStarted -= HandleSupernovaDragStart;
                _boardInputHandler.OnObjectDragEnded -= HandleSupernovaDragEnd;
            }
            _boardInputHandler = handler;
            _boardInputHandler?.SetBoardObjectFinder(FindBoardObjectAt);
            if (_boardInputHandler != null)
            {
                _boardInputHandler.OnObjectDragStarted += HandleSupernovaDragStart;
                _boardInputHandler.OnObjectDragEnded += HandleSupernovaDragEnd;
            }
        }
        public void SetTutorialBridge(TutorialSimBridge bridge) => _tutorialBridge = bridge;
        public GameWorld GetWorld() => _runner.GetWorld();

        // ── 관전 보드 변경 ──

        public void SetSpectateBoard(int boardIndex)
        {
            _localPlayerIndex = boardIndex;
            _unitViewManager.SetActiveBoard(boardIndex);
            RefreshSynergySnapshot();
        }

        // ── 커맨드 전달 (View → Simulation) ──

        public void SendCommand(GameCommand command)
        {
            _runner.EnqueueCommand(command);
        }

        // ── 슈퍼노바 오브젝트 ──

        private readonly Dictionary<(int traitId, byte playerIndex), SupernovaObjectView>
            _supernovaObjects = new();

        // 타겟 부여 VFX (유닛에 붙는 루프 이펙트)
        private readonly Dictionary<(int traitId, byte playerIndex), (int entityId, AsyncOperationHandle<GameObject> handle, float appliedScale)>
            _supernovaTargetVfx = new();

        // 전투 시작 루프 VFX (전투 종료 시 제거)
        private readonly List<AsyncOperationHandle<GameObject>> _battleStartVfxHandles = new();

        private void HandleSupernovaObjectEvent(SimEvent evt)
        {
            int traitId = evt.Value0;
            byte subType = (byte)evt.Value1;
            var key = (traitId, evt.PlayerIndex);

            switch (subType)
            {
                case SupernovaSubType.Spawn:
                    SpawnSupernovaObject(key, traitId, evt.Col, evt.Row);
                    break;

                case SupernovaSubType.Remove:
                    RemoveSupernovaObject(key);
                    break;

                case SupernovaSubType.Move:
                    if (_supernovaObjects.TryGetValue(key, out var moveView))
                        moveView.UpdatePosition(evt.Col, evt.Row);
                    break;

                case SupernovaSubType.TierChanged:
                    if (_supernovaTargetVfx.ContainsKey(key))
                    {
                        // 루프 VFX는 동일하지만 스케일이 티어별로 다르므로 재부착
                        RemoveTargetVfx(key);
                        SpawnTargetVfx(key, traitId, evt.EntityId);
                    }
                    break;

                case SupernovaSubType.TargetAssigned:
                {
                    RemoveSupernovaObject(key);
                    // 전투 진입 시 자동 배정된 경우 토스트
                    var world = _runner.GetWorld();
                    if (world != null && world.IsCombatActive)
                        ToastManager.Instance.ShowToastByTokenKey("NOT_SUPERNOVA_ITEM_APPLY");
                    SpawnTargetVfx(key, traitId, evt.EntityId);
                    break;
                }

                case SupernovaSubType.TargetRemoved:
                    RemoveTargetVfx(key);
                    break;

                case SupernovaSubType.InvalidDrop:
                    ToastManager.Instance.ShowToastByTokenKey("NOT_SUPERNOVA_TYPE");
                    break;
            }
        }

        private async void SpawnSupernovaObject((int traitId, byte playerIndex) key, int traitId, byte col, byte row)
        {
            RemoveSupernovaObject(key); // 기존 오브젝트 정리

            if (_synergyVfxConfig == null) return;
            var synergyType = (SynergyType)traitId;
            if (!_synergyVfxConfig.TryGetTaggedVfx(synergyType, SynergyVfxTag.BoardObject, out var boardVfx)) return;

            var go = await Addressables.InstantiateAsync(boardVfx.Vfx, transform);
            if (go == null) return;
            Debug.Log($"<color=magenta>[Supernova] SpawnObject: {go.name} at ({col},{row})</color>");

            var view = go.GetComponent<SupernovaObjectView>();
            if (view == null)
            {
                Addressables.ReleaseInstance(go);
                return;
            }

            byte tier = 1;
            var world = _runner.GetWorld();
            if (world != null)
            {
                int prepIdx = SynergySystem.FindPrepBehavior(world, key.playerIndex, traitId);
                if (prepIdx >= 0)
                    tier = world.PrepBehaviors[key.playerIndex][prepIdx].Tier;
            }

            view.Setup(traitId, col, row, tier,
                () => _runner.GetWorld(),
                cmd => SendCommand(cmd));

            // 별똥별 원샷 → 0.5초 후 구체 등록 (드래그 가능해짐)
            SpawnBoardObjectWithIntroAsync(key, view, synergyType).Forget();
        }

        private async UniTaskVoid SpawnBoardObjectWithIntroAsync(
            (int traitId, byte playerIndex) key, SupernovaObjectView view, SynergyType synergyType)
        {
            // 구체는 0.5초간 숨김 (스폰 연출 중)
            view.gameObject.SetActive(false);

            if (_synergyVfxConfig.TryGetTaggedVfx(synergyType, SynergyVfxTag.BoardObjectSpawn, out var spawnVfx))
                SpawnOneShotVfxAtAsync(spawnVfx.Vfx, view.transform.position).Forget();

            var canceled = await UniTask.Delay(500, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            if (canceled || view == null) return;

            view.gameObject.SetActive(true);
            _supernovaObjects[key] = view;
        }

        private void RemoveSupernovaObject((int traitId, byte playerIndex) key)
        {
            if (_supernovaObjects.TryGetValue(key, out var view))
            {
                if (view != null)
                    Addressables.ReleaseInstance(view.gameObject);
                _supernovaObjects.Remove(key);
            }
        }

        private async void SpawnTargetVfx((int traitId, byte playerIndex) key, int traitId, int entityId)
        {
            RemoveTargetVfx(key);

            if (_synergyVfxConfig == null) return;
            var synergyType = (SynergyType)traitId;
            var ct = destroyCancellationToken;

            await UniTask.DelayFrame(1, cancellationToken: ct);

            var targetView = FindUnitView(entityId);
            if (targetView == null) return;

            await UniTask.WaitUntil(() => targetView == null || targetView.IsReady, cancellationToken: ct);
            if (targetView == null) return;

            var world = _runner.GetWorld();
            if (world == null) return;
            int prepIdx = SynergySystem.FindPrepBehavior(world, key.playerIndex, traitId);
            if (prepIdx < 0) return;
            var prep = world.PrepBehaviors[key.playerIndex][prepIdx];
            if (prep.PrepTargetEntityId != entityId) return;

            if (!_synergyVfxConfig.TryGetTaggedVfx(synergyType, SynergyVfxTag.TargetVfx, out var targetVfx)) return;

            var parentTransform = targetVfx.Position != SkillPosition.CUSTOM
                ? targetView.GetSkillPositionTransform(targetVfx.Position)
                : targetView.transform;
            var handle = Addressables.InstantiateAsync(targetVfx.Vfx, parentTransform);
            var go = await handle;
            if (go == null || !handle.IsValid()) return;
            Debug.Log($"<color=magenta>[Supernova] TargetVfx: {go.name} on entityId={entityId}</color>");

            var sn = prep as SynergyPrepSupernova;
            var scaleBonus = sn?.ViewScaleBonus ?? 0f;
            _supernovaTargetVfx[key] = (entityId, handle, scaleBonus);
            if (scaleBonus > 0f) targetView.AddViewScale(scaleBonus);
        }

        /// <summary>보드 뷰 또는 전투 뷰에서 유닛 조회 (entityId = 보드 EntityId)</summary>
        private UnitView FindUnitView(int entityId)
        {
            return _unitViewManager.FindBoardView(entityId)
                ?? _unitViewManager.FindCombatViewByEntityId(entityId);
        }

        private async UniTaskVoid SpawnOneShotVfxAsync(AssetReferenceGameObject vfxRef, Transform parent)
        {
            var handle = Addressables.InstantiateAsync(vfxRef, parent);
            var go = await handle;
            if (go == null || !handle.IsValid()) return;

            var canceled = await UniTask.Delay(3000, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            if (handle.IsValid()) Addressables.ReleaseInstance(handle);
        }

        private async UniTaskVoid SpawnOneShotVfxAtAsync(AssetReferenceGameObject vfxRef, Vector3 worldPos)
        {
            var handle = Addressables.InstantiateAsync(vfxRef, transform);
            var go = await handle;
            if (go == null || !handle.IsValid()) return;

            go.transform.position = worldPos;

            var canceled = await UniTask.Delay(3000, cancellationToken: destroyCancellationToken).SuppressCancellationThrow();
            if (handle.IsValid()) Addressables.ReleaseInstance(handle);
        }

        /// <summary>전투 시작 시 PrepBehavior 부여 유닛에 OnBattleStart_TierN 루프 VFX 부착</summary>
        private void PlayPrepBattleStartVfx()
        {
            if (_synergyVfxConfig == null) return;

            foreach (var kv in _supernovaTargetVfx)
            {
                var synergyType = (SynergyType)kv.Key.traitId;
                int entityId = kv.Value.entityId;

                var world = _runner.GetWorld();
                if (world == null) continue;
                int prepIdx = SynergySystem.FindPrepBehavior(world, kv.Key.playerIndex, kv.Key.traitId);
                if (prepIdx < 0) continue;
                byte tier = world.PrepBehaviors[kv.Key.playerIndex][prepIdx].Tier;

                var battleTag = SynergyVfxConfigSO.BattleStartTierToTag(tier);
                if (!_synergyVfxConfig.TryGetTaggedVfx(synergyType, battleTag, out var battleVfx)) continue;

                SpawnBattleStartVfxOnViewAsync(battleVfx.Vfx, entityId, kv.Key).Forget();
            }
        }

        /// <summary>유닛 뷰에 루프 VFX 부착 (전투 종료 시 일괄 제거)</summary>
        private async UniTaskVoid SpawnBattleStartVfxOnViewAsync(AssetReferenceGameObject vfxRef, int entityId,
            (int traitId, byte playerIndex) targetKey)
        {
            var view = FindUnitView(entityId);
            if (view == null) return;

            await UniTask.WaitUntil(() => view == null || view.IsReady, cancellationToken: destroyCancellationToken);
            if (view == null) return;

            var handle = Addressables.InstantiateAsync(vfxRef, view.transform);
            var go = await handle;
            if (go == null || !handle.IsValid()) return;

            // OnBattleStart VFX 생성 완료 → TargetVfx 릴리즈 (스케일 정보 유지)
            if (_supernovaTargetVfx.TryGetValue(targetKey, out var targetEntry))
            {
                if (targetEntry.handle.IsValid())
                    Addressables.ReleaseInstance(targetEntry.handle);
                _supernovaTargetVfx[targetKey] = (targetEntry.entityId, default, targetEntry.appliedScale);
            }

            Debug.Log($"<color=magenta>[Supernova] BattleStartVfx: {go.name} on entityId={entityId}</color>");
            _battleStartVfxHandles.Add(handle);
        }

        /// <summary>전투 종료 시 OnBattleStart 루프 VFX 일괄 제거</summary>
        private void ClearBattleStartVfx()
        {
            for (int i = 0; i < _battleStartVfxHandles.Count; i++)
            {
                if (_battleStartVfxHandles[i].IsValid())
                    Addressables.ReleaseInstance(_battleStartVfxHandles[i]);
            }
            _battleStartVfxHandles.Clear();
        }

        // ── 슈퍼노바 오브젝트 드래그 VFX ──

        private readonly List<AsyncOperationHandle<GameObject>> _dragHighlightHandles = new();

        private void HandleSupernovaDragStart(IBoardDraggableObject boardObj)
        {
            if (boardObj is not SupernovaObjectView supernovaObj) return;
            if (_synergyVfxConfig == null) return;

            var synergyType = (SynergyType)supernovaObj.TraitId;
            if (!_synergyVfxConfig.TryGetTaggedVfx(synergyType, SynergyVfxTag.DragHighlight, out var dragVfx)) return;

            int traitBit = 1 << supernovaObj.TraitId;
            var world = _runner.GetWorld();
            if (world == null) return;

            var boardSlots = world.BoardSlots[_localPlayerIndex];
            for (int i = 0; i < world.BoardSize; i++)
            {
                int entityId = boardSlots[i];
                if (entityId == UnitData.InvalidId) continue;

                ref var unit = ref world.GetUnit(entityId);
                if ((unit.TraitFlags & traitBit) == 0) continue;

                var unitView = _unitViewManager.FindBoardView(entityId);
                if (unitView == null) continue;

                var posTransform = dragVfx.Position != SkillPosition.CUSTOM
                    ? unitView.GetSkillPositionTransform(dragVfx.Position)
                    : unitView.transform;

                SpawnDragHighlightAsync(dragVfx.Vfx, posTransform).Forget();
            }
        }

        private async UniTaskVoid SpawnDragHighlightAsync(AssetReferenceGameObject vfxRef, Transform parent)
        {
            var handle = Addressables.InstantiateAsync(vfxRef, parent);
            var go = await handle;
            if (go == null || !handle.IsValid()) return;
            _dragHighlightHandles.Add(handle);
        }

        private void HandleSupernovaDragEnd()
        {
            for (int i = 0; i < _dragHighlightHandles.Count; i++)
            {
                if (_dragHighlightHandles[i].IsValid())
                    Addressables.ReleaseInstance(_dragHighlightHandles[i]);
            }
            _dragHighlightHandles.Clear();
        }

        private void RemoveTargetVfx((int traitId, byte playerIndex) key)
        {
            if (_supernovaTargetVfx.TryGetValue(key, out var entry))
            {
                var unitView = FindUnitView(entry.entityId);
                unitView?.RemoveViewScale(entry.appliedScale);

                if (entry.handle.IsValid())
                    Addressables.ReleaseInstance(entry.handle);
                _supernovaTargetVfx.Remove(key);
            }
        }

        /// <summary>전투 뷰 생성 시 슈퍼노바 스케일 재적용 + 버프 아이콘 갱신</summary>
        private async void HandleCombatViewCreated(int entityId, UnitView view)
        {
            // 버프 아이콘: 뷰 생성 전에 이미 추적된 버프가 있으면 즉시 반영
            _buffIconTracker?.RefreshIconsForUnit(view.CombatId);

            float scale = 0f;
            foreach (var kv in _supernovaTargetVfx)
            {
                if (kv.Value.entityId == entityId && kv.Value.appliedScale > 0f)
                {
                    scale = kv.Value.appliedScale;
                    break;
                }
            }
            if (scale <= 0f) return;

            await UniTask.WaitUntil(() => view == null || view.IsReady);
            if (view == null) return;

            view.AddViewScale(scale, forceSet: true);
        }

        /// <summary>재접속 시 기존 PrepBehavior 상태에서 슈퍼노바 비주얼 복원</summary>
        public void RestoreSupernovaObjects()
        {
            var world = _runner.GetWorld();
            if (world == null) return;

            byte playerIndex = (byte)_localPlayerIndex;
            int count = world.PrepBehaviorCounts[playerIndex];

            for (int i = 0; i < count; i++)
            {
                var b = world.PrepBehaviors[playerIndex][i];
                if (b is not SynergyPrepSupernova sn) continue;

                var key = (sn.TraitId, playerIndex);

                if (sn.PrepTargetEntityId >= 0)
                {
                    // 타겟 부여 상태 → 유닛에 VFX
                    SpawnTargetVfx(key, sn.TraitId, sn.PrepTargetEntityId);
                }
                else if (sn.ObjectCol >= 0)
                {
                    // 미부여 → 구체 오브젝트
                    SpawnSupernovaObject(key, sn.TraitId, (byte)sn.ObjectCol, (byte)sn.ObjectRow);
                }
            }
        }

        /// <summary>BoardInputHandler용: 좌표로 보드 오브젝트 조회</summary>
        public IBoardDraggableObject FindBoardObjectAt(int col, int row)
        {
            foreach (var kv in _supernovaObjects)
            {
                if (kv.Value != null && kv.Value.Col == col && kv.Value.Row == row)
                    return kv.Value;
            }
            return null;
        }

        // ── 게임 종료 ──

        public void ExitGame()
        {
            _runner?.StopSimulation();
        }
    }
}
