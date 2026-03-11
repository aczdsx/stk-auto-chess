using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 타일 FX 통합 관리.
    /// - 배치 인디케이터 (Placement, AttackRange, SkillRange)
    /// - 전투 원소별 이펙트 (FireArea, WindCast 등)
    /// 모든 FX는 ObjectPool로 풀링되며, handle 기반으로 표시/제거.
    /// </summary>
    public class TileEffectManager : MonoBehaviour
    {
        private int _boardIndex;

        // handle → 활성 FX 리스트
        private readonly Dictionary<int, EffectGroup> _activeEffects = new();
        // 수명 기반 자동 제거
        private readonly List<TimedEffect> _timedEffects = new();
        // TileEffectType별 풀
        private readonly Dictionary<TileEffectType, ObjectPool<GameObject>> _pools = new();

        private int _nextHandle = 1;
        private Transform _fxRoot;

        private struct EffectGroup
        {
            public TileEffectType Type;
            public List<GameObject> FxList;
        }

        private struct TimedEffect
        {
            public int Handle;
            public float RemainTime;
        }

        // ── TileEffectType → Addressable 경로 매핑 ──

        private static readonly Dictionary<TileEffectType, string> AddressablePaths = new()
        {
            [TileEffectType.Placement] = "Prefabs/Fx/Common/fx_common_area_plan.prefab",
            [TileEffectType.AttackRange] = "Prefabs/Fx/Common/fx_common_area_plan_02.prefab",
            [TileEffectType.SkillRange] = "Prefabs/Fx/Common/fx_common_area_commander_01.prefab",
            [TileEffectType.FireArea] = "Prefabs/Fx/Common/fx_common_area_fire.prefab",
            [TileEffectType.WindArea] = "Prefabs/Fx/Common/fx_common_area_wind.prefab",
            [TileEffectType.LightningArea] = "Prefabs/Fx/Common/fx_common_area_light.prefab",
            [TileEffectType.EarthArea] = "Prefabs/Fx/Common/fx_common_area_earth.prefab",
            [TileEffectType.WaterArea] = "Prefabs/Fx/Common/fx_common_area_water.prefab",
            [TileEffectType.FireCast] = "Prefabs/Fx/Common/fx_common_cast_fire.prefab",
            [TileEffectType.WindCast] = "Prefabs/Fx/Common/fx_common_cast_wind.prefab",
            [TileEffectType.LightningCast] = "Prefabs/Fx/Common/fx_common_cast_light.prefab",
            [TileEffectType.EarthCast] = "Prefabs/Fx/Common/fx_common_cast_earth.prefab",
            [TileEffectType.WaterCast] = "Prefabs/Fx/Common/fx_common_cast_water.prefab",
        };

        // ── SynergyType 변환 헬퍼 ──

        public static TileEffectType SynergyToAreaType(SynergyType synergy)
        {
            if (synergy == SynergyType.FIRE) return TileEffectType.FireArea;
            if (synergy == SynergyType.WIND) return TileEffectType.WindArea;
            if (synergy == SynergyType.LIGHTNING) return TileEffectType.LightningArea;
            if (synergy == SynergyType.EARTH) return TileEffectType.EarthArea;
            if (synergy == SynergyType.WATER) return TileEffectType.WaterArea;
            return TileEffectType.FireArea; // fallback
        }

        public static TileEffectType SynergyToCastType(SynergyType synergy)
        {
            if (synergy == SynergyType.FIRE) return TileEffectType.FireCast;
            if (synergy == SynergyType.WIND) return TileEffectType.WindCast;
            if (synergy == SynergyType.LIGHTNING) return TileEffectType.LightningCast;
            if (synergy == SynergyType.EARTH) return TileEffectType.EarthCast;
            if (synergy == SynergyType.WATER) return TileEffectType.WaterCast;
            return TileEffectType.FireCast; // fallback
        }

        // ── 초기화 ──

        public void Initialize(int boardIndex = 0)
        {
            _boardIndex = boardIndex;

            var rootObj = new GameObject("TileEffectRoot");
            rootObj.transform.SetParent(transform);
            _fxRoot = rootObj.transform;
        }

        // ── 통합 API ──

        /// <summary>단일 타일에 FX 표시. duration=0이면 수동 제거, >0이면 자동 제거.</summary>
        public int Show(TileEffectType type, int col, int row, float duration = 0f)
        {
            var pos = BoardWorldHelper.BoardGridToWorld(_boardIndex, col, row);
            return ShowAtInternal(type, pos, duration);
        }

        /// <summary>범위 내 모든 타일에 FX 표시 (맨허튼 거리). 하나의 handle로 그룹핑.</summary>
        public int ShowRange(TileEffectType type, int centerCol, int centerRow, int range, float duration = 0f)
        {
            int handle = _nextHandle++;
            var fxList = new List<GameObject>();

            int width = BoardHelper.CombatWidth;
            int height = BoardHelper.CombatHeight;

            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    if (BoardHelper.ManhattanDistance(centerCol, centerRow, c, r) > range)
                        continue;

                    var pos = BoardWorldHelper.CombatGridToWorld(_boardIndex, c, r);
                    var fx = GetFromPool(type);
                    fx.transform.position = pos;
                    fxList.Add(fx);
                }
            }

            _activeEffects[handle] = new EffectGroup { Type = type, FxList = fxList };

            if (duration > 0f)
                _timedEffects.Add(new TimedEffect { Handle = handle, RemainTime = duration });

            return handle;
        }

        /// <summary>체비셰프 거리 기반 네모 범위 표시 (range=1 → 3×3)</summary>
        public int ShowRangeBox(TileEffectType type, int centerCol, int centerRow, int range, float duration = 0f)
        {
            int handle = _nextHandle++;
            var fxList = new List<GameObject>();

            int width = BoardHelper.CombatWidth;
            int height = BoardHelper.CombatHeight;

            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    int dc = c - centerCol;
                    int dr = r - centerRow;
                    if (dc < 0) dc = -dc;
                    if (dr < 0) dr = -dr;
                    int chebyshev = dc > dr ? dc : dr;
                    if (chebyshev > range)
                        continue;

                    var pos = BoardWorldHelper.CombatGridToWorld(_boardIndex, c, r);
                    var fx = GetFromPool(type);
                    fx.transform.position = pos;
                    fxList.Add(fx);
                }
            }

            _activeEffects[handle] = new EffectGroup { Type = type, FxList = fxList };

            if (duration > 0f)
                _timedEffects.Add(new TimedEffect { Handle = handle, RemainTime = duration });

            return handle;
        }

        /// <summary>특정 행의 col 범위에 FX 표시.</summary>
        public int ShowRow(TileEffectType type, int row, int centerCol, int halfWidth, float duration = 0f)
        {
            int handle = _nextHandle++;
            var fxList = new List<GameObject>();

            for (int c = centerCol - halfWidth; c <= centerCol + halfWidth; c++)
            {
                if (!BoardHelper.IsValidCombatPosition(c, row)) continue;

                var pos = BoardWorldHelper.CombatGridToWorld(_boardIndex, c, row);
                var fx = GetFromPool(type);
                fx.transform.position = pos;
                fxList.Add(fx);
            }

            _activeEffects[handle] = new EffectGroup { Type = type, FxList = fxList };

            if (duration > 0f)
                _timedEffects.Add(new TimedEffect { Handle = handle, RemainTime = duration });

            return handle;
        }

        /// <summary>ㄷ자형 범위 표시. 타겟 방향 기준 2×3.</summary>
        public int ShowDirectionalRect(TileEffectType type, int centerCol, int centerRow, int dirCol, int dirRow, float duration = 0f)
        {
            int handle = _nextHandle++;
            var fxList = new List<GameObject>();

            bool rowDominant = dirRow != 0;

            if (rowDominant)
            {
                // 가로 3칸 × 세로 2칸 (본인행 + 전방1행), 본인 타일 제외
                for (int rowOffset = 0; rowOffset <= 1; rowOffset++)
                {
                    int r = centerRow + dirRow * rowOffset;
                    for (int c = centerCol - 1; c <= centerCol + 1; c++)
                    {
                        if (c == centerCol && r == centerRow) continue; // 본인 타일 제외
                        if (!BoardHelper.IsValidCombatPosition(c, r)) continue;
                        var pos = BoardWorldHelper.CombatGridToWorld(_boardIndex, c, r);
                        var fx = GetFromPool(type);
                        fx.transform.position = pos;
                        fxList.Add(fx);
                    }
                }
            }
            else
            {
                // 세로 3칸 × 가로 2칸 (본인열 + 전방1열), 본인 타일 제외
                for (int colOffset = 0; colOffset <= 1; colOffset++)
                {
                    int c = centerCol + dirCol * colOffset;
                    for (int r = centerRow - 1; r <= centerRow + 1; r++)
                    {
                        if (c == centerCol && r == centerRow) continue; // 본인 타일 제외
                        if (!BoardHelper.IsValidCombatPosition(c, r)) continue;
                        var pos = BoardWorldHelper.CombatGridToWorld(_boardIndex, c, r);
                        var fx = GetFromPool(type);
                        fx.transform.position = pos;
                        fxList.Add(fx);
                    }
                }
            }

            _activeEffects[handle] = new EffectGroup { Type = type, FxList = fxList };

            if (duration > 0f)
                _timedEffects.Add(new TimedEffect { Handle = handle, RemainTime = duration });

            return handle;
        }

        /// <summary>월드 좌표에 FX 표시.</summary>
        public int ShowAt(TileEffectType type, Vector3 worldPos, float duration = 0f)
        {
            return ShowAtInternal(type, worldPos, duration);
        }

        /// <summary>handle에 연결된 모든 FX 제거.</summary>
        public void Hide(int handle)
        {
            if (!_activeEffects.TryGetValue(handle, out var group))
                return;

            var pool = GetPool(group.Type);
            for (int i = 0; i < group.FxList.Count; i++)
            {
                var fx = group.FxList[i];
                if (fx != null)
                    pool.Release(fx);
            }
            group.FxList.Clear();
            _activeEffects.Remove(handle);
        }

        /// <summary>모든 활성 FX 제거.</summary>
        public void HideAll()
        {
            // 복사본으로 순회 (Hide에서 dictionary 수정하므로)
            var handles = new List<int>(_activeEffects.Keys);
            for (int i = 0; i < handles.Count; i++)
                Hide(handles[i]);

            _timedEffects.Clear();
        }

        // ── Update ──

        private void Update()
        {
            if (_timedEffects.Count == 0) return;

            float dt = Time.deltaTime;
            for (int i = _timedEffects.Count - 1; i >= 0; i--)
            {
                var timed = _timedEffects[i];
                timed.RemainTime -= dt;

                if (timed.RemainTime <= 0f)
                {
                    Hide(timed.Handle);
                    _timedEffects.RemoveAt(i);
                }
                else
                {
                    _timedEffects[i] = timed;
                }
            }
        }

        // ── 정리 ──

        private void OnDestroy()
        {
            HideAll();

            foreach (var pool in _pools.Values)
                pool.Clear();
            _pools.Clear();
        }

        // ── 내부 ──

        private int ShowAtInternal(TileEffectType type, Vector3 worldPos, float duration)
        {
            int handle = _nextHandle++;
            var fx = GetFromPool(type);
            fx.transform.position = worldPos;

            _activeEffects[handle] = new EffectGroup
            {
                Type = type,
                FxList = new List<GameObject> { fx },
            };

            if (duration > 0f)
                _timedEffects.Add(new TimedEffect { Handle = handle, RemainTime = duration });

            return handle;
        }

        private GameObject GetFromPool(TileEffectType type)
        {
            var pool = GetPool(type);
            return pool.Get();
        }

        private ObjectPool<GameObject> GetPool(TileEffectType type)
        {
            if (_pools.TryGetValue(type, out var pool))
                return pool;

            string path = AddressablePaths.TryGetValue(type, out var p) ? p : null;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[TileEffectManager] No addressable path for {type}");
                path = AddressablePaths[TileEffectType.Placement]; // fallback
            }

            pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Addressables.InstantiateAsync(path, _fxRoot).WaitForCompletion();
                    go.name = $"{type}_{go.GetInstanceID()}";
                    return go;
                },
                actionOnGet: go =>
                {
                    go.SetActive(true);
                    go.transform.SetParent(_fxRoot, false);
                },
                actionOnRelease: go =>
                {
                    go.SetActive(false);
                },
                actionOnDestroy: go =>
                {
                    Addressables.ReleaseInstance(go);
                });

            _pools[type] = pool;
            return pool;
        }
    }
}