using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// Prefab 키 기반 VFX GameObject 풀.
    /// Instantiate/Destroy 대신 SetActive on/off로 재사용하여 GC 압력 감소.
    /// </summary>
    public class VfxPool
    {
        private readonly Dictionary<int, Stack<GameObject>> _pools = new();
        private readonly Dictionary<int, ParticleSystem[]> _particleCache = new();
        private readonly Dictionary<int, TrailRenderer[]> _trailCache = new();
        private readonly Transform _poolRoot;

        public VfxPool(Transform poolRoot)
        {
            _poolRoot = poolRoot;
        }

        /// <summary>풀에서 VFX GO를 꺼내거나, 없으면 새로 Instantiate</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            int key = prefab.GetInstanceID();
            GameObject go = null;

            if (_pools.TryGetValue(key, out var stack))
            {
                while (stack.Count > 0)
                {
                    go = stack.Pop();
                    if (go != null) break;
                    go = null; // 파괴된 GO는 버리고 계속
                }
            }

            if (go == null)
            {
                go = Object.Instantiate(prefab, position, rotation);
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
                go.transform.localScale = prefab.transform.localScale;
            }

            if (parent != null)
                go.transform.SetParent(parent);
            else if (_poolRoot != null)
                go.transform.SetParent(_poolRoot);

            go.SetActive(true);
            ReplayParticles(go);
            return go;
        }

        /// <summary>VFX GO를 풀에 반환 (파괴하지 않고 비활성화)</summary>
        public void Return(GameObject go, GameObject prefab)
        {
            if (go == null) return;
            int key = prefab.GetInstanceID();

            StopParticles(go);
            ClearTrails(go);
            go.SetActive(false);
            go.transform.SetParent(_poolRoot);

            PushToStack(key, go);
        }

        /// <summary>캐시된 Particles/Trails로 풀에 반환 (GC 할당 회피)</summary>
        public void Return(GameObject go, GameObject prefab, ParticleSystem[] particles, TrailRenderer[] trails)
        {
            if (go == null) return;
            int key = prefab.GetInstanceID();

            if (particles != null)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if (particles[i] != null)
                        particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            if (trails != null)
            {
                for (int i = 0; i < trails.Length; i++)
                {
                    if (trails[i] != null) trails[i].Clear();
                }
            }
            go.SetActive(false);
            go.transform.SetParent(_poolRoot);

            PushToStack(key, go);
        }

        private void PushToStack(int key, GameObject go)
        {
            if (!_pools.TryGetValue(key, out var stack))
            {
                stack = new Stack<GameObject>();
                _pools[key] = stack;
            }
            stack.Push(go);
        }

        /// <summary>풀의 모든 GO를 파괴하고 비움</summary>
        public void Clear()
        {
            foreach (var stack in _pools.Values)
            {
                while (stack.Count > 0)
                {
                    var go = stack.Pop();
                    if (go != null) Object.Destroy(go);
                }
            }
            _pools.Clear();
            _particleCache.Clear();
            _trailCache.Clear();
        }

        private ParticleSystem[] GetCachedParticles(GameObject go)
        {
            int id = go.GetInstanceID();
            if (!_particleCache.TryGetValue(id, out var particles))
            {
                particles = go.GetComponentsInChildren<ParticleSystem>(true);
                _particleCache[id] = particles;
            }
            return particles;
        }

        private TrailRenderer[] GetCachedTrails(GameObject go)
        {
            int id = go.GetInstanceID();
            if (!_trailCache.TryGetValue(id, out var trails))
            {
                trails = go.GetComponentsInChildren<TrailRenderer>(true);
                _trailCache[id] = trails;
            }
            return trails;
        }

        public ParticleSystem[] GetParticles(GameObject go) => GetCachedParticles(go);
        public TrailRenderer[] GetTrails(GameObject go) => GetCachedTrails(go);

        private void ReplayParticles(GameObject go)
        {
            var particles = GetCachedParticles(go);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Clear();
                particles[i].Play();
            }
        }

        private void StopParticles(GameObject go)
        {
            var particles = GetCachedParticles(go);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void ClearTrails(GameObject go)
        {
            var trails = GetCachedTrails(go);
            for (int i = 0; i < trails.Length; i++)
            {
                trails[i].Clear();
            }
        }
    }
}
