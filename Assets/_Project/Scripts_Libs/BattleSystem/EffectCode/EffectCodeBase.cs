using System;
using System.Collections.Generic;
using CookApps.Obfuscator;
using UnityEngine.Pool;
using UnityEngine.Profiling;

namespace CookApps.TeamBattle.BattleSystem
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
        protected ObfuscatorInt codeId;

        public int CodeId
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

        /// 초기화
        public virtual void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
        {
            CADebug.Assert(codeInfo.CodeId == codeId, "EffectCodeBase does not match codeId!!");
            this.codeInfo = codeInfo;
            this.container = container;
            this.source = source;
        }

        public virtual void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
        {
            CADebug.Assert(codeInfo.CodeId == codeId, "EffectCodeBase does not match codeId!! (Merge)");
            this.codeInfo = codeInfo;
            this.source = source;
        }

        public virtual void RemoveFromContainer()
        {
            if (container != null)
            {
                if (container.RemoveEffectCode(this))
                {
                }
            }
        }

        public virtual void OnPreRemoved()
        {
            container = null;
            source = null;
        }

        // 함수 캐싱
        public static Comparison<EffectCodeBase> SortByPriorityFunc = SortByPriority;

        private static int SortByPriority(EffectCodeBase x, EffectCodeBase y)
        {
            if (x.CodeInfo.Priority != y.CodeInfo.Priority)
            {
                return y.CodeInfo.Priority - x.CodeInfo.Priority;
            }

            return y.CodeId - x.CodeId;
        }
    }

    public class EffectCodeHelper
    {
        // 사용처: 우선순위 대로 호출하다 true 리턴하는 이펙트코드를 리턴받고 싶을 때
        public static T ReturnFirst<T>(List<T> effectCodes, Func<T, bool> lambda) where T : EffectCodeBase
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

        public static T ReturnFirst<T, R>(List<T> effectCodes, Func<T, R, bool> lambda, R arg) where T : EffectCodeBase
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

        // 사용처: 리턴값 없는 함수 & 인자가 없는 함수
        public static void Call<T>(IEnumerable<T> effectCodes, Action<T> lambda) where T : EffectCodeBase
        {
            foreach (T code in effectCodes)
            {
                lambda(code);
            }
        }

        // 사용처: 리턴값 없는 함수 & 인자가 있는 함수
        public static void CallWithArgs<R, T>(List<R> effectCodes, Action<R, T> lambda, T data) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                Profiler.BeginSample("CallWithArgs");
                lambda(effectCodes[i], data);
                Profiler.EndSample();
            }
        }

        // 사용처: 리턴값 없는 함수 & 인자가 있는 함수
        public static void CallWithArgs<R, T, Y>(List<R> effectCodes, Action<R, T, Y> lambda, T data1, Y data2) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                lambda(effectCodes[i], data1, data2);
            }
        }

        // 사용처: 리턴값 없는 함수 & 인자가 있는 함수
        public static void CallWithArgs<R, T, Y, U>(List<R> effectCodes, Action<R, T, Y, U> lambda, T data1, Y data2, U data3) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                lambda(effectCodes[i], data1, data2, data3);
            }
        }

        // 사용처: 리턴값을 머지하는데 함수 호출할 때 마다 리턴값을 머지하고 싶을 때
        public static T MergeEach<R, T>(List<R> effectCodes, Func<R, T> lambda, Func<T, T, T> merger, T initial) where R : EffectCodeBase
        {
            T res = initial;
            for (var i = 0; i < effectCodes.Count; i++)
            {
                T x = lambda(effectCodes[i]);
                res = merger(res, x);
            }

            return res;
        }

        // 사용처: 리턴값을 머지하는데 함수 모두 호출하고 리턴값을 한번에 머지하고 싶을 때
        public static T MergeOnce<R, T>(List<R> effectCodes, Func<R, T> lambda, Func<T[], T> merger) where R : EffectCodeBase
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

        // 사용처: 리턴값을 다음 이펙트 코드의 인자로 넣고 싶을 때
        public static T Passing<R, T>(List<R> effectCodes, Func<R, T, T> lambda, T initial) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                initial = lambda(effectCodes[i], initial);
            }

            return initial;
        }

        public static T Passing<R, T, Y>(List<R> effectCodes, Func<R, T, Y, T> lambda, T initial, Y additional) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                initial = lambda(effectCodes[i], initial, additional);
            }

            return initial;
        }

        public static T Passing<R, T, Y, U>(List<R> effectCodes, Func<R, T, Y, U, T> lambda, T initial, Y additional1, U additional2) where R : EffectCodeBase
        {
            for (var i = 0; i < effectCodes.Count; i++)
            {
                initial = lambda(effectCodes[i], initial, additional1, additional2);
            }

            return initial;
        }
    }
}
