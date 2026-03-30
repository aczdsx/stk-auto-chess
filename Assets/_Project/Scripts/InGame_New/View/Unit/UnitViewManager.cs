using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 유닛 뷰 생명주기 관리. 시뮬레이션 상태와 UnitView를 동기화.
    /// 풀링 기반으로 UnitView 인스턴스를 재사용.
    /// </summary>
    public class UnitViewManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private UnitView _unitPrefab;
        [SerializeField] private int _poolSize = 64;

        private readonly Dictionary<int, UnitView> _boardUnitViews = new();  // EntityId → View
        private readonly Dictionary<int, UnitView> _combatUnitViews = new(); // CombatId → View
        private readonly List<UnitView> _pool = new();
        private readonly HashSet<int> _syncActiveIds = new();
        private readonly List<int> _syncRemoveBuffer = new();
        private int _activeBoardIndex;
        private bool _initialViewsReady;

        // VFX parking: 페이즈 전환 시 Persistent VFX를 파괴하지 않고 임시 보관
        private Transform _vfxParkingHolder;
        private readonly Dictionary<int, List<(int skillSpecId, UnityEngine.GameObject go, SkillPosition position)>>
            _parkedPersistentVfx = new();

        public Transform VfxParkingHolder => _vfxParkingHolder;

        /// <summary>첫 보드 뷰 로딩이 모두 완료되면 발화</summary>
        public event Action OnAllBoardViewsReady;

        /// <summary>전투 뷰가 새로 생성될 때 발화 (sourceEntityId, view)</summary>
        public event Action<int, UnitView> OnCombatViewCreated;

        public void SetPrefab(UnitView prefab, int poolSize = 64)
        {
            _unitPrefab = prefab;
            _poolSize = poolSize;
        }

        // ── 초기화 ──

        public void Initialize()
        {
            // VFX parking holder 생성 (비활성화하여 파티클 시스템 자동 중단)
            _vfxParkingHolder = new GameObject("VfxParkingHolder").transform;
            _vfxParkingHolder.SetParent(transform);
            _vfxParkingHolder.gameObject.SetActive(false);

            // 풀 생성
            for (int i = 0; i < _poolSize; i++)
            {
                var view = Instantiate(_unitPrefab, transform);
                view.gameObject.SetActive(false);
                _pool.Add(view);
            }
        }

        // ── 보드/벤치 유닛 동기화 (Preparation) ──

        /// <summary>모든 플레이어의 보드/벤치 유닛 동기화</summary>
        public void SyncBoardUnits(GameWorld world)
        {
            // 현재 프레임에 존재하는 EntityId 수집
            _syncActiveIds.Clear();
            var activeIds = _syncActiveIds;

            for (int p = 0; p < world.Config.PlayerCount; p++)
            {
                if (!world.Players[p].IsAlive) continue;
                bool isActive = p == _activeBoardIndex;

                // 보드 유닛
                var boardSlots = world.BoardSlots[p];
                for (int slot = 0; slot < world.BoardSize; slot++)
                {
                    int entityId = boardSlots[slot];
                    if (entityId == UnitData.InvalidId) continue;

                    int unitIdx = world.FindUnitIndex(entityId);
                    if (unitIdx < 0) continue;

                    ref var unit = ref world.Units[unitIdx];
                    activeIds.Add(entityId);

                    BoardHelper.FromIndex(slot, out int col, out int row);
                    byte sizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
                    byte sizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

                    // 다중 타일 유닛은 앵커 슬롯 외의 슬롯에서도 동일 EntityId가 등록되므로 중복 스킵
                    if (col != unit.BoardCol || row != unit.BoardRow) continue;

                    Vector3 worldPos = BoardWorldHelper.BoardGridToWorld(p, col, row)
                        + BoardWorldHelper.GetFootprintCenterOffset(sizeW, sizeH);

                    var view = GetOrCreateBoardView(entityId, unit.ChampionSpecId, unit.StarLevel);
                    if (isActive)
                    {
                        view.SetTargetPosition(worldPos);
                        int previewMaxHP = BoardSystem.CalcPreviewMaxHP(world, (byte)p, ref unit, entityId);
                        view.UpdateHP(previewMaxHP, previewMaxHP);
                    }
                    else
                    {
                        view.SetPositionImmediate(worldPos);
                    }
                }
            }

            // PvE 적 프리뷰 (준비 페이즈에서 적 위치 표시)
            for (int i = 0; i < world.PvEEnemyCount; i++)
            {
                ref var enemy = ref world.PvEEnemies[i];
                int pseudoId = -(i + 1); // 음수 ID로 실제 EntityId와 구분
                activeIds.Add(pseudoId);

                // PvE 좌표는 이미 전투 그리드 기준 (예: (0,6), (3,4))
                int mirrorCol = enemy.GridCol;
                int mirrorRow = enemy.GridRow;

                Vector3 worldPos = BoardWorldHelper.CombatGridToWorld(_activeBoardIndex, mirrorCol, mirrorRow)
                    + BoardWorldHelper.GetFootprintCenterOffset(enemy.SizeW, enemy.SizeH);

                var view = GetOrCreateBoardView(pseudoId, enemy.ChampionSpecId, 1);
                view.SetPositionImmediate(worldPos);

                // 적은 플레이어 쪽(하단)을 바라봄
                Vector3 lookTarget = BoardWorldHelper.CombatGridToWorld(_activeBoardIndex, mirrorCol, 0);
                view.UpdateFacing(lookTarget);
            }

            // 존재하지 않는 뷰 제거
            _syncRemoveBuffer.Clear();
            foreach (var kvp in _boardUnitViews)
            {
                if (!activeIds.Contains(kvp.Key))
                    _syncRemoveBuffer.Add(kvp.Key);
            }
            foreach (int id in _syncRemoveBuffer)
            {
                ReturnToPool(_boardUnitViews[id]);
                _boardUnitViews.Remove(id);
            }

            CheckInitialViewsReady();
        }

        private void CheckInitialViewsReady()
        {
            if (_initialViewsReady || _boardUnitViews.Count == 0) return;
            foreach (var view in _boardUnitViews.Values)
            {
                if (!view.IsReady) return;
            }
            _initialViewsReady = true;
            OnAllBoardViewsReady?.Invoke();
        }

        // ── 전투 유닛 동기화 (Combat) ──

        /// <summary>전투 매치의 CombatUnit 동기화</summary>
        public void SyncCombatUnits(CombatMatchState matchState, int boardIndex)
        {
            if (matchState == null) return;
            bool isActive = boardIndex == _activeBoardIndex;

            _syncActiveIds.Clear();
            var activeIds = _syncActiveIds;

            for (int i = 0; i < matchState.UnitCount; i++)
            {
                ref var unit = ref matchState.Units[i];
                if (unit.CombatId == CombatUnit.InvalidId) continue;

                activeIds.Add(unit.CombatId);

                byte sizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
                byte sizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;
                Vector3 centerOffset = BoardWorldHelper.GetFootprintCenterOffset(sizeW, sizeH);

                Vector3 destPos = BoardWorldHelper.CombatGridToWorld(boardIndex, unit.GridCol, unit.GridRow) + centerOffset;

                // 이동 중이면 MoveFrom → Dest 보간 위치 계산
                Vector3 worldPos;
                if (unit.IsMoving && unit.MoveDuration > 0)
                {
                    float progress = 1f - (float)unit.MoveTimer / unit.MoveDuration;

                    // Ease 적용
                    if (unit.DashEase != MoveEaseType.None)
                        progress = ApplyEase(progress, unit.DashEase);
                    else if (unit.IsKnockbackMoving)
                        progress = EaseOutExpo(progress);

                    Vector3 fromPos = BoardWorldHelper.CombatGridToWorld(boardIndex, unit.MoveFromCol, unit.MoveFromRow) + centerOffset;

                    // 오버슈트 페이즈: 그리드 안 바뀜, MoveFrom→GridPos 방향으로 1.8유닛 오프셋
                    if (unit.DashPhase == DashPhase.Overshoot)
                    {
                        Vector3 dir = (destPos - fromPos).normalized;
                        worldPos = Vector3.Lerp(destPos, destPos + dir * 1.8f, progress);
                    }
                    else
                    {
                        worldPos = Vector3.Lerp(fromPos, destPos, progress);
                    }

                    // 백라인 점프: 포물선 높이
                    if (unit.IsBacklineJumping)
                        worldPos.y += Mathf.Sin(progress * Mathf.PI) * 1.5f;
                }
                else
                {
                    worldPos = destPos;
                }

                // 위치를 먼저 계산한 뒤 뷰 생성 (풀에서 꺼낸 뷰가 이전 위치에서 활성화되는 것을 방지)
                var view = GetOrCreateCombatView(unit.CombatId, unit.SourceEntityId, unit.ChampionSpecId, unit.StarLevel, unit.TeamIndex == 0, worldPos);

                if (unit.IsAlive)
                {
                    if (isActive)
                    {
                        view.SetPositionImmediate(worldPos);
                        view.UpdateHP(unit.CurrentHP, unit.MaxHP, unit.ShieldAmount);
                        view.UpdateMana(unit.CurrentMana, unit.MaxMana);

                        // 방향 전환 (SetCombatState보다 먼저 — 공격 시작 시 타겟을 바라본 후 공격 모션 시작)
                        if (view.IsPlayingAttackAnim && unit.State != CombatState.Attacking)
                        {
                            // 공격 모션 중이면 방향 전환 스킵 (단, 새 공격 시작 시에는 방향 갱신)
                        }
                        else if (unit.IsMoving && unit.MoveDuration > 0 && !unit.IsKnockbackMoving)
                        {
                            view.UpdateFacing(destPos);
                        }
                        else if (unit.CurrentTargetId != CombatUnit.InvalidId)
                        {
                            int targetIdx = matchState.FindUnitIndex(unit.CurrentTargetId);
                            if (targetIdx >= 0)
                            {
                                ref var target = ref matchState.Units[targetIdx];
                                var targetPos = BoardWorldHelper.CombatGridToWorld(boardIndex, target.GridCol, target.GridRow)
                                    + BoardWorldHelper.GetFootprintCenterOffset(
                                        target.SizeW > 0 ? target.SizeW : (byte)1,
                                        target.SizeH > 0 ? target.SizeH : (byte)1);
                                view.UpdateFacing(targetPos);
                            }
                        }
                        else
                        {
                            // 타겟 없으면 기본 방향: 아군은 상단(적진), 적은 하단(아군진)
                            int defaultRow = unit.TeamIndex == 0 ? BoardHelper.CombatHeight - 1 : 0;
                            var defaultTarget = BoardWorldHelper.CombatGridToWorld(boardIndex, unit.GridCol, defaultRow);
                            view.UpdateFacing(defaultTarget);
                        }

                        view.SetCombatState(unit.State, unit.AttackSpeed);
                    }
                    else
                    {
                        view.SetPositionImmediate(worldPos);
                    }
                }
                else
                {
                    view.PlayDeathAnimation();
                }
            }

            // 존재하지 않는 전투 뷰 제거
            _syncRemoveBuffer.Clear();
            foreach (var kvp in _combatUnitViews)
            {
                if (!activeIds.Contains(kvp.Key))
                    _syncRemoveBuffer.Add(kvp.Key);
            }
            foreach (int id in _syncRemoveBuffer)
            {
                ReturnToPool(_combatUnitViews[id]);
                _combatUnitViews.Remove(id);
            }
        }

        // ── 전투 시작/종료 ──

        /// <summary>전투 시작 시 보드 뷰를 숨기고 전투 뷰 활성화. Persistent VFX는 parking하여 전투 뷰에서 재사용.</summary>
        public void OnCombatStart()
        {
            foreach (var kvp in _boardUnitViews)
            {
                var view = kvp.Value;
                int entityId = view.EntityId;
                var detached = view.DeactivateWithParking();
                if (detached != null && detached.Count > 0)
                {
                    foreach (var (_, go, _) in detached)
                    {
                        if (go != null) go.transform.SetParent(_vfxParkingHolder, worldPositionStays: false);
                    }
                    _parkedPersistentVfx[entityId] = detached;
                }
            }
            _boardUnitViews.Clear();
        }

        /// <summary>모든 전투 뷰를 강제 Idle 전환 (전투 종료 시)</summary>
        public void ForceAllCombatViewsIdle()
        {
            foreach (var view in _combatUnitViews.Values)
                view.ForceIdle();
        }

        /// <summary>전투 종료 시 전투 뷰를 정리</summary>
        public void OnCombatEnd()
        {
            foreach (var view in _combatUnitViews.Values)
                ReturnToPool(view);
            _combatUnitViews.Clear();
            ClearParkedVfx();
        }

        private void ClearParkedVfx()
        {
            foreach (var list in _parkedPersistentVfx.Values)
            {
                foreach (var (_, go, _) in list)
                {
                    if (go != null) Destroy(go);
                }
            }
            _parkedPersistentVfx.Clear();
        }

        private async UniTaskVoid ScheduleVfxReparent(int entityId, UnitView view)
        {
            var ct = destroyCancellationToken;
            bool canceled = await UniTask.WaitUntil(
                () => view == null || view.IsReady,
                cancellationToken: ct).SuppressCancellationThrow();
            if (canceled || this == null || view == null) return;

            if (_parkedPersistentVfx.TryGetValue(entityId, out var list))
            {
                foreach (var (skillSpecId, go, position) in list)
                    view.AdoptPersistentVfx(skillSpecId, go, position);
                _parkedPersistentVfx.Remove(entityId);
            }
        }

        /// <summary>활성 보드 변경 (관전)</summary>
        public void SetActiveBoard(int boardIndex)
        {
            _activeBoardIndex = boardIndex;
        }

        /// <summary>모든 보드 UnitView 순회 (타겟 라인 등)</summary>
        public IReadOnlyDictionary<int, UnitView> BoardUnitViews => _boardUnitViews;

        /// <summary>EntityId로 보드 UnitView 조회 (보드 드래그용)</summary>
        public UnitView FindBoardView(int entityId)
        {
            _boardUnitViews.TryGetValue(entityId, out var view);
            return view;
        }

        public UnitView FindCombatView(int combatId)
        {
            _combatUnitViews.TryGetValue(combatId, out var view);
            return view;
        }

        /// <summary>SourceEntityId(보드 EntityId)로 전투 UnitView 조회</summary>
        public UnitView FindCombatViewByEntityId(int entityId)
        {
            foreach (var kv in _combatUnitViews)
            {
                if (kv.Value != null && kv.Value.EntityId == entityId)
                    return kv.Value;
            }
            return null;
        }

        // ── 고스트 뷰 (홀로그램 드래그 프리뷰) ──

        private UnitView _ghostView;

        public UnitView CreateGhostView(int entityId, GameWorld world)
        {
            ReleaseGhostView();

            ref var unit = ref world.GetUnit(entityId);
            string prefabPath = GetCharacterPrefabPath(unit.ChampionSpecId);

            _ghostView = GetFromPool();
            _ghostView.Initialize(entityId, unit.StarLevel, prefabPath);
            return _ghostView;
        }

        public void ReleaseGhostView()
        {
            if (_ghostView == null) return;
            ReturnToPool(_ghostView);
            _ghostView = null;
        }

        // ── 풀 관리 ──

        private static string GetCharacterPrefabPath(int championSpecId)
        {
            var spec = SpecDataManager.Instance.GetSpecCharacter(championSpecId);
            return spec?.ToCharacterResourcePath();
        }

        private UnitView GetOrCreateBoardView(int entityId, int champSpecId, byte starLevel)
        {
            if (_boardUnitViews.TryGetValue(entityId, out var existing))
            {
                existing.UpdateStarLevel(starLevel);
                return existing;
            }

            var view = GetFromPool();
            view.Initialize(entityId, starLevel, GetCharacterPrefabPath(champSpecId), champSpecId);
            _boardUnitViews[entityId] = view;
            return view;
        }

        private UnitView GetOrCreateCombatView(int combatId, int sourceEntityId, int champSpecId, byte starLevel, bool isPlayer = true, Vector3 initialPosition = default)
        {
            if (_combatUnitViews.TryGetValue(combatId, out var existing))
                return existing;

            var view = GetFromPool();
            // 풀에서 꺼낸 뷰의 위치를 먼저 보정한 뒤 초기화 (이전 위치에서 미끄러지는 현상 방지)
            view.transform.position = initialPosition;
            view.InitializeAsCombat(combatId, sourceEntityId, starLevel, GetCharacterPrefabPath(champSpecId), champSpecId, isPlayer);
            _combatUnitViews[combatId] = view;
            OnCombatViewCreated?.Invoke(sourceEntityId, view);

            // Parked VFX가 있으면 전투 뷰에 reparent 스케줄링
            if (_parkedPersistentVfx.ContainsKey(sourceEntityId))
                ScheduleVfxReparent(sourceEntityId, view).Forget();

            return view;
        }

        private UnitView GetFromPool()
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    var view = _pool[i];
                    view.gameObject.SetActive(true);
                    return view;
                }
            }

            // 풀 확장
            var newView = Instantiate(_unitPrefab, transform);
            _pool.Add(newView);
            return newView;
        }

        private void ReturnToPool(UnitView view)
        {
            view.Deactivate();
        }

        /// <summary>OutExpo 이징: 빠르게 시작 → 느리게 감속</summary>
        private static float EaseOutExpo(float t)
        {
            return t >= 1f ? 1f : 1f - UnityEngine.Mathf.Pow(2f, -10f * t);
        }

        /// <summary>MoveEaseType에 따른 보간 커브 적용</summary>
        private static float ApplyEase(float t, MoveEaseType ease)
        {
            switch (ease)
            {
                case MoveEaseType.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case MoveEaseType.Linear:
                    return t;
                case MoveEaseType.OutExpo:
                    return EaseOutExpo(t);
                case MoveEaseType.InExpo:
                    return t >= 1f ? 1f : Mathf.Pow(2f, 10f * (t - 1f));
                default:
                    return t;
            }
        }
    }
}
