using UnityEngine;

public class DontDestroyObject : MonoBehaviour
{
    protected void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}