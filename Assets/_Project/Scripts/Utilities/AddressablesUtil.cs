using UnityEngine.AddressableAssets;
using System;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public static class AddressablesUtil
    {
        public static bool IsValidKey(object key)
        {
            return Addressables.LoadResourceLocationsAsync(key).WaitForCompletion().Count > 0 ? true : false;
        }

        public static GameObject Instantiate(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            if (!IsValidKey(key))
            {
                UnityEngine.Debug.LogWarning($"Addressable key is null : {key}");
                return null;
            }
            return Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace, trackHandle).WaitForCompletion();
        }

        public static GameObject Instantiate(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            if (!IsValidKey(key))
            {
                UnityEngine.Debug.LogWarning($"Addressable key is null : {key}");
                return null;
            }
            return Addressables.InstantiateAsync(key,position,rotation,parent,trackHandle).WaitForCompletion();
        }

        public static T Load<T>(object key)
        {
            if (!IsValidKey(key)) throw new ArgumentException("addressable name not contain", key.ToString());
            return Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
        }


    }
}
