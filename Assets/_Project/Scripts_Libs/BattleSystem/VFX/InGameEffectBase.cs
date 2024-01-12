using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameEffectBase : CachedMonoBehaviour
    {
        // [SerializeField] private SortingGroup sortingGroup;

        // protected int sortingOrder;
        protected bool cachedFlipX = false;

        public virtual void SetSortingOrder(int sortingOrder)
        {
            // this.sortingOrder = sortingOrder + 10000;
            // sortingGroup.sortingOrder = sortingOrder;
        }

        public virtual void ManagedUpdate(float dt)
        {
            if (xFollowee != null || yFollowee != null)
            {
                Follow();
            }
        }

        public virtual void Initialize(Vector3 position /*, int sortingOrder*/, bool isFlipX)
        {
            CachedTr.localPosition = position;
            // SetSortingOrder(sortingOrder);
            SetFlipX(isFlipX);
        }

        public virtual void Restart()
        {
        }

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
        }

        #region Follow
        private IFollowable xFollowee = null;
        private float xOffset;
        private IFollowable yFollowee = null;
        private float yOffset;
        private int followSortOrderDiff;

        public void AddFollowable(IFollowable followable)
        {
            AddFollowable(followable, Vector2.zero, 3);
        }

        public void AddFollowable(IFollowable followable, Vector2 offset, int sortOrderDiff = 3)
        {
            xFollowee = followable;
            xOffset = offset.x;
            yFollowee = followable;
            yOffset = offset.y;
            followSortOrderDiff = sortOrderDiff;
        }

        public void AddXFollowable(IFollowable followable, float offset, int sortOrderDiff = 3)
        {
            xFollowee = followable;
            xOffset = offset;
            followSortOrderDiff = sortOrderDiff;
        }

        public void AddYFollowable(IFollowable followable, float offset, int sortOrderDiff = 3)
        {
            yFollowee = followable;
            yOffset = offset;
            followSortOrderDiff = sortOrderDiff;
        }

        private void Follow()
        {
            Vector3 position = CachedTr.localPosition;
            if (xFollowee != null)
            {
                position.x = xFollowee.GetPosition().x + xOffset;
                // SetSortingOrder(xFollowee.GetSortingLayerOrder() + followSortOrderDiff);
            }

            if (yFollowee != null)
            {
                position.y = yFollowee.GetPosition().y + yOffset;
                // SetSortingOrder(yFollowee.GetSortingLayerOrder() + followSortOrderDiff);
            }

            position.z = 0f;
            CachedTr.localPosition = position;
        }

        public Vector3 GetPosition()
        {
            return CachedTr.localPosition;
        }

        // public int GetSortingLayerOrder()
        // {
        //     return sortingOrder;
        // }
        #endregion
    }
}
