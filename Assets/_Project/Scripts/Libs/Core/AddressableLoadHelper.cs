using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle
{
    public static class AddressableLoadHelper
    {
        private static Dictionary<string, Object> cachedObjects = new ();
        private static HashSet<string> runningOperationKeys = new ();

        public static async UniTask<T> LoadAssetAsync<T>(string assetLabel) where T : Object
        {
            var resourceLocations = await Addressables.LoadResourceLocationsAsync(assetLabel, typeof(T));
            if (resourceLocations.Count != 1)
                throw new EvaluateException($"{assetLabel} is not single addressable!");

            return await LoadAssetAsync<T>(resourceLocations[0]);
        }

        public static async UniTask<T> LoadAssetAsync<T>(AssetReference assetRef) where T : Object
        {
            var resourceLocations = await Addressables.LoadResourceLocationsAsync(assetRef, typeof(T));
            if (resourceLocations.Count != 1)
                throw new EvaluateException($"{assetRef.RuntimeKey} is not single addressable!");

            return await LoadAssetAsync<T>(resourceLocations[0]);
        }

        internal static async UniTask<T> LoadAssetAsync<T>(IResourceLocation location) where T : Object
        {
            {
                if (cachedObjects.TryGetValue(location.InternalId, out var cachedObj))
                {
                    return cachedObj as T;
                }
            }

            if (!runningOperationKeys.Add(location.InternalId))
            {
                await UniTask.WaitUntil(() => !runningOperationKeys.Contains(location.InternalId));

                if (cachedObjects.TryGetValue(location.InternalId, out var cachedObj))
                {
                    return cachedObj as T;
                }
                else
                {
                    throw new Exception("Fail Load addressable from cached!");
                }
            }

            try
            {
                var oper = Addressables.LoadAssetAsync<T>(location);
                var res = await oper;
                cachedObjects.Add(location.InternalId, res);
                runningOperationKeys.Remove(location.InternalId);
                return res;
            }
            catch
            {
                runningOperationKeys.Remove(location.InternalId);
                throw;
            }
        }

        public static void ReleaseLoadedAsset<T>(T asset) where T : Object
        {
            if (asset == null)
                return;

            foreach (var pair in cachedObjects)
            {
                if (pair.Value == asset)
                {
                    cachedObjects.Remove(pair.Key);
                    Addressables.Release(asset);
                    return;
                }
            }
        }

        public static void ReleaseAll(string containKey)
        {
            var allKey = cachedObjects.Keys.ToArray();
            foreach (var key in allKey)
            {
                if (!key.Contains(containKey))
                    continue;

                var cachedObj = cachedObjects[key];
                cachedObjects.Remove(key);
                Addressables.Release(cachedObj);
            }
        }
    }
}
