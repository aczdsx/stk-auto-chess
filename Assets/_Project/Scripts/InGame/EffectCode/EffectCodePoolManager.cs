using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;

namespace CookApps.BattleSystem
{
    public class UseEffectCodeIdsAttribute : Attribute
    {
        public List<ObfuscatorInt> CodeIds { get; }

        public UseEffectCodeIdsAttribute(params int[] codeIds)
        {
            CodeIds = codeIds.Select(id => new ObfuscatorInt(id)).ToList();
        }
    }

    /// <summary>
    /// 이펙트 코드들을 재사용하는 풀링 매니저
    /// </summary>
    public partial class EffectCodePoolManager : Singleton<EffectCodePoolManager>
    {
        #region Effect Code Datas
        private Dictionary<long, long> codeIdToBaseCodeId = new ();
        private Dictionary<long, Queue<EffectCodeBase>> pools = new ();

        public void Clear()
        {
            pools.Clear();
        }

        public void RegisterCodeIdWithBaseCodeId(long codeId, long baseCodeId)
        {
            codeIdToBaseCodeId.TryAdd(codeId, baseCodeId);
        }

        private EffectCodeBase CreateEffectCode(long codeId)
        {
            // var res = CreateEffectCodeInternal(codeId);
            // if (res == null && codeIdToBaseCodeId.TryGetValue(codeId, out var baseCodeId))
            // {
            //     res = CreateEffectCodeInternal(baseCodeId);
            // }

            return null;
        }
        #endregion

        #region EffectCodeBase class Pooling
        public EffectCodeBase Get(long codeId)
        {
            EffectCodeBase codeBase;
            if (!pools.ContainsKey(codeId) || pools[codeId].Count <= 0)
            {
                codeBase = CreateEffectCode(codeId);
                if (codeBase == null)
                    return null;
                codeBase.CodeId = codeId;
            }
            else
            {
                var pool = pools[codeId];
                codeBase = pool.Dequeue();
            }
            return codeBase;
        }

        public T Get<T>(int codeId) where T : EffectCodeBase
        {
            T res;
            if (!pools.ContainsKey(codeId) || pools[codeId].Count <= 0)
            {
                var codeBase = CreateEffectCode(codeId);
                res = codeBase as T;
                if (res == null)
                    return null;

                res.CodeId = codeId;
            }
            else
            {
                var pool = pools[codeId];
                res = pool.Dequeue() as T;
            }
        
            return res;
        }

        public void Return(EffectCodeBase effectCode)
        {
            if (pools.ContainsKey(effectCode.CodeId))
            {
                pools[effectCode.CodeId].Enqueue(effectCode);
            }
            else
            {
                var pool = new Queue<EffectCodeBase>();
                pool.Enqueue(effectCode);
                pools.Add(effectCode.CodeId, pool);
            }
        }
        #endregion
    }
}
