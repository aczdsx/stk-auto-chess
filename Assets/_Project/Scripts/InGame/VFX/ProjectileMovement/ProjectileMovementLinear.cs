using UnityEngine;
using UnityEngine.Rendering;

namespace CookApps.BattleSystem
{
    public class ProjectileMovementLinear : ProjectileMovementBase
    {
        public static ProjectileMovementLinear Create()
        {
            return new ProjectileMovementLinear();
        }

        private Vector3 direction;

        public override void SetData(InGameEffectViewProjectile effectView, Vector3 srcPos, Vector3 destPos, float speed)
        {
            base.SetData(effectView, srcPos, destPos, speed);
            direction = destPos - srcPos;
        }

        public override void ManagedUpdate(float dt)
        {
            Vector3 position = EffectView.CachedTr.localPosition;
            Vector3 move = direction.normalized * dt * speed;

            Transform transform = EffectView.CachedTr;
            transform.localPosition = position + move;
        }
    }
}
