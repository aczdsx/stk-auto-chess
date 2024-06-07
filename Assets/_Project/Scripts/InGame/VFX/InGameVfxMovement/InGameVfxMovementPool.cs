using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace CookApps.BattleSystem
{
    public static class InGameVfxMovementPool
    {
        private static Dictionary<Type, LinkedPool<InGameVfxMovementBase>> pools = new ();

        public static T Get<T>() where T : InGameVfxMovementBase
        {
            Type type = typeof(T);

            if (!pools.TryGetValue(type, out var pool))
            {
                pool = new LinkedPool<InGameVfxMovementBase>(
                    InGameVfxMovementBase.Create<T>,
                    collectionCheck: false);
                pools.Add(type, pool);
            }

            return pool.Get() as T;
        }

        public static void Return<T>(T movement) where T : InGameVfxMovementBase
        {
            Type type = typeof(T);

            if (pools.TryGetValue(type, out var pool))
            {
                pool.Release(movement);
            }
        }
    }

}
