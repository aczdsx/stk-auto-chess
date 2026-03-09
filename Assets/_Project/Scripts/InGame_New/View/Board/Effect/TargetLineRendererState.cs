using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    // ── 상태 기반 클래스 ──

    public abstract class TargetLineStateBase
    {
        protected readonly TargetLineRendererPool Pool;
        protected readonly UnitViewManager UnitViewManager;

        protected TargetLineStateBase(UnitViewManager unitViewManager, float yOffset)
        {
            UnitViewManager = unitViewManager;
            Pool = new TargetLineRendererPool(yOffset);
        }

        public virtual void Enter() { }
        public virtual void Exit() => Pool.HideAll();
        public abstract void Draw();
        public void Clear() => Pool.ClearAll();

        protected static UnitView FindNearestEnemy(
            UnitView source, IReadOnlyDictionary<int, UnitView> boardViews, bool isSourcePlayer)
        {
            UnitView nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var kvp in boardViews)
            {
                bool isTargetPlayer = kvp.Key >= 0;
                if (isTargetPlayer == isSourcePlayer) continue;

                var tv = kvp.Value;
                if (tv == null || !tv.gameObject.activeSelf) continue;

                float dist = Vector3.Distance(source.transform.position, tv.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = tv;
                }
            }
            return nearest;
        }
    }

    // ── Idle 상태: 아군/적군 교대 표시 ──

    public class TargetLineIdleState : TargetLineStateBase
    {
        private bool _drawPlayerNext = true;

        public TargetLineIdleState(UnitViewManager unitViewManager, float yOffset)
            : base(unitViewManager, yOffset) { }

        public override void Enter() => _drawPlayerNext = true;

        public override void Draw()
        {
            Pool.HideAll();

            var boardViews = UnitViewManager.BoardUnitViews;
            foreach (var kvp in boardViews)
            {
                bool isPlayerUnit = kvp.Key >= 0;
                if (isPlayerUnit != _drawPlayerNext) continue;

                var sourceView = kvp.Value;
                if (sourceView == null || !sourceView.gameObject.activeSelf) continue;

                var targetView = FindNearestEnemy(sourceView, boardViews, isPlayerUnit);
                if (targetView == null) continue;

                Pool.DrawLine(sourceView, targetView, isPlayerUnit);
            }
            _drawPlayerNext = !_drawPlayerNext;
        }
    }

    // ── Focused 상태: 선택/홀드 캐릭터 관련 라인만 ──

    public class TargetLineFocusedState : TargetLineStateBase
    {
        private readonly ISimulationRunner _runner;
        private float _cachedTileSpacing;
        private int _focusedEntityId = UnitData.InvalidId;

        public bool HasFocus => _focusedEntityId != UnitData.InvalidId;
        public bool IsInitialized => _cachedTileSpacing > 0f;

        public TargetLineFocusedState(
            UnitViewManager unitViewManager, ISimulationRunner runner,
            float yOffset, float tileSpacing)
            : base(unitViewManager, yOffset)
        {
            _runner = runner;
            _cachedTileSpacing = tileSpacing;
        }

        public void SetTileSpacing(float spacing) => _cachedTileSpacing = spacing;

        public void SetFocus(int entityId)
        {
            _focusedEntityId = entityId;
            Draw();
        }

        public void ClearFocus()
        {
            _focusedEntityId = UnitData.InvalidId;
            Pool.HideAll();
        }

        public void Refresh()
        {
            if (_focusedEntityId == UnitData.InvalidId) return;
            Draw();
        }

        public override void Exit()
        {
            _focusedEntityId = UnitData.InvalidId;
            base.Exit();
        }

        public override void Draw()
        {
            if (_focusedEntityId == UnitData.InvalidId) return;
            Pool.HideAll();

            var boardViews = UnitViewManager.BoardUnitViews;
            if (!boardViews.TryGetValue(_focusedEntityId, out var focusedView)) return;
            if (focusedView == null || !focusedView.gameObject.activeSelf) return;

            bool isFocusedPlayer = _focusedEntityId >= 0;
            var world = _runner.GetWorld();
            if (world == null) return;

            // 1) 포커스 유닛 → 가장 가까운 적 (범위 무제한, keepVisible로 유지)
            var focusTarget = FindNearestEnemy(focusedView, boardViews, isFocusedPlayer);
            if (focusTarget != null)
                Pool.DrawLine(focusedView, focusTarget, isFocusedPlayer, keepVisible: true);

            // 2) 적 → 포커스 유닛 (각 적의 공격범위 내일 때만, keepVisible로 유지)
            foreach (var kvp in boardViews)
            {
                if (kvp.Key == _focusedEntityId) continue;
                var otherView = kvp.Value;
                if (otherView == null || !otherView.gameObject.activeSelf) continue;

                bool isOtherPlayer = kvp.Key >= 0;
                if (isOtherPlayer == isFocusedPlayer) continue;

                var otherTarget = FindNearestEnemy(otherView, boardViews, isOtherPlayer);
                if (otherTarget != focusedView) continue;

                int otherRange = GetAttackRange(world, kvp.Key);
                float otherMaxDist = otherRange * _cachedTileSpacing;
                float dist = Vector3.Distance(otherView.transform.position, focusedView.transform.position);
                if (dist > otherMaxDist) continue;

                Pool.DrawLine(otherView, focusedView, isOtherPlayer, keepVisible: true);
            }
        }

        private static int GetAttackRange(GameWorld world, int entityId)
        {
            if (entityId >= 0)
            {
                int idx = world.FindUnitIndex(entityId);
                if (idx >= 0)
                    return world.Units[idx].AttackRange > 0 ? world.Units[idx].AttackRange : 1;
            }
            else
            {
                int pveIdx = -(entityId) - 1;
                if (pveIdx >= 0 && pveIdx < world.PvEEnemyCount)
                    return world.PvEEnemies[pveIdx].AttackRange > 0 ? world.PvEEnemies[pveIdx].AttackRange : 1;
            }
            return 1;
        }
    }

    // ── VFX 라인 풀 ──

    public class TargetLineRendererPool
    {
        private readonly List<InGameVfxTargetLine> _lines = new();
        private readonly float _yOffset;

        public TargetLineRendererPool(float characterYOffset)
        {
            _yOffset = characterYOffset;
        }

        public void DrawLine(UnitView sourceView, UnitView targetView, bool isOwn, bool keepVisible = false)
        {
            var line = GetOrCreateLine();
            if (line == null) return;

            var offset = new Vector3(0, _yOffset, 0);

            line.SetActiveObject(true);

            Action onComplete = keepVisible
                ? null
                : () =>
                {
                    if (line != null && line.CachedGo != null)
                        line.SetActiveObject(false);
                };

            line.TargetLine.DrawLine(
                () => sourceView != null ? sourceView.transform.position + offset : Vector3.zero,
                () => targetView != null ? targetView.transform.position + offset : Vector3.zero,
                isOwn,
                keepVisible,
                onComplete);
        }

        public void HideAll()
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i] != null && _lines[i].CachedGo != null && _lines[i].CachedGo.activeSelf)
                    _lines[i].SetActiveObject(false);
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i] != null && _lines[i].CachedGo != null)
                {
                    _lines[i].SetActiveObject(false);
                    Addressables.ReleaseInstance(_lines[i].CachedGo);
                }
            }
            _lines.Clear();
        }

        private InGameVfxTargetLine GetOrCreateLine()
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i] != null && _lines[i].CachedGo != null && !_lines[i].CachedGo.activeSelf)
                    return _lines[i];
            }

            var vfxData = SpecDataManager.Instance.GetInGameVfxData(InGameVfxNameType.TargetLineRenderer);
            if (vfxData == null || string.IsNullOrEmpty(vfxData.addressable_path)) return null;

            var go = Addressables.InstantiateAsync(vfxData.addressable_path).WaitForCompletion();
            if (go == null) return null;

            var line = go.GetComponent<InGameVfxTargetLine>();
            if (line == null)
            {
                Addressables.ReleaseInstance(go);
                return null;
            }

            _lines.Add(line);
            return line;
        }
    }
}
