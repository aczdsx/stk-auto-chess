using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfx : CachedMonoBehaviour
    {
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
                InGameVfxMovementPool.Release(movement);
            }
        }
    }
}
