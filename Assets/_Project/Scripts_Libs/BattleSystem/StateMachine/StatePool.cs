using System;
using System.Collections.Generic;

namespace CookApps.TeamBattle.BattleSystem
{
    internal class StatePool : Singleton<StatePool>
    {
        private Dictionary<Type, Queue<StateBase>> pools = new ();

        public void Clear()
        {
            foreach (KeyValuePair<Type, Queue<StateBase>> pool in pools)
            {
                while (pool.Value.Count > 0)
                {
                    pool.Value.Dequeue();
                }
            }
        }

        public T GetState<T>() where T : StateBase, new()
        {
            T state;
            Type type = typeof(T);
            if (pools.ContainsKey(type) && pools[type].Count > 0)
            {
                Queue<StateBase> pool = pools[type];
                state = pool.Dequeue() as T;
            }
            else
            {
                state = new T();
            }

            return state;
        }

        public StateBase GetState(Type stateType)
        {
            StateBase state;
            if (pools.ContainsKey(stateType) && pools[stateType].Count > 0)
            {
                Queue<StateBase> pool = pools[stateType];
                state = pool.Dequeue();
            }
            else
            {
                state = Activator.CreateInstance(stateType) as StateBase;
            }

            return state;
        }

        public void Push<T>(T state) where T : StateBase
        {
            Type type = typeof(T);
            if (pools.ContainsKey(type))
            {
                pools[type].Enqueue(state);
            }
            else
            {
                var pool = new Queue<StateBase>();
                pool.Enqueue(state);
                pools.Add(type, pool);
            }
        }
    }
}
