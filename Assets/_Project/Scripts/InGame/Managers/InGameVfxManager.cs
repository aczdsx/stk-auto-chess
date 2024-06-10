#define CHECK_POOL_LEAKING
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;

namespace CookApps.BattleSystem
{
    public class InGameVfxManager : Singleton<InGameVfxManager>
    {
        private List<InGameVfx> runningEffects = new ();
        private Queue<InGameVfx> addWaitingInGameVfxs = new ();
        private Queue<InGameVfx> removeWaitingInGameVfxs = new ();

        public void Initialize()
        {
            InGameMainFlowManager.Instance.AddUpdateListener(InGameMainFlowManager.UpdatePriority_Objects, ManagedUpdate);
            InGameMainFlowManager.Instance.AddLateUpdateListener(InGameMainFlowManager.UpdatePriority_Objects, LateManagedUpdate);
        }

        public void Clear()
        {
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
            InGameMainFlowManager.Instance.RemoveLateUpdateListener(LateManagedUpdate);
            foreach (InGameVfx inGameVfx in addWaitingInGameVfxs)
            {
                InGameVfxPool.Return(inGameVfx);
            }
            addWaitingInGameVfxs.Clear();
            foreach (var effect in runningEffects)
            {
                InGameVfxPool.Return(effect);
            }
            runningEffects.Clear();
            removeWaitingInGameVfxs.Clear();
            InGameVfxPool.Clear();
        }

        private void ManagedUpdate(float dt)
        {
            for (var i = 0; i < runningEffects.Count; i++)
            {
                runningEffects[i].ManagedUpdate(dt);
            }
        }

        private void LateManagedUpdate(float dt)
        {
            while (addWaitingInGameVfxs.Count > 0)
            {
                InGameVfx effect = addWaitingInGameVfxs.Dequeue();
                runningEffects.Add(effect);
            }

            while (removeWaitingInGameVfxs.Count > 0)
            {
                InGameVfx effect = removeWaitingInGameVfxs.Dequeue();
                runningEffects.Remove(effect);
            }
        }

        #region Ingame Effect
        public void WarmUpInGameVfx(InGameVfxName vfxName, int warmUpCount = 1)
        {
            InGameVfxPool.WarmUp(vfxName, warmUpCount);
        }

        public InGameVfx AddInGameVfx(InGameVfxName vfxName, Transform parent)
        {
            var effect = InGameVfxPool.Get(vfxName, parent);
            addWaitingInGameVfxs.Enqueue(effect);
            return effect;
        }

        public void RemoveInGameVfx(InGameVfx view)
        {
            InGameVfxPool.Return(view);
            removeWaitingInGameVfxs.Enqueue(view);
        }

        private class InGameVfxPool
        {
            private static Dictionary<InGameVfxName, ObjectPool<InGameVfx>> pools = new ();
#if CHECK_POOL_LEAKING
            private static HashSet<InGameVfx> allRunningVfxs = new ();
#endif

            internal static void WarmUp(InGameVfxName vfxName, int warmUpCount)
            {
                if (pools.ContainsKey(vfxName))
                    return;

                var pool = new ObjectPool<InGameVfx>(
                    () =>
                    {
                        var go = Addressables.InstantiateAsync(GetAddressablePath(vfxName)).WaitForCompletion();
                        return go.GetComponent<InGameVfx>();
                    },
                    actionOnDestroy: vfx => Addressables.ReleaseInstance(vfx.CachedGo)
                );

                for (var i = 0; i < warmUpCount; i++)
                {
                    var vfx = pool.Get();
                    pool.Release(vfx);
                }

                pools.Add(vfxName, pool);
            }

            internal static InGameVfx Get(InGameVfxName vfxName, Transform parent)
            {
                if (!pools.TryGetValue(vfxName, out var pool))
                {

                    pool = new ObjectPool<InGameVfx>(
                        () =>
                        {
                            var go = Addressables.InstantiateAsync(GetAddressablePath(vfxName)).WaitForCompletion();
                            var vfx = go.GetComponent<InGameVfx>();
                            vfx.VfxName = vfxName;
                            return vfx;
                        },
#if CHECK_POOL_LEAKING
                        actionOnGet: vfx => allRunningVfxs.Add(vfx),
                        actionOnRelease: vfx => allRunningVfxs.Remove(vfx),
#endif
                        actionOnDestroy: vfx => Addressables.ReleaseInstance(vfx.CachedGo)
                    );
                    pools.Add(vfxName, pool);
                }

                var vfx = pool.Get();
                vfx.CachedTr.SetParent(parent, false);
                vfx.CachedGo.SetActive(true);
                return vfx;
            }

            internal static void Return(InGameVfx vfx)
            {
                vfx.CachedGo.SetActive(false);
                if (pools.TryGetValue(vfx.VfxName, out var pool))
                {
                    pool.Release(vfx);
                }
            }

            internal static void Clear()
            {
#if CHECK_POOL_LEAKING
                if (allRunningVfxs.Count > 0)
                    Debug.LogError("!!! 인게임 이펙트 풀 누수 !!!");
#endif

                foreach (var pool in pools.Values)
                {
                    pool.Clear();
                }
                pools.Clear();
            }

            private static string GetAddressablePath(InGameVfxName vfxName)
            {
                return SpecDataManager.Instance.GetInGameVfxData(vfxName).addressable_path;
            }
        }
        #endregion
    }
}
