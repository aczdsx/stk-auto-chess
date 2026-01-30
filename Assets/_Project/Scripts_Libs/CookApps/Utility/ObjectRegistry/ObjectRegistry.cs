using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public enum RegistryKey
    {
        None,
        InGameCamera,
        MainCamera,
        CharacterCamera,
        CommanderSkillTrail,
        GuideAlert,
        EventSystem,
    }
    
    public interface IRegistrable
    {
        RegistryKey Key { get; }
    }
    
    /// <summary>
    /// 같은 키로 여러 오브젝트를 등록할 수 있으며,
    /// Get 시 활성화된 객체를 우선 반환하는 레지스트리.
    /// </summary>
    public class ObjectRegistry : Singleton<ObjectRegistry>
    {
        private readonly Dictionary<RegistryKey, List<IRegistrable>> _registry = new ();

        public static event Action<RegistryKey, IRegistrable> Registered;
        public static event Action<RegistryKey, IRegistrable> Unregistered;

        public static void Register(IRegistrable obj)
        {
            if (!Instance._registry.TryGetValue(obj.Key, out var list))
            {
                list = new List<IRegistrable>();
                Instance._registry[obj.Key] = list;
            }

            if (!list.Contains(obj))
            {
                list.Add(obj);
                Registered?.Invoke(obj.Key, obj);
            }
        }

        public static void Unregister(IRegistrable obj)
        {
            if (Instance._registry.TryGetValue(obj.Key, out var list))
            {
                if (list.Remove(obj))
                {
                    Unregistered?.Invoke(obj.Key, obj);
                }

                if (list.Count == 0)
                {
                    Instance._registry.Remove(obj.Key);
                }
            }
        }

        public static bool TryGetObject<T>(RegistryKey key, out T res) where T : class, IRegistrable
        {
            res = null;
            if (!Instance._registry.TryGetValue(key, out var list) || list.Count == 0)
                return false;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var obj = list[i];
                if (obj is MonoBehaviour { gameObject: { activeInHierarchy: true } })
                {
                    res = obj as T;
                    return true;
                }
            }

            return false;
        }
        
        public static bool TryGetObject(RegistryKey key, out RegisteredObject res)
        {
            return TryGetObject<RegisteredObject>(key, out res);
        }
        
        public static T GetObject<T>(RegistryKey key) where T : class, IRegistrable
        {
            if (TryGetObject<T>(key, out var res))
            {
                return res;
            }

            return null;
        }
    }
}
