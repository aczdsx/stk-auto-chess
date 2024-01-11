using UnityEngine;

namespace CookApps.TeamBattle
{
    public interface ICachedGameObject
    {
        GameObject CachedGo { get; }
    }

    public interface ICachedTransform
    {
        Transform CachedTr { get; }
    }

    public interface ICachedRectTransform
    {
        RectTransform CachedRectTr { get; }
    }

    public class CachedMonoBehaviour : MonoBehaviour, ICachedGameObject, ICachedTransform, ICachedRectTransform
    {
        private GameObject cachedGo = null;

        public GameObject CachedGo
        {
            get
            {
                cachedGo ??= gameObject;
                return cachedGo;
            }
        }

        private Transform cachedTr = null;

        public Transform CachedTr
        {
            get
            {
                cachedTr ??= transform;
                return cachedTr;
            }
        }

        private RectTransform cachedRectTr = null;

        public RectTransform CachedRectTr
        {
            get
            {
                cachedRectTr ??= GetComponent<RectTransform>();
                return cachedRectTr;
            }
        }

        protected virtual void OnDestroy()
        {
            cachedGo = null;
            cachedTr = null;
            cachedRectTr = null;
        }
    }
}
