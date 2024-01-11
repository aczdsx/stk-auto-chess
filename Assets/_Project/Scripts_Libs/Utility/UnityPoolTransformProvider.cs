using UnityEngine;

namespace CookApps.TeamBattle.Utility
{
    public class UnityPoolTransformProvider : SingletonMonoBehaviour<UnityPoolTransformProvider>
    {
        private Transform originsTr = null;
        public Transform OriginsTr => originsTr;
        private Transform poolTr = null;
        public Transform PoolTr => poolTr;

        public void Awake()
        {
            var origins = new GameObject("origins");
            originsTr = origins.transform;
            origins.transform.SetParent(transform);
            origins.SetActive(false);
            var pool = new GameObject("pool");
            poolTr = pool.transform;
            pool.transform.SetParent(transform);
        }
    }
}
