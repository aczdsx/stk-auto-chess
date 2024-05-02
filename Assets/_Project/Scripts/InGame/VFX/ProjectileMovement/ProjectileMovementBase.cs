using System;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public abstract class ProjectileMovementBase
    {
        public event Action OnReachedTarget = delegate { };

        protected InGameEffectViewProjectile EffectView;
        protected Vector3 srcPos;
        protected Vector3 destPos;
        protected ObfuscatorFloat speed;

        public virtual void SetData(InGameEffectViewProjectile effectView, Vector3 srcPos, Vector3 destPos, float speed)
        {
            this.EffectView = effectView;
            this.srcPos = srcPos;
            this.destPos = destPos;
            this.speed = speed;
        }

        public abstract void ManagedUpdate(float dt);

        public virtual void Clear()
        {
            EffectView = null;
        }

        public virtual void InvokeReachedTarget()
        {
            // onetime event
            OnReachedTarget?.Invoke();
            OnReachedTarget = delegate { };
        }
    }
}
