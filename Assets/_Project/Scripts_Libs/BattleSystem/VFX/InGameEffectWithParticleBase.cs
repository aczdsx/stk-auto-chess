using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameEffectWithParticleBase : InGameEffectBase
    {
        [SerializeField] private ParticleSystem particle;

        protected virtual ObfuscatorFloat Duration => 0.5f;

        protected virtual bool IsAutoRemove => false;

        private void Awake()
        {
            if (particle == null)
            {
                particle = GetComponent<ParticleSystem>();
            }
        }

        private float elapsedTime;
        protected bool isRemoved;

        public override void Initialize(Vector3 position /*, int soringOrder*/, bool isFlipX)
        {
            base.Initialize(position /*, soringOrder*/, isFlipX);

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

        public virtual void Clear()
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
            isRemoved = true;
            InGameObjectManager.Instance.RemoveInGameEffect(this);
            ReturnToPool();
        }

        protected virtual void ReturnToPool()
        {
        }
    }
}
