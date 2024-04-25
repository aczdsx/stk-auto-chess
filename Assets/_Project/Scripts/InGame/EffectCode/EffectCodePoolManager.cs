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
    public class EffectCodePoolManager : Singleton<EffectCodePoolManager>
    {
        #region Effect Code Datas
        private Dictionary<int, Func<EffectCodeBase>> effectCodeCreators = new ();
        private Dictionary<int, EffectCodeType> effectCodeTypeMap = new ();
        private Dictionary<int, EffectCodeLifeType> effectCodeLifeTypeMap = new ();
        private Dictionary<int, Queue<EffectCodeBase>> pools = new ();

        public void Clear()
        {
            effectCodeCreators.Clear();
            effectCodeTypeMap.Clear();
            effectCodeLifeTypeMap.Clear();
            pools.Clear();
        }

        public void RegisterCodeId(int codeId, Type effectCodeImpl)
        {
            NewExpression constructorExpression = Expression.New(effectCodeImpl);
            Expression<Func<EffectCodeBase>> lambdaExpression = Expression.Lambda<Func<EffectCodeBase>>(constructorExpression);
            Func<EffectCodeBase> createHeadersFunc = lambdaExpression.Compile();
            AddEffectCodeCreator(codeId, createHeadersFunc);
        }

        public void RegisterAttributedCodeIds()
        {
            IEnumerable<Type> allEffectCodeImpls = InheritHelper.GetAllImplementations<EffectCodeBase>();
            foreach (Type effectCodeImpl in allEffectCodeImpls)
            {
                IEnumerable<ObfuscatorInt> codeIds = effectCodeImpl.GetCustomAttributes<UseEffectCodeIdsAttribute>().SelectMany(x => x.CodeIds);
                NewExpression constructorExpression = Expression.New(effectCodeImpl);
                Expression<Func<EffectCodeBase>> lambdaExpression = Expression.Lambda<Func<EffectCodeBase>>(constructorExpression);
                Func<EffectCodeBase> createHeadersFunc = lambdaExpression.Compile();
                foreach (ObfuscatorInt codeId in codeIds)
                {
                    AddEffectCodeCreator(codeId, createHeadersFunc);
                }
            }
        }

        private void AddEffectCodeCreator(int codeId, Func<EffectCodeBase> lambda)
        {
            if (!effectCodeCreators.TryAdd(codeId, lambda))
            {
                CADebug.LogError($"EffectCodePoolManager.RegisterCodeId - Already registered codeId {codeId}");
                return;
            }

            EffectCodeBase temp = lambda.Invoke();
            effectCodeTypeMap.Add(codeId, temp.Type);
            effectCodeLifeTypeMap.Add(codeId, temp.LifeType);
        }

        public EffectCodeType GetEffectCodeType(int codeId)
        {
            effectCodeTypeMap.TryGetValue(codeId, out EffectCodeType type);
            return type;
        }

        public EffectCodeLifeType GetEffectCodeLifeType(int codeId)
        {
            effectCodeLifeTypeMap.TryGetValue(codeId, out EffectCodeLifeType type);
            return type;
        }
        #endregion

        #region EffectCodeBase class Pooling
        public EffectCodeBase GetEffectCodeBase(int codeId)
        {
            if (!effectCodeCreators.ContainsKey(codeId))
            {
                return null;
            }

            EffectCodeBase codeBase;
            if (!pools.ContainsKey(codeId) || pools[codeId].Count <= 0)
            {
                codeBase = effectCodeCreators[codeId].Invoke();
                codeBase.CodeId = codeId;
            }
            else
            {
                Queue<EffectCodeBase> pool = pools[codeId];
                codeBase = pool.Dequeue();
            }

            return codeBase;
        }

        public T GetEffectCodeBase<T>(int codeId) where T : EffectCodeBase
        {
            if (!effectCodeCreators.ContainsKey(codeId))
            {
                return null;
            }

            EffectCodeBase effectCode;
            if (!pools.ContainsKey(codeId) || pools[codeId].Count <= 0)
            {
                effectCode = effectCodeCreators[codeId].Invoke();
                effectCode.CodeId = codeId;
            }
            else
            {
                Queue<EffectCodeBase> pool = pools[codeId];
                effectCode = pool.Dequeue();
            }

            if (effectCode is T res)
            {
                return res;
            }

            Push(effectCode);
            throw new Exception($"{codeId} is not {typeof(T).Name}");
        }

        public void Push(EffectCodeBase effectCode)
        {
            if (pools.TryGetValue(effectCode.CodeId, out Queue<EffectCodeBase> pool))
            {
                pool.Enqueue(effectCode);
                return;
            }

            pool = new Queue<EffectCodeBase>();
            pool.Enqueue(effectCode);
            pools.Add(effectCode.CodeId, pool);
        }
        #endregion
    }
}
