using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using CookApps.TeamBattle;
using UnityEngine.Pool;
using UnityEngine.Profiling;

namespace CookApps.BattleSystem
{
    public static class EffectCodeTypeExtension
    {
        public static bool IsGlobalCode(this EffectCodeType type)
        {
            return type == EffectCodeType.Game;
        }

        public static bool IsStatCode(this EffectCodeType type)
        {
            return type == EffectCodeType.Stat;
        }

        public static bool IsCharacterCode(this EffectCodeType type)
        {
            return type == EffectCodeType.Character ||
                   type == EffectCodeType.Buff ||
                   type == EffectCodeType.Debuff ||
                   type == EffectCodeType.CrowdControl;
        }

        public static bool IsItemCode(this EffectCodeType type)
        {
            return type == EffectCodeType.Item;
        }
    }

    public interface IEffectCodeSource
    {
    }

    public abstract class EffectCodeBase
    {
        protected long codeId;

        public long CodeId
        {
            get => codeId;
            set => codeId = value;
        }

        public virtual EffectCodeType Type => EffectCodeType.Base;
        public virtual EffectCodeLifeType LifeType => EffectCodeLifeType.Instant;

        protected IEffectCodeSource source;
        public IEffectCodeSource Source => source;
        protected EffectCodeContainer container;
        public EffectCodeContainer Container => container;
        protected EffectCodeInfo codeInfo;
        public EffectCodeInfo CodeInfo => codeInfo;
        public virtual bool IsRemoveWithSource => true;

        /// <summary>
        /// 컨테이너에 AddOrMergeEffectCode가 호출되었을 때 컨테이너에 같은 codeId를 가진 이펙트코드가 없을 경우 호출됩니다.
        /// </summary>
        /// <param name="codeInfo"></param>
        /// <param name="container"></param>
        /// <param name="source"></param>
        public virtual void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            CADebug.Assert(codeInfo.CodeId == codeId, "EffectCodeBase does not match codeId!!");
            this.codeInfo = codeInfo;
            this.container = container;
            this.source = source;
        }

        /// <summary>
        /// 컨테이너에 AddOrMergeEffectCode가 호출되었을 때 컨테이너에 같은 codeId를 가진 이펙트코드가 있을 경우 호출됩니다.
        /// </summary>
        /// <param name="codeInfo"></param>
        /// <param name="source"></param>
        public virtual void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            CADebug.Assert(codeInfo.CodeId == codeId, "EffectCodeBase does not match codeId!! (Merge)");
            this.codeInfo = codeInfo;
            this.source = source;
        }

        public virtual bool TryRemoveWithSource(IEffectCodeSource source)
        {
            if (!IsRemoveWithSource)
                return false;

            return this.source == source;
        }

        /// <summary>
        /// 컨테이너에서 제거해야할 때 호출됩니다.
        /// </summary>
        public void RemoveFromContainer()
        {
            container?.RemoveEffectCode(this);
        }

        /// <summary>
        /// 제거 직전에 호출됩니다.
        /// 해제해야하는 것들을 해제해주세요.
        /// </summary>
        public virtual void OnPreRemoved()
        {
            container = null;
            source = null;
        }

        /// <summary>
        /// zeroalloc을 위한 함수 캐싱
        /// </summary>
        public static Comparison<EffectCodeBase> SortByPriorityFunc = SortByPriority;

        private static int SortByPriority(EffectCodeBase x, EffectCodeBase y)
        {
            if (x.CodeInfo.Priority != y.CodeInfo.Priority)
            {
                return y.CodeInfo.Priority - x.CodeInfo.Priority;
            }

            return (int)(y.CodeId - x.CodeId);
        }
    }

    public static class EffectCodeForLoopHelper
    {
        /// <summary>
        /// 사용처: 인자가 1개인 함수를 순서대로 호출하다 첫번째로 true 리턴하는 이펙트코드를 리턴받고 싶을 때
        /// </summary>
        public static T ReturnFirst<T>(IReadOnlyList<T> effectCodes, Func<T, bool> lambda) where T : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (lambda(effectCodes[i]))
                {
                    return effectCodes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 사용처: 인자가 2개인 함수를 순서대로 호출하다 첫번째로 true 리턴하는 이펙트코드를 리턴받고 싶을 때
        /// </summary>
        public static T ReturnFirst<T, R>(IReadOnlyList<T> effectCodes, Func<T, R, bool> lambda, R arg) where T : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                if (lambda(effectCodes[i], arg))
                {
                    return effectCodes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 사용처: 인자가 없고 리턴값 없는 함수 순회 호출
        /// </summary>
        public static void Call<T>(IReadOnlyList<T> effectCodes, Action<T> lambda) where T : EffectCodeBase
        {
            foreach (T code in effectCodes)
            {
                lambda(code);
            }
        }

        /// <summary>
        /// 사용처: 인자가 1개인 리턴값 없는 함수 순회 호출
        /// </summary>
        public static void CallWithArgs<R, T>(IReadOnlyList<R> effectCodes, Action<R, T> lambda, T data) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                Profiler.BeginSample("CallWithArgs");
                lambda(effectCodes[i], data);
                Profiler.EndSample();
            }
        }

        /// <summary>
        /// 사용처: 인자가 2개인 리턴값 없는 함수 순회 호출
        /// </summary>
        public static void CallWithArgs<R, T, Y>(IReadOnlyList<R> effectCodes, Action<R, T, Y> lambda, T data1, Y data2) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                lambda(effectCodes[i], data1, data2);
            }
        }

        /// <summary>
        /// 사용처: 인자가 3개인 리턴값 없는 함수 순회 호출
        /// </summary>
        public static void CallWithArgs<R, T, Y, U>(IReadOnlyList<R> effectCodes, Action<R, T, Y, U> lambda, T data1, Y data2, U data3) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                lambda(effectCodes[i], data1, data2, data3);
            }
        }

        /// <summary>
        /// 사용처: 리턴값을 머지하는데 함수 호출할 때 마다 리턴값을 머지하고 싶을 때
        /// </summary>
        public static T MergeEach<R, T>(IReadOnlyList<R> effectCodes, Func<R, T> lambda, Func<T, T, T> merger, T initial) where R : EffectCodeBase
        {
            T res = initial;
            for (var i = 0; i < effectCodes.Count; i++)
            {
                T x = lambda(effectCodes[i]);
                res = merger(res, x);
            }

            return res;
        }

        /// <summary>
        /// 사용처: 리턴값을 머지하는데 함수 모두 호출하고 리턴값을 한번에 머지하고 싶을 때
        /// </summary>
        public static T MergeOnce<R, T>(IReadOnlyList<R> effectCodes, Func<R, T> lambda, Func<T[], T> merger) where R : EffectCodeBase
        {
            var results = new T[effectCodes.Count];
            for (var i = 0; i < effectCodes.Count; i++)
            {
                T x = lambda(effectCodes[i]);
                results[i] = x;
            }

            T res = merger(results);
            return res;
        }

        /// <summary>
        /// 사용처: 리턴값을 다음 이펙트 코드의 인자로 넣고 싶을 때
        /// </summary>
        public static T Passing<R, T>(IReadOnlyList<R> effectCodes, Func<R, T, T> lambda, T initial) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                initial = lambda(effectCodes[i], initial);
            }

            return initial;
        }

        /// <summary>
        /// 사용처: 리턴값을 다음 이펙트 코드의 인자로 넣고 싶을 때
        /// </summary>
        public static T Passing<R, T, Y>(IReadOnlyList<R> effectCodes, Func<R, T, Y, T> lambda, T initial, Y additional) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                initial = lambda(effectCodes[i], initial, additional);
            }

            return initial;
        }

        /// <summary>
        /// 사용처: 리턴값을 다음 이펙트 코드의 인자로 넣고 싶을 때
        /// </summary>
        public static T Passing<R, T, Y, U>(IReadOnlyList<R> effectCodes, Func<R, T, Y, U, T> lambda, T initial, Y additional1, U additional2) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                initial = lambda(effectCodes[i], initial, additional1, additional2);
            }

            return initial;
        }
    }
}
