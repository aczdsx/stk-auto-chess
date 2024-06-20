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
            removeWaitingInGameVfxs.Clear(); // 여기에 든거는 runningEffects에도 들어있음.
            foreach (var effect in runningEffects)
            {
                InGameVfxPool.Return(effect);
            }
            runningEffects.Clear();
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
                InGameVfxPool.Return(effect);
                runningEffects.Remove(effect);
            }
        }

        #region Ingame Effect
        public void WarmUpInGameVfx(InGameVfxNameType vfxNameType, int warmUpCount = 1)
        {
            InGameVfxPool.WarmUp(vfxNameType, warmUpCount);
        }

        public InGameVfx AddInGameVfx(InGameVfxNameType vfxNameType, IFollowable parent)
        {
            Debug.LogColor($"vfxName : {vfxNameType}");
            var effect = InGameVfxPool.Get(vfxNameType, InGameObjectManager.Instance.Playground);
            addWaitingInGameVfxs.Enqueue(effect);
            effect.SetFollowable(parent);
            return effect;
        }

        public InGameVfx AddInGameVfx(InGameVfxNameType vfxNameType, Vector3 worldPosition)
        {
            var effect = InGameVfxPool.Get(vfxNameType, InGameObjectManager.Instance.Playground);
            addWaitingInGameVfxs.Enqueue(effect);
            effect.CachedTr.position = worldPosition;
            return effect;
        }

        public InGameVfx AddInGameTileFx(ElementType type, Vector3 worldPosition)
        {
            InGameVfxNameType vfxNameType = InGameVfxNameType.NONE;
            if (type == ElementType.DARK)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_darkness;
            }
            else if (type == ElementType.FIRE)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_fire;
            }
            else if (type == ElementType.WIND)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_water; // [TODO] wind 필요
            }
            else if (type == ElementType.LIGHT)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_light;
            }
            else if (type == ElementType.EARTH)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_earth;
            }
            else if (type == ElementType.WATER)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_water;
            }

            if (vfxNameType != InGameVfxNameType.NONE)
            {
                var effect = InGameVfxPool.Get(vfxNameType, InGameObjectManager.Instance.Playground);
                addWaitingInGameVfxs.Enqueue(effect);
                effect.CachedTr.position = worldPosition;
                return effect;
            }
            return null;
        }

        public InGameVfx AddInGamePreSkillActionFx(ElementType type, Vector3 worldPosition)
        {
            InGameVfxNameType vfxNameType = InGameVfxNameType.NONE;
            if (type == ElementType.DARK)
            {
                vfxNameType = InGameVfxNameType.fx_common_cast_darkness;
            }
            else if (type == ElementType.FIRE)
            {
                vfxNameType = InGameVfxNameType.fx_common_cast_fire;
            }
            else if (type == ElementType.WIND)
            {
                vfxNameType = InGameVfxNameType.fx_common_area_water; // [TODO] wind 필요
            }
            else if (type == ElementType.LIGHT)
            {
                vfxNameType = InGameVfxNameType.fx_common_cast_light;
            }
            else if (type == ElementType.EARTH)
            {
                vfxNameType = InGameVfxNameType.fx_common_cast_earth;
            }
            else if (type == ElementType.WATER)
            {
                vfxNameType = InGameVfxNameType.fx_common_cast_water;
            }

            if (vfxNameType != InGameVfxNameType.NONE)
            {
                var effect = InGameVfxPool.Get(vfxNameType, InGameObjectManager.Instance.Playground);
                addWaitingInGameVfxs.Enqueue(effect);
                effect.CachedTr.position = worldPosition;
                return effect;
            }
            return null;
        }

        public void RemoveInGameVfx(InGameVfx view)
        {
            view.CachedGo.SetActive(false);
            removeWaitingInGameVfxs.Enqueue(view);
        }

        private class InGameVfxPool
        {
            private static Dictionary<InGameVfxNameType, ObjectPool<InGameVfx>> pools = new ();
#if CHECK_POOL_LEAKING
            private static HashSet<InGameVfx> allActivatedVfxs = new ();
#endif

            internal static void WarmUp(InGameVfxNameType vfxNameType, int warmUpCount)
            {
                if (pools.ContainsKey(vfxNameType))
                    return;

                var pool = new ObjectPool<InGameVfx>(
                    () =>
                    {
                        var go = Addressables.InstantiateAsync(GetAddressablePath(vfxNameType)).WaitForCompletion();
                        return go.GetComponent<InGameVfx>();
                    },
                    actionOnDestroy: vfx => Addressables.ReleaseInstance(vfx.CachedGo)
                );

                for (var i = 0; i < warmUpCount; i++)
                {
                    var vfx = pool.Get();
                    pool.Release(vfx);
                }

                pools.Add(vfxNameType, pool);
            }

            private static int inc = 0;
            internal static InGameVfx Get(InGameVfxNameType vfxNameType, Transform parent)
            {
                if (!pools.TryGetValue(vfxNameType, out var pool))
                {

                    pool = new ObjectPool<InGameVfx>(
                        () =>
                        {
                            var go = Addressables.InstantiateAsync(GetAddressablePath(vfxNameType)).WaitForCompletion();
                            go.name = $"{vfxNameType}_{inc++}";
                            var vfx = go.GetComponent<InGameVfx>();
                            vfx.VfxNameType = vfxNameType;
                            return vfx;
                        },
#if CHECK_POOL_LEAKING
                        actionOnGet: vfx => allActivatedVfxs.Add(vfx),
                        actionOnRelease: vfx => allActivatedVfxs.Remove(vfx),
#endif
                        actionOnDestroy: vfx => Addressables.ReleaseInstance(vfx.CachedGo)
                    );
                    pools.Add(vfxNameType, pool);
                }

                var vfx = pool.Get();
                vfx.CachedTr.SetParent(parent, false);
                vfx.CachedGo.SetActive(true);
                return vfx;
            }

            internal static void Return(InGameVfx vfx)
            {
                vfx.Clear();
                vfx.CachedGo.SetActive(false);
                if (pools.TryGetValue(vfx.VfxNameType, out var pool))
                {
                    pool.Release(vfx);
                }
            }

            internal static void Clear()
            {
#if CHECK_POOL_LEAKING
                if (allActivatedVfxs.Count > 0)
                    Debug.LogError("!!! 인게임 이펙트 풀 누수 !!!");
#endif

                foreach (var pool in pools.Values)
                {
                    pool.Clear();
                }
                pools.Clear();
            }

            private static string GetAddressablePath(InGameVfxNameType vfxNameType)
            {
                Debug.LogColor($"vfxNameType : {vfxNameType}");
                return SpecDataManager.Instance.GetInGameVfxData(vfxNameType).addressable_path;
            }
        }
        #endregion
    }
}
