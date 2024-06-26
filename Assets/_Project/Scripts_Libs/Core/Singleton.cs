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
    /// Lazy 싱글톤
    /// </summary>
    public class LazySingleton<T> where T : class, new()
    {
        private static readonly Lazy<T> _instance = new (() => new T());

        public static T Instance
        {
            get
            {
                T inst = _instance.Value;
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
        private static object _lock = new ();

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
                        _instance = (T) FindObjectOfType(typeof(T));
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
        private static readonly Lazy<T> _instance = new (() =>
        {
            var instance = (T) FindObjectOfType(typeof(T));
            if (instance == null)
            {
                var singleton = new GameObject(typeof(T).ToString());
                instance = singleton.AddComponent<T>();
                DontDestroyOnLoad(singleton);
            }

            return instance;
        });

        public static T Instance
        {
            get
            {
                T inst = _instance.Value;
                lock (inst)
                {
                    return inst;
                }
            }
        }
    }

    public abstract class GameObjectSingletonBase : MonoBehaviour
{
    public abstract bool AllowMultiInstance { get; }
    public abstract void SetAsSingleton(bool set);
}

public class GameObjectSingleton<T> : GameObjectSingletonBase where T : GameObjectSingleton<T>
{
    public static bool Loaded
    {
        get
        {
            return __inst != null && __inst.Valid;
        }
    }

    public static T Instance
    {
        get { return __inst; }
    }

    protected static T _inst
    {
        get
        {
            return __inst;
        }
    }

    /////////////////////////////////////////////////////////////////
    // instance member
    public override bool AllowMultiInstance
    {
        get
        {
            return false;
        }
    }

    public override void SetAsSingleton(bool set)
    {
        if (!this.AllowMultiInstance)
        {
            Debug.LogError(typeof(T).Name + " : AllowMultiInstance is false");
            return;
        }

        if (set)
        {
            if (object.ReferenceEquals(__inst, this))
                return;

            __inst = this as T;
            this.OnAttached();
        }
        else
        {
            if (!object.ReferenceEquals(__inst, this))
                return;

            __inst = null;
            this.OnDetached();
        }
    }

    protected virtual bool Valid
    {
        get
        {
            return __inst != null;
        }
    }

    protected virtual void Awake()
    {
        if (__inst != null)
        {
            if (!this.AllowMultiInstance)
                Debug.LogError(typeof(T).Name + " is already attached");

            return;
        }

        __inst = this as T;

        this.OnAttached();
    }

    protected virtual void OnDestroy()
    {
        if (object.ReferenceEquals(this, __inst))
        {
            __inst = null;
            this.OnDetached();
        }
    }

    protected virtual void OnAttached()
    {

    }

    protected virtual void OnDetached()
    {

    }

    public static void SetAsSingleton(GameObject obj, bool set)
    {
        foreach (GameObjectSingletonBase comp in obj.GetComponents<GameObjectSingletonBase>())
            comp.SetAsSingleton(set);
    }

    /////////////////////////////////////////////////////////////////
    // private
    protected static T __inst = null;
}

}
