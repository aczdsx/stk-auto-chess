using UnityEngine;
using UnityEngine.Rendering;

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
        }
    }
}
