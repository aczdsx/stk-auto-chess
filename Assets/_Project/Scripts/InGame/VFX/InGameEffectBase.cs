using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameEffectBase : CachedMonoBehaviour, IInGameEffectView
    {
        protected bool cachedFlipX = false;

        public virtual void ManagedUpdate(float dt) { }

        public virtual void Initialize(Vector3 position /*, int sortingOrder*/, bool isFlipX)
        {
            CachedTr.localPosition = position;
            // SetSortingOrder(sortingOrder);
            SetFlipX(isFlipX);
        }

        public virtual void Restart() { }

        public virtual void SetFlipX(bool isFlipX)
        {
            if (cachedFlipX != isFlipX)
            {
                Vector3 scale = CachedTr.localScale;
                float x = scale.x;
                scale.x = isFlipX ? -Mathf.Abs(x) : Mathf.Abs(x);
                CachedTr.localScale = scale;
                cachedFlipX = isFlipX;
            }
        }

        public virtual void Remove()
        {
            InGameEffectManager.Instance.RemoveInGameEffect(this);
        }
    }
}
