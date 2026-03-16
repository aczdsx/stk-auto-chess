using System;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public abstract class InGameVfxMovementBase
    {
        /// <summary>
        /// 이동 완료 시 호출. VFX 체인(폭발 이펙트 등) 전용.
        /// 데미지/상태이상 등 게임로직을 절대 등록하지 말 것 — 시뮬레이션 결정론이 깨짐.
        /// </summary>
        public event Action OnReachedTarget = null;

        protected Vector3 srcPos;
        protected Vector3 destPos;
        protected Vector3 prevPos;
        protected Vector3 currPos;
        protected ObfuscatorFloat speed;
        public Vector3 CurrentPosition => currPos;
        public Vector3 PreviousPosition => prevPos;
        public Vector3 TargetPosition => destPos;

        /// <summary>
        /// speed가 1이면 1초에 1만큼 이동.
        /// </summary>
        public virtual void SetData(Vector3 srcPos, Vector3 destPos, float speed)
        {
            this.srcPos = prevPos = currPos = srcPos;
            this.destPos = destPos;
            this.speed = speed;
        }

        public virtual void Clear()
        {
            OnReachedTarget = null;
        }

        public abstract void ManagedUpdate(float dt);

        public virtual void InvokeReachedTarget()
        {
            // onetime event
            OnReachedTarget?.Invoke();
            OnReachedTarget = null;
        }

        public static InGameVfxMovementBase Create<T>() where T : InGameVfxMovementBase
        {
            return (InGameVfxMovementBase)Activator.CreateInstance(typeof(T));
        }
    }
}
