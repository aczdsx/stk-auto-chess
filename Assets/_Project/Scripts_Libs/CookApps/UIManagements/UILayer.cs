using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public abstract class UILayer : CachedMonoBehaviour
    {
        [SerializeField] private UILayerType uiLayerType;
        [SerializeField] protected Animator baseAnimator;
        [SerializeField] private AssetReferenceGameObject[] preloadAddressables;

        public AssetReferenceGameObject[] PreloadAddressables => preloadAddressables;

        protected event Action<UILayer> EnterEndCallback;
        protected internal event Action<UILayer> ExitEndCallback;

        private const string EnterAnimationKey = "StartEnter"; // 애니메이터 상태 키 (입장 애니메이션)
        private const string ExitAnimationKey = "StartExit";  // 애니메이터 상태 키 (퇴장 애니메이션)

        private static readonly int EnterAnimationHash = Animator.StringToHash(EnterAnimationKey);
        private static readonly int ExitAnimationHash = Animator.StringToHash(ExitAnimationKey);

        private bool hasEnterAnimation;
        private bool hasExitAnimation;

        public virtual int Priority => 0;

        public string Key { get; set; }
        public UILayerType UILayerType => uiLayerType;

        protected internal virtual void Awake()
        {
            if (baseAnimator == null)
            {
                baseAnimator = GetComponent<Animator>();
                if (baseAnimator == null)
                {
                    return;
                }
            }

            hasEnterAnimation = baseAnimator.runtimeAnimatorController != null && baseAnimator.HasState(0, EnterAnimationHash);
            hasExitAnimation = baseAnimator.runtimeAnimatorController != null && baseAnimator.HasState(0, ExitAnimationHash);

        }

        protected internal virtual void OnPreEnter(object param)
        {
            for (var i = 0; i < PreloadAddressables.Length; i++)
            {
                PreloadAddressables[i].LoadAssetAsync();
            }
        }

        protected internal virtual void StartEnterAnimation(Action<UILayer> endCallback)
        {
            if (hasEnterAnimation)
            {
                EnterEndCallback -= endCallback;
                EnterEndCallback += endCallback;
                baseAnimator.Play(EnterAnimationKey);
                AddAnimationEventToCurrentClip(EnterAnimationKey);
                return;
            }

            CallAfterDelayFrame(1, endCallback).Forget();
        }

        protected internal virtual void OnPostEnter()
        {
        }

        protected internal virtual void OnPreExit()
        {
        }

        protected internal virtual void StartExitAnimation(Action<UILayer> endCallback)
        {
            if (hasExitAnimation)
            {
                ExitEndCallback -= endCallback;
                ExitEndCallback += endCallback;
                baseAnimator.Play(ExitAnimationKey);
                AddAnimationEventToCurrentClip(ExitAnimationKey);
                return;
            }

            // exit은 CallAfterDelayFrame으로 호출하면 문제가 생기는 경우가 있다..
            endCallback?.Invoke(this);
        }

        protected internal virtual void OnPostExit()
        {
            for (var i = 0; i < PreloadAddressables.Length; i++)
            {
                PreloadAddressables[i].ReleaseAsset();
            }
        }

        protected internal virtual void OnBackButton(ref bool offPrevUI)
        {
            CloseThisUILayer();
        }

        public void AnimationCompleteHandler(string name)
        {
            if (name == EnterAnimationKey)
            {
                Action<UILayer> tempAction = EnterEndCallback;
                EnterEndCallback = null;
                tempAction?.Invoke(this);
            }

            if (name == ExitAnimationKey)
            {
                Action<UILayer> tempAction = ExitEndCallback;
                ExitEndCallback = null;
                tempAction?.Invoke(this);
            }
        }

        private void AddAnimationEventToCurrentClip(string animationKey)
        {
            if (baseAnimator == null)
            {
                return;
            }

            // Play 직후 Update를 호출하여 상태를 갱신
            baseAnimator.Update(0);

            // 현재 재생 중인 클립 정보 가져오기
            var clipInfos = baseAnimator.GetCurrentAnimatorClipInfo(0);
            if (clipInfos.Length == 0)
            {
                return;
            }

            var clip = clipInfos[0].clip;
            if (clip == null)
            {
                return;
            }

            // 이미 AnimationCompleteHandler 이벤트가 있는지 확인
            var events = clip.events;
            bool hasCompleteEvent = false;

            foreach (var evt in events)
            {
                if (evt.functionName == nameof(AnimationCompleteHandler) && evt.stringParameter == animationKey)
                {
                    hasCompleteEvent = true;
                    break;
                }
            }

            if (!hasCompleteEvent)
            {
                // 마지막 프레임에 이벤트 추가
                var newEvent = new AnimationEvent
                {
                    time = clip.length,
                    functionName = nameof(AnimationCompleteHandler),
                    stringParameter = animationKey
                };

                clip.AddEvent(newEvent);
            }
        }

        private async UniTask CallAfterDelayFrame(int delayFrame, Action<UILayer> endCallback)
        {
            for (int i = 0; i < delayFrame; i++)
            {
                await UniTask.Yield();
            }
            endCallback?.Invoke(this);
        }

        public UILayerExitTask WaitForExit()
        {
            return new UILayerExitTask(this);
        }

        public UILayerExitTask<T> WaitForExit<T>()
        {
            return new UILayerExitTask<T>(this);
        }

        protected virtual void CloseThisUILayer()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
