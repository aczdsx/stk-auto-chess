using System.Collections.Generic;
using System.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace CookApps.TeamBattle
{
    public static class AddressableInstantiateHelper
    {
        private static Dictionary<GameObject, GameObject> instantiatedGameObjectMap = new ();
        private static Dictionary<GameObject, List<GameObject>> instantiatedGameObjects = new ();

        public static async UniTask<GameObject> InstantiateAsync(string assetLabel, Transform parent = null)
        {
            IList<IResourceLocation> resourceLocations = await Addressables.LoadResourceLocationsAsync(assetLabel, typeof(GameObject));
            if (resourceLocations.Count != 1)
            {
                throw new EvaluateException($"{assetLabel} is not single addressable!");
            }

            return await InstantiateAsync(resourceLocations[0], parent);
        }

        public static async UniTask<GameObject> InstantiateAsync(AssetReference assetRef, Transform parent = null)
        {
            IList<IResourceLocation> resourceLocations = await Addressables.LoadResourceLocationsAsync(assetRef, typeof(GameObject));
            if (resourceLocations.Count != 1)
            {
                throw new EvaluateException($"{assetRef.RuntimeKey} is not single addressable!");
            }

            return await InstantiateAsync(resourceLocations[0], parent);
        }

        private static async UniTask<GameObject> InstantiateAsync(IResourceLocation loc, Transform parent)
        {
            var origin = await AddressableLoadHelper.LoadAssetAsync<GameObject>(loc);
            GameObject go = Object.Instantiate(origin, parent);
            instantiatedGameObjectMap.Add(go, origin);
            instantiatedGameObjects.TryAdd(origin, new List<GameObject>());
            instantiatedGameObjects[origin].Add(go);
            return go;
        }

        public static void ReleaseGameObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (!instantiatedGameObjectMap.Remove(go, out GameObject origin))
            {
                return;
            }

            if (!instantiatedGameObjects.TryGetValue(origin, out List<GameObject> list))
            {
                return;
            }

            list.Remove(go);
            if (list.Count == 0)
            {
                AddressableLoadHelper.ReleaseLoadedAsset(origin);
            }
        }
    }
}
