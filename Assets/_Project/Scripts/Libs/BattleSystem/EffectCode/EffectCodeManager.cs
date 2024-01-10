using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.EffectCode
{
    public class EffectCodeManager : Singleton<EffectCodeManager>
    {
        #region Effect Code Datas
        private Dictionary<int, Func<EffectCodeBase>> effectCodeClassDatas = new ();
        private Dictionary<int, EffectCodeType> effectCodeTypeDatas = new ();
        private Dictionary<int, EffectCodeLifeType> effectCodeLifeTypeDatas = new ();
        private Dictionary<int, Queue<EffectCodeBase>> pools = new ();

        public void Clear()
        {
            effectCodeClassDatas.Clear();
            effectCodeTypeDatas.Clear();
            effectCodeLifeTypeDatas.Clear();
            pools.Clear();
        }

        public void LoadEffectCodeClassDatas()
        {
            effectCodeClassDatas.Clear();
            effectCodeTypeDatas.Clear();
            effectCodeLifeTypeDatas.Clear();

            Type baseType = typeof(EffectCodeBase);
            IEnumerable<Type> allEffectCodeImpls = GetType().Assembly.GetTypes().Where(x => x.IsSubclassOf(baseType) && !x.IsAbstract);
            foreach (Type effectCodeImpl in allEffectCodeImpls)
            {
                FieldInfo codeIdFieldInfo = effectCodeImpl.GetField("UseCodeIds");
                var codeIds = (List<ObfuscatorInt>) codeIdFieldInfo.GetValue(null);
                NewExpression constructorExpression = Expression.New(effectCodeImpl);
                var lambdaExpression = Expression.Lambda<Func<EffectCodeBase>>(constructorExpression);
                var createHeadersFunc = lambdaExpression.Compile();
                foreach (ObfuscatorInt codeId in codeIds)
                {
                    AddEffectCodeCreator(codeId, createHeadersFunc);
                }
            }
        }

        public void AddEffectCodeCreator(int codeId, Func<EffectCodeBase> lambda)
        {
            effectCodeClassDatas.Add(codeId, lambda);
            var temp = lambda.Invoke();
            effectCodeTypeDatas.Add(codeId, temp.Type);
            effectCodeLifeTypeDatas.Add(codeId, temp.LifeType);
        }

        public EffectCodeType GetEffectCodeType(int codeId)
        {
            EffectCodeType type;
            effectCodeTypeDatas.TryGetValue(codeId, out type);
            return type;
        }

        public EffectCodeLifeType GetEffectCodeLifeType(int codeId)
        {
            EffectCodeLifeType type;
            effectCodeLifeTypeDatas.TryGetValue(codeId, out type);
            return type;
        }
        #endregion

        #region EffectCodeBase class Pooling
        public EffectCodeBase GetEffectCodeBase(int codeId)
        {
            if (!effectCodeClassDatas.ContainsKey(codeId))
            {
                return null;
            }

            EffectCodeBase codeBase;
            if (!pools.ContainsKey(codeId) || pools[codeId].Count <= 0)
            {
                codeBase = effectCodeClassDatas[codeId].Invoke();
                codeBase.CodeId = codeId;
            }
            else
            {
                var pool = pools[codeId];
                codeBase = pool.Dequeue();
            }

            return codeBase;
        }

        public T GetEffectCodeBase<T>(int codeId) where T : EffectCodeBase
        {
            if (!effectCodeClassDatas.ContainsKey(codeId))
            {
                return null;
            }

            T res;
            if (!pools.ContainsKey(codeId) || pools[codeId].Count <= 0)
            {
                var codeBase = effectCodeClassDatas[codeId].Invoke();
                codeBase.CodeId = codeId;
                res = codeBase as T;
            }
            else
            {
                var pool = pools[codeId];
                res = pool.Dequeue() as T;
            }

            if (res == null)
            {
                Push(res);
            }

            return res;
        }

        public void Push(EffectCodeBase effectCode)
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
