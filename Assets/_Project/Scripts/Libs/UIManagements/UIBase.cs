using System;
using UnityEngine;
using CookApps.TeamBattle.Core;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public abstract class UIBase : CachedMonoBehaviour
    {
        [SerializeField] protected Animator baseAnimator;

        protected Action<UIBase> enterEndCallback;
        protected Action<UIBase> exitEndCallback;

        private bool hasEnterAnimation;
        private bool hasExitAnimation;

        public virtual int Priority => 0;

        public string Key { get; set; }
        public SceneUIManager.UIType UIType { get; set; }

        protected virtual void Awake()
        {
            if (baseAnimator == null)
            {
                baseAnimator = GetComponent<Animator>();
                if (baseAnimator == null)
                    return;
            }

            foreach (var clip in baseAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "StartEnter" || clip.name == "StartExit")
                {
                    if (clip.name == "StartEnter")
                        hasEnterAnimation = true;
                    if (clip.name == "StartExit")
                        hasExitAnimation = true;
                    AnimationEvent animationEndEvent = new AnimationEvent();
                    animationEndEvent.time = clip.length;
                    animationEndEvent.functionName = "AnimationCompleteHandler";
                    animationEndEvent.stringParameter = clip.name;
                    clip.AddEvent(animationEndEvent);
                }
            }
        }

        public virtual void OnPreEnter(object param)
        {
        }

        public virtual void StartEnterAnimation(Action<UIBase> endCallback)
        {
            if (hasEnterAnimation)
            {
                enterEndCallback = endCallback;
                baseAnimator.Play("StartEnter");
                return;
            }

            CallAfterDelayFrame(1, endCallback).Forget();
        }

        public virtual void OnPostEnter()
        {
        }

        public virtual void OnPreExit()
        {
        }

        public virtual void StartExitAnimation(Action<UIBase> endCallback)
        {
            if (hasExitAnimation)
            {
                exitEndCallback = endCallback;
                baseAnimator.Play("StartExit");
                return;
            }

            // exit은 CallAfterDelayFrame으로 호출하면 문제가 생기는 경우가 있다..
            endCallback?.Invoke(this);
        }

        public virtual void OnPostExit()
        {
        }

        public virtual void OnBackButton(ref bool offPrevUI)
        {
            SceneUIManager.Instance.RequestPopUI(this);
        }

        private void AnimationCompleteHandler(string name)
        {
            if (name == "StartEnter")
            {
                var tempAction = enterEndCallback;
                enterEndCallback = null;
                tempAction?.Invoke(this);
            }

            if (name == "StartExit")
            {
                var tempAction = exitEndCallback;
                exitEndCallback = null;
                tempAction?.Invoke(this);
            }
        }

        private async UniTask CallAfterDelayFrame(int delayFrame, Action<UIBase> endCallback)
        {
            await UniTask.DelayFrame(delayFrame);
            endCallback?.Invoke(this);
        }
    }
}
