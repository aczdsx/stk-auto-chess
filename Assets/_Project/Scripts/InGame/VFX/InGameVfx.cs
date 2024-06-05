using System;
using CookApps.AutoBattler;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfx : CachedMonoBehaviour
    {
        public enum CollisionType
        {
            None,
            Enter,
            Exit,
            Stay
        }

        public event Action<CollisionType, InGameTile> OnCollisionWithTile;

        public string VfxName { get; internal set; }
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

        protected virtual void OnCollisionEnter2D(Collision2D other)
        {
            if (!other.transform.CompareTag("Slot"))
                return;

            var tileView = other.transform.GetComponent<InGameTileView>();
            var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
            OnCollisionWithTile?.Invoke(CollisionType.Enter, tile);
        }

        protected virtual void OnCollisionExit2D(Collision2D other)
        {
            if (!other.transform.CompareTag("Slot"))
                return;

            var tileView = other.transform.GetComponent<InGameTileView>();
            var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
            OnCollisionWithTile?.Invoke(CollisionType.Exit, tile);
        }

        protected virtual void OnCollisionStay2D(Collision2D other)
        {
            if (!other.transform.CompareTag("Slot"))
                return;

            var tileView = other.transform.GetComponent<InGameTileView>();
            var tile = InGameObjectManager.Instance.GetInGameTile(tileView.ID);
            OnCollisionWithTile?.Invoke(CollisionType.Stay, tile);
        }
    }
}
