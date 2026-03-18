using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
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
        private int _activeBoardIndex;
        private bool _initialViewsReady;

        /// <summary>첫 보드 뷰 로딩이 모두 완료되면 발화</summary>
        public event Action OnAllBoardViewsReady;

        public void SetPrefab(UnitView prefab, int poolSize = 64)
        {
            _unitPrefab = prefab;
            _poolSize = poolSize;
        }

        // ── 초기화 ──

        public void Initialize()
        {
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
            var activeIds = new HashSet<int>();

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
                        int previewMaxHP = BoardSystem.CalcPreviewMaxHP(world, (byte)p, ref unit);
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
            var toRemove = new List<int>();
            foreach (var kvp in _boardUnitViews)
            {
                if (!activeIds.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (int id in toRemove)
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

            var activeIds = new HashSet<int>();

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

                    // 넉백 이동은 OutExpo ease 적용
                    if (unit.IsKnockbackMoving)
                        progress = EaseOutExpo(progress);

                    Vector3 fromPos = BoardWorldHelper.CombatGridToWorld(boardIndex, unit.MoveFromCol, unit.MoveFromRow) + centerOffset;
                    worldPos = Vector3.Lerp(fromPos, destPos, progress);
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
                        view.SetCombatState(unit.State, unit.AttackSpeed);

                        // 이동 중이면 이동 방향, 아니면 타겟 방향 바라보기
                        if (unit.IsMoving && unit.MoveDuration > 0)
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
            var toRemove = new List<int>();
            foreach (var kvp in _combatUnitViews)
            {
                if (!activeIds.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            }
            foreach (int id in toRemove)
            {
                ReturnToPool(_combatUnitViews[id]);
                _combatUnitViews.Remove(id);
            }
        }

        // ── 전투 시작/종료 ──

        /// <summary>전투 시작 시 보드 뷰를 숨기고 전투 뷰 활성화</summary>
        public void OnCombatStart()
        {
            foreach (var view in _boardUnitViews.Values)
                view.Deactivate();
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
    }
}
