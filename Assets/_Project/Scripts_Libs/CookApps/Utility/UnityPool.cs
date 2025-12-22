using System;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CookApps.TeamBattle.Utility
{
    public class UnityPool<T> : Singleton<UnityPool<T>> where T : class, ICachedGameObject, ICachedTransform
    {
        private GameObject prefab;
        private IObjectPool<T> pool;

        private bool isInitialized = false;
        private int maxCapacity = int.MaxValue / 2;
        private int currentCapacity = 0;

        public void Initialize(GameObject prefab, int maxCapacity = int.MaxValue / 2)
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            this.maxCapacity = maxCapacity;
            currentCapacity = this.maxCapacity;
            this.prefab = prefab;
            var component = prefab.GetComponent<T>();
            if (component == null)
            {
                throw new Exception($"{typeof(T)} Pool given Prefab does not have {typeof(T)}!!");
            }

            pool = new LinkedPool<T>(CreatePooledItem, null, OnReturnedToPool, OnDestroyPoolObject);
        }

        public GameObject ClearPool()
        {
            pool?.Clear();
            pool = null;
            isInitialized = false;
            maxCapacity = int.MaxValue / 2;
            currentCapacity = 0;
            GameObject temp = prefab;
            prefab = null;
            return temp;
        }

        private T CreatePooledItem()
        {
            GameObject go = Object.Instantiate(prefab);
            return go.GetComponent<T>();
        }

        // Called when an item is returned to the pool using Release
        private void OnReturnedToPool(T obj)
        {
            obj.CachedGo.SetActive(false);
            obj.CachedTr.SetParent(UnityPoolTransformProvider.Instance.PoolTr, false);
        }

        // If the pool capacity is reached then any items returned will be destroyed.
        // We can control what the destroy behavior does, here we destroy the GameObject.
        private void OnDestroyPoolObject(T obj)
        {
            Object.Destroy(obj.CachedGo);
        }

        public T Get(Transform parent)
        {
            if (!isInitialized)
            {
                return null;
            }

            if (currentCapacity <= 0)
            {
                return null;
            }

            if (pool == null)
            {
                return null;
            }

            currentCapacity--;
            T poolObj = pool.Get();
            poolObj.CachedTr.SetParent(parent, false);
            if (ReferenceEquals(parent, null))
            {
                SceneManager.MoveGameObjectToScene(poolObj.CachedGo, SceneManager.GetActiveScene());
            }

            poolObj.CachedGo.SetActive(true);
            return poolObj;
        }

        public void Return(T poolObj)
        {
            if (!isInitialized)
            {
                return;
            }

            currentCapacity++;
            if (maxCapacity < currentCapacity)
            {
                currentCapacity = maxCapacity;
                Object.Destroy(poolObj.CachedGo);
                return;
            }

            pool.Release(poolObj);
        }
    }
}
