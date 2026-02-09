using System;
using UnityEngine;

namespace CookApps.TeamBattle
{
    /// <summary>
    /// 싱글톤
    /// </summary>
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;
        private static object _lock = new ();

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
    /// 모노 상속 싱글톤
    /// </summary>
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static bool _destroyed = false;
        private static T _instance;
        private static object _lock = new ();

        // 정적 생성자로 _destroyed를 false로 보장
        static SingletonMonoBehaviour()
        {
            Debug.Log($"{typeof(T).Name} Static constructor called - setting _destroyed = false");
            _destroyed = false;
        }

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
                        Debug.Log($"{typeof(T).Name} Creating new instance");
                        _instance = (T) FindFirstObjectByType(typeof(T));
                        if (_instance == null)
                        {
                            var singleton = new GameObject(typeof(T).ToString());
                            _instance = singleton.AddComponent<T>();
                        }
                    }

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                _destroyed = false;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            Debug.Log($"{typeof(T).Name} OnApplicationQuit called - setting _destroyed = true");
            _instance = null;
            _destroyed = true;
        }

        protected virtual void OnDestroy()
        {
            Debug.Log($"{typeof(T).Name} OnDestroy called");
            _instance = null;
            _destroyed = true;
        }

        public static bool IsAlive()
        {
            return !_destroyed;
        }
    }
}
