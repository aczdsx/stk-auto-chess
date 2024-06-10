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

        [Flags]
        public enum CollisionType
        {
            None = 0,
            Enter = 0x001,
            Exit = 0x010,
            Stay = 0x100,
        }

        public event Action<CollisionType, InGameTile, InGameVfx> OnCollisionWithTile;
        public CollisionType CollisionMask { get; set; } = CollisionType.Enter | CollisionType.Exit;

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
        }

        public virtual void Initialize(bool isFlipX, InGameVfxMovementBase movementBase = null)
        {
            movement = movementBase;
            CachedTr.localPosition = movementBase?.CurrentPosition ?? Vector3.zero;
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
            InGameVfxManager.Instance.RemoveInGameVfx(this);
            if (movement != null)
            {
                InGameVfxMovementPool.Return(movement);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CollisionMask.HasFlag(CollisionType.Enter))
                return;

            if (!other.CompareTag("Slot"))
                return;

            var tileView = other.GetComponent<InGameTileView>();
            // UnityEngine.Debug.Log("OnCollisionEnter: " + tileView.name);
            var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
            OnCollisionWithTile?.Invoke(CollisionType.Enter, tile, this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!CollisionMask.HasFlag(CollisionType.Exit))
                return;

            if (!other.CompareTag("Slot"))
                return;

            var tileView = other.GetComponent<InGameTileView>();
            // UnityEngine.Debug.Log("OnCollisionExit: " + tileView.name);
            var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
            OnCollisionWithTile?.Invoke(CollisionType.Exit, tile, this);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!CollisionMask.HasFlag(CollisionType.Stay))
                return;

            if (!other.CompareTag("Slot"))
                return;

            var tileView = other.GetComponent<InGameTileView>();
            // UnityEngine.Debug.Log("OnCollisionStay: " + tileView.ID);
            var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
            OnCollisionWithTile?.Invoke(CollisionType.Stay, tile, this);
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
    }
}
