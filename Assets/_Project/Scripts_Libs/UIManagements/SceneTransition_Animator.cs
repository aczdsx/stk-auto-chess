using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransition_Animator : SceneTransitionBase
    {
        [SerializeField] private Image dim;
        [SerializeField] private Animator _animator;

        public static SceneTransition_Animator Create()
        {
            var prefab = Resources.Load<GameObject>("UI/TransitionAnimator");
            GameObject go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            return go.GetComponent<SceneTransition_Animator>();
        }

        public override void Initialize(object viewOption)
        {
            
        }

        public override async UniTask FadeInAsync()
        {
            _animator.SetTrigger("SetTransitionIn");
            await UniTask.Yield(PlayerLoopTiming.Update);
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.Delay(TimeSpan.FromSeconds(stateInfo.length));
        }

        public override async UniTask FadeOutAsync()
        {
            _animator.SetTrigger("SetTransitionOut");
            await UniTask.Yield(PlayerLoopTiming.Update);
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.Delay(TimeSpan.FromSeconds(stateInfo.length));
        }
    }
}
