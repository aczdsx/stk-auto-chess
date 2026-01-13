using System;
using CookApps.Obfuscator;
using UnityEngine;

namespace CookApps.BattleSystem
{
    public class InGameVfxWithAnimation : InGameVfx
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationEventListener animationEventListener;
        
        private bool isAutoRemove = true;
        private bool isRemoved = false;
        private float animationDuration = 0f;
        private float elapsedTime = 0f;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animationEventListener == null)
            {
                animationEventListener = GetComponent<AnimationEventListener>();
                if (animationEventListener == null)
                {
                    animationEventListener = gameObject.AddComponent<AnimationEventListener>();
                }
            }

            // AnimationEventListener 이벤트 구독
            animationEventListener.OnAnimationEvent += OnAnimationEventReceived;
        }

        protected override void OnDestroy()
        {
            // 이벤트 구독 해제
            if (animationEventListener != null)
            {
                animationEventListener.OnAnimationEvent -= OnAnimationEventReceived;
            }
            base.OnDestroy();
        }

        public override void Initialize(bool isFlipX, InGameVfxMovementBase movementBase = null)
        {
            base.Initialize(isFlipX, movementBase);

            if (animator != null)
            {
                // Animator 초기화 및 애니메이션 재생
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                
                // 현재 애니메이션 클립의 길이 가져오기
                var clipInfos = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfos.Length > 0)
                {
                    var clip = clipInfos[0].clip;
                    animationDuration = clip.length;
                    isAutoRemove = !clip.isLooping;
                }
                else
                {
                    // 클립 정보가 없으면 Animator의 첫 번째 상태에서 길이 가져오기
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    animationDuration = stateInfo.length;
                    isAutoRemove = !stateInfo.loop;
                }
            }

            Clear();
        }

        public override void Restart()
        {
            base.Restart();

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
                
                var clipInfos = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfos.Length > 0)
                {
                    var clip = clipInfos[0].clip;
                    animationDuration = clip.length;
                    isAutoRemove = !clip.isLooping;
                }
                else
                {
                    var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                    animationDuration = stateInfo.length;
                    isAutoRemove = !stateInfo.loop;
                }
            }

            Clear();
        }

        public override void Clear()
        {
            elapsedTime = 0f;
            isRemoved = false;
        }

        public override void ManagedUpdate(float dt)
        {
            base.ManagedUpdate(dt);
            
            if (!isAutoRemove)
            {
                return;
            }

            elapsedTime += dt;
            if (elapsedTime > animationDuration)
            {
                AutoRemove();
            }
        }

        protected virtual void AutoRemove()
        {
            if (!isAutoRemove)
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
            
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        /// <summary>
        /// 애니메이션 이벤트를 받아서 처리하는 메서드
        /// </summary>
        private void OnAnimationEventReceived(AnimationEventKey eventKey)
        {
            switch (eventKey)
            {
                case AnimationEventKey.Start:
                    OnAnimationStart();
                    break;
                case AnimationEventKey.End:
                    OnAnimationEnd();
                    break;
                case AnimationEventKey.VFXStart:
                    OnVfxStart();
                    break;
                case AnimationEventKey.VFXEnd:
                    OnVfxEnd();
                    break;
                default:
                    OnCustomAnimationEvent(eventKey);
                    break;
            }
        }

        /// <summary>
        /// 애니메이션 시작 이벤트
        /// </summary>
        protected virtual void OnAnimationStart()
        {
        }

        /// <summary>
        /// 애니메이션 종료 이벤트
        /// </summary>
        protected virtual void OnAnimationEnd()
        {
            if (isAutoRemove && !isRemoved)
            {
                Remove();
            }
        }

        /// <summary>
        /// VFX 시작 이벤트
        /// </summary>
        protected virtual void OnVfxStart()
        {
        }

        /// <summary>
        /// VFX 종료 이벤트
        /// </summary>
        protected virtual void OnVfxEnd()
        {
        }

        /// <summary>
        /// 커스텀 애니메이션 이벤트 처리
        /// </summary>
        protected virtual void OnCustomAnimationEvent(AnimationEventKey eventKey)
        {
        }

        /// <summary>
        /// 특정 애니메이션 상태로 전환
        /// </summary>
        public void PlayAnimation(string stateName)
        {
            if (animator != null)
            {
                animator.Play(stateName);
                animator.Update(0f);
                
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                animationDuration = stateInfo.length;
                isAutoRemove = !stateInfo.loop;
                elapsedTime = 0f;
            }
        }

        /// <summary>
        /// 애니메이션 속도 설정
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
        }

        /// <summary>
        /// Animator 컴포넌트 반환
        /// </summary>
        public Animator Animator => animator;

        /// <summary>
        /// AnimationEventListener 컴포넌트 반환
        /// </summary>
        public AnimationEventListener AnimationEventListener => animationEventListener;
    }
}
