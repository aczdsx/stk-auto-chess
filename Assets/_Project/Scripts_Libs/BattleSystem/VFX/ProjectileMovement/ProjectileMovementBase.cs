using System;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public enum ProjectileMovementType
    {
        Linear,
        Bezier,
    }

    public abstract class ProjectileMovementBase
    {
        public event Action OnReachedTarget = delegate { };

        protected InGameEffectProjectileBase effect;
        protected Vector3 srcPos;
        protected Vector3 destPos;
        protected ObfuscatorFloat speed;

        public virtual void SetData(InGameEffectProjectileBase effect, Vector3 srcPos, Vector3 destPos, float speed)
        {
            this.effect = effect;
            this.srcPos = srcPos;
            this.destPos = destPos;
            this.speed = speed;
        }

        public abstract void ManagedUpdate(float dt);

        public virtual void Clear()
        {
            effect = null;
        }

        public virtual void InvokeReachedTarget()
        {
            // onetime event
            OnReachedTarget?.Invoke();
            OnReachedTarget = delegate { };
        }
    }
}
