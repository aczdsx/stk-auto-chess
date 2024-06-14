using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxWithParticle : InGameVfx
    {
        public ParticleSystem Particle => particle;
        [SerializeField] private ParticleSystem particle;

        protected ObfuscatorFloat Duration;

        protected bool IsAutoRemove;

        private void Awake()
        {
            if (particle == null)
            {
                particle = GetComponent<ParticleSystem>();
            }

            Duration = particle.main.duration;
            IsAutoRemove = !particle.main.loop;
        }

        private float elapsedTime;
        protected bool isRemoved;

        public override void Initialize(bool isFlipX, InGameVfxMovementBase movementBase = null)
        {
            base.Initialize(isFlipX, movementBase);

            if (particle != null)
            {
                particle.Stop();
                particle.Play();
            }

            Clear();
        }

        public override void Restart()
        {
            base.Restart();

            if (particle != null)
            {
                particle.Stop();
                particle.Play();
            }

            Clear();
        }

        public override void Clear()
        {
            elapsedTime = 0;
            isRemoved = false;
        }

        public override void ManagedUpdate(float dt)
        {
            base.ManagedUpdate(dt);
            if (!IsAutoRemove)
            {
                return;
            }

            elapsedTime += dt;
            if (elapsedTime > Duration)
            {
                AutoRemove();
            }
        }

        protected virtual void AutoRemove()
        {
            if (!IsAutoRemove)
            {
                return;
            }

            if (isRemoved)
            {
                return;
            }

            Remove();
        }

        public override void Remove()
        {
            base.Remove();
            isRemoved = true;
        }
    }
}
