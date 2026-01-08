using System.Collections.Generic;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public static class DataBridgePool
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _dataBridges = new ();
        }
        
        static Dictionary<System.Type, Queue<object>> _dataBridges;
        
        public static T GetDataBridge<T>() where T : new()
        {
            Queue<object> queue;
            if (_dataBridges.TryGetValue(typeof(T), out queue))
            {
                if (queue.Count > 0)
                {
                    return (T)queue.Dequeue();
                }
            }

            return new T();
        }
        
        public static void ReleaseDataBridge<T>(T dataBridge)
        {
            Queue<object> queue;
            if (!_dataBridges.TryGetValue(typeof(T), out queue))
            {
                queue = new Queue<object>();
                _dataBridges[typeof(T)] = queue;
            }

            queue.Enqueue(dataBridge);
        }
    }
}
