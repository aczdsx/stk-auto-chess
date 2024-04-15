using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public abstract class UILayer : CachedMonoBehaviour
    {
        [SerializeField] protected Animator baseAnimator;

        protected Action<UILayer> enterEndCallback;
        protected Action<UILayer> exitEndCallback;

        private bool hasEnterAnimation;
        private bool hasExitAnimation;

        public virtual int Priority => 0;

        public string Key { get; set; }
        public UILayerType UILayerType { get; set; }

        protected virtual void Awake()
        {
            if (baseAnimator == null)
            {
                baseAnimator = GetComponent<Animator>();
                if (baseAnimator == null)
                {
                    return;
                }
            }

            foreach (AnimationClip clip in baseAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == "StartEnter" || clip.name == "StartExit")
                {
                    if (clip.name == "StartEnter")
                    {
                        hasEnterAnimation = true;
                    }

                    if (clip.name == "StartExit")
                    {
                        hasExitAnimation = true;
                    }

                    var animationEndEvent = new AnimationEvent();
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

        public virtual void StartEnterAnimation(Action<UILayer> endCallback)
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

        public virtual void StartExitAnimation(Action<UILayer> endCallback)
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
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void AnimationCompleteHandler(string name)
        {
            if (name == "StartEnter")
            {
                Action<UILayer> tempAction = enterEndCallback;
                enterEndCallback = null;
                tempAction?.Invoke(this);
            }

            if (name == "StartExit")
            {
                Action<UILayer> tempAction = exitEndCallback;
                exitEndCallback = null;
                tempAction?.Invoke(this);
            }
        }

        private async UniTask CallAfterDelayFrame(int delayFrame, Action<UILayer> endCallback)
        {
            await UniTask.DelayFrame(delayFrame);
            endCallback?.Invoke(this);
        }
    }
}
