using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxMovementLinear : InGameVfxMovementBase
    {
        private Vector3 direction;
        public override void SetData(Vector3 srcPos, Vector3 destPos, float speed)
        {
            base.SetData(srcPos, destPos, speed);
            direction = destPos - srcPos;
        }

        public override void ManagedUpdate(float dt)
        {
            Vector3 move = direction.normalized * dt * speed;
            prevPos = currPos;
            currPos += move;
            // check reached target
            if (Vector3.Dot(direction, currPos - srcPos) >= Vector3.Dot(direction, destPos - srcPos))
            {
                currPos = destPos; // 오버슈트 방지: destPos에서 정지
                InvokeReachedTarget();
            }
        }
    }
}
