using System;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfx : CachedMonoBehaviour
    {
        private abstract class GenericDataContainerBase { }

        private class GenericDataContainer<T> : GenericDataContainerBase
        {
            private T data;
            public GenericDataContainer(T data) => this.data = data;
            public T GetData() => data;
        }

        public InGameVfxNameType VfxNameType { get; internal set; }
        protected InGameVfxMovementBase movement;
        protected bool cachedFlipX = false;

        public virtual void ManagedUpdate(float dt)
        {
            if (movement != null)
            {
                movement.ManagedUpdate(dt);
                CachedTr.localPosition = movement.CurrentPosition;
            }
            else
            {
                Follow();
            }
        }

        public virtual void Initialize(bool isFlipX, InGameVfxMovementBase movementBase = null)
        {
            if ((movement = movementBase) != null)
                CachedTr.localPosition = movement.CurrentPosition;
            SetFlipX(isFlipX);
        }

        public virtual void Clear()
        {
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
            InGameVfxManager.Instance.RemoveInGameVfx(this);
            if (movement != null)
            {
                InGameVfxMovementPool.Return(movement);
            }
        }


        #region CustomData
        private GenericDataContainerBase container;
        public void SetCustomData<T>(T data)
        {
            container = new GenericDataContainer<T>(data);
        }

        public T GetCustomData<T>()
        {
            if (this.container is GenericDataContainer<T> container)
            {
                return container.GetData();
            }
            return default;
        }
        #endregion

        #region Follow
        private IFollowable followee = null;
        private Vector3 offset;
        public void SetFollowable(IFollowable followee)
        {
            SetFollowable(followee, Vector3.zero);
        }

        public void SetFollowable(IFollowable followee, in Vector3 offset)
        {
            this.followee = followee;
            this.offset = offset;
            Follow();
        }

        private void Follow()
        {
            var hasFollowee = followee is { IsAlive: true };

            if (!hasFollowee)
                return;

            var position = followee.GetPosition() + offset;
            CachedTr.position = position;
        }
        #endregion
    }
}
