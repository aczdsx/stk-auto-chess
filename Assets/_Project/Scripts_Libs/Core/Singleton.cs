using System;
using UnityEngine;

namespace CookApps.TeamBattle {
    /// <summary>
    /// 싱글톤
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static object _lock = new object();
        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// Lazy 싱글톤
    /// </summary>
    public class LazySingleton<T> where T : class, new()
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());
        public static T Instance
        {
            get
            {
                var inst = _instance.Value;
                lock (inst)
                {
                    return inst;
                }
            }
        }
    }

    /// <summary>
    /// 모노 상속 싱글톤
    /// </summary>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static bool _destroyed;
        private static T _instance;
        private static object _lock = new object();
        public static T Instance
        {
            get
            {
                if (_destroyed)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));
                        if (_instance == null)
                        {
                            var singleton = new GameObject(typeof(T).ToString());
                            _instance = singleton.AddComponent<T>();
                            DontDestroyOnLoad(singleton);
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _instance = null;
            _destroyed = true;
        }

        protected virtual void OnDestroy()
        {
            _instance = null;
            _destroyed = true;
        }

        public static bool IsAlive()
        {
            return !_destroyed;
        }
    }

    /// <summary>
    /// 모노 상속 lazy 싱글톤
    /// </summary>
    public class LazySingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static readonly Lazy<T> _instance = new Lazy<T>(() =>
        {
            T instance = (T)FindObjectOfType(typeof(T));
            if (instance == null)
            {
                GameObject singleton = new GameObject(typeof(T).ToString());
                instance = singleton.AddComponent<T>();
                DontDestroyOnLoad(singleton);
            }

            return instance;
        });

        public static T Instance
        {
            get
            {
                var inst = _instance.Value;
                lock (inst)
                {
                    return inst;
                }
            }
        }
    }
}
