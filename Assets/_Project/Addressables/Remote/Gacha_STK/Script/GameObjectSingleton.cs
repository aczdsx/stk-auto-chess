using UnityEngine;

public class GameObjectSingleton<T> : MonoBehaviour where T : Component
{
    private static T _instance;

    private static bool _instanciated;

    public static bool Loaded
    {
        get
        {
            return _instance != null;
        }
    }
    
    public static T Instance
    {
        get
        {
            if (_instanciated)
            {
                return _instance;
            }

            if (_instance == null)
            {
                _instance = FindObjectOfType(typeof(T)) as T;
                if (_instance == null)
                {
                    GameObject obj = new GameObject();
                    // obj.hideFlags = HideFlags.HideAndDontSave;
                    _instance = obj.AddComponent<T>();
                }
            }

            _instanciated = true;

            return _instance;
        }
    }
    
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            _instanciated = true;
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"Singleton {typeof(T)} is already exists.");
            GameObject.Destroy(this);
        }
    }

    protected virtual void OnDestroy()
    {
        _instance = null;
        _instanciated = false;
    }
}
