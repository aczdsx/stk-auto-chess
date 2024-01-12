using UnityEngine;
using UnityEngine.Rendering;

namespace CookApps.TeamBattle.BattleSystem
{
    public class ProjectileMovementLinear : ProjectileMovementBase
    {
        public static ProjectileMovementLinear Create()
        {
            return new ProjectileMovementLinear();
        }

        private Vector3 direction;

        public override void SetData(InGameEffectProjectileBase effect, Vector3 srcPos, Vector3 destPos, float speed)
        {
            base.SetData(effect, srcPos, destPos, speed);
            direction = destPos - srcPos;
        }

        public override void ManagedUpdate(float dt)
        {
            Vector3 position = effect.CachedTr.localPosition;
            Vector3 move = direction.normalized * dt * speed;

            Transform transform = effect.CachedTr;
            transform.localPosition = position + move;
        }
    }
}
