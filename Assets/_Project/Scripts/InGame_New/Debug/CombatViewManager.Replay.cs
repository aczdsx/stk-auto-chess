using UnityEngine;

namespace CookApps.AutoChess.View
{
    // ── 리플레이 전용 (디버그 빌드 전용) ──
    public partial class CombatViewManager
    {
        /// <summary>
        /// 리플레이용 View 리셋.
        /// 투사체/pending 큐를 정리하되 유닛 View는 유지.
        /// </summary>
        public void ResetForReplay()
        {
            _pendingProjectiles.Clear();
            _pendingMeleeAttacks.Clear();
            _pendingMeleeTargetIds.Clear();
            _tileEffectManager?.HideAll();
            ClearAllProjectiles();

            // VFX 루트 하위 fire-and-forget VFX 정리
            if (_vfxRoot != null)
            {
                for (int i = _vfxRoot.childCount - 1; i >= 0; i--)
                    Object.Destroy(_vfxRoot.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// 리플레이 후 투사체 즉시 스폰 (pending 딜레이 없이).
        /// </summary>
        public void SpawnProjectileImmediate(int sourceId, int targetId, ProjectileType projType,
            byte col, byte row, sbyte dirCol, sbyte dirRow, int champSpecId, int projectileId,
            int skillSpecId = 0, sbyte skillVfxIndex = -1, int moveInterval = 0)
        {
            var sourceView = _unitViewManager?.FindCombatView(sourceId);
            if (sourceView == null) return;

            GameObject prefab = null;

            if (skillVfxIndex >= 0)
            {
                var skillPrefabs = sourceView.GetSkillEffectPrefabs();
                if (skillPrefabs != null && skillVfxIndex < skillPrefabs.Length && skillPrefabs[skillVfxIndex]?.Prefab != null)
                    prefab = skillPrefabs[skillVfxIndex].Prefab;
            }
            if (prefab == null) prefab = sourceView.GetProjectilePrefab();
            if (prefab == null)
            {
                var skillPrefabs = sourceView.GetSkillEffectPrefabs();
                if (skillPrefabs != null && skillPrefabs.Length > 0 && skillPrefabs[0]?.Prefab != null)
                    prefab = skillPrefabs[0].Prefab;
            }
            if (prefab == null) return;

            float moveSpeed = DefaultProjectileSpeed;
            if (moveInterval > 0)
            {
                Vector3 startPos = BoardWorldHelper.CombatGridToWorld(0, col, row);
                Vector3 nextPos = BoardWorldHelper.CombatGridToWorld(0, (byte)(col + dirCol), (byte)(row + dirRow));
                float tileDist = Vector3.Distance(startPos, nextPos);
                float timePerTile = moveInterval / _simulationFPS;
                moveSpeed = tileDist / timePerTile;
            }

            SpawnProjectileFromPending(new PendingProjectile
            {
                Delay = 0f,
                ProjectileId = projectileId,
                SourceId = sourceId,
                TargetId = targetId,
                ProjType = projType,
                Col = col,
                Row = row,
                DirCol = dirCol,
                DirRow = dirRow,
                VfxPrefab = prefab,
                MoveSpeed = moveSpeed,
            });
        }
    }
}
