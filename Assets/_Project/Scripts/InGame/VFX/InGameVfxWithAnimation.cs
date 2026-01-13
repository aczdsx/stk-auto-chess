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

        // 이벤트 콜백
        private Action onAnimationStartCallback;
        private Action onAnimationEndCallback;
        private Action onVfxStartCallback;
        private Action onVfxEndCallback;
        private Action<AnimationEventKey> onCustomAnimationEventCallback;

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
            // 콜백 초기화
            onAnimationStartCallback = null;
            onAnimationEndCallback = null;
            onVfxStartCallback = null;
            onVfxEndCallback = null;
            onCustomAnimationEventCallback = null;
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
            onAnimationStartCallback?.Invoke();
        }

        /// <summary>
        /// 애니메이션 종료 이벤트
        /// </summary>
        protected virtual void OnAnimationEnd()
        {
            onAnimationEndCallback?.Invoke();
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
            onVfxStartCallback?.Invoke();
        }

        /// <summary>
        /// VFX 종료 이벤트
        /// </summary>
        protected virtual void OnVfxEnd()
        {
            onVfxEndCallback?.Invoke();
        }

        /// <summary>
        /// 커스텀 애니메이션 이벤트 처리
        /// </summary>
        protected virtual void OnCustomAnimationEvent(AnimationEventKey eventKey)
        {
            onCustomAnimationEventCallback?.Invoke(eventKey);
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

        #region 콜백 등록 메서드

        /// <summary>
        /// 애니메이션 시작 이벤트 콜백 등록
        /// </summary>
        public void SetOnAnimationStartCallback(Action callback)
        {
            onAnimationStartCallback = callback;
        }

        /// <summary>
        /// 애니메이션 종료 이벤트 콜백 등록
        /// </summary>
        public void SetOnAnimationEndCallback(Action callback)
        {
            onAnimationEndCallback = callback;
        }

        /// <summary>
        /// VFX 시작 이벤트 콜백 등록
        /// </summary>
        public void SetOnVfxStartCallback(Action callback)
        {
            onVfxStartCallback = callback;
        }

        /// <summary>
        /// VFX 종료 이벤트 콜백 등록
        /// </summary>
        public void SetOnVfxEndCallback(Action callback)
        {
            onVfxEndCallback = callback;
        }

        /// <summary>
        /// 커스텀 애니메이션 이벤트 콜백 등록
        /// </summary>
        public void SetOnCustomAnimationEventCallback(Action<AnimationEventKey> callback)
        {
            onCustomAnimationEventCallback = callback;
        }

        /// <summary>
        /// 모든 콜백 제거
        /// </summary>
        public void ClearAllCallbacks()
        {
            onAnimationStartCallback = null;
            onAnimationEndCallback = null;
            onVfxStartCallback = null;
            onVfxEndCallback = null;
            onCustomAnimationEventCallback = null;
        }

        #endregion
    }
}
