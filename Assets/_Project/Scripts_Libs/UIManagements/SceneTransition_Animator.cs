using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransition_Animator : MonoBehaviour, ISceneTransition
    {
        [SerializeField] private Image dim;
        [SerializeField] private Animator _animator;
        private float fadeInDuration = 0.25f;
        private float fadeOutDuration = 0.5f;

        public static SceneTransition_Animator Create()
        {
            var prefab = Resources.Load<GameObject>("UI/TransitionAnimator");
            GameObject go = Instantiate(prefab);
            DontDestroyOnLoad(go);
            return go.GetComponent<SceneTransition_Animator>();
        }

        public async UniTask FadeInAsync()
        {
            _animator.Play("SetTransitionIn");
            await UniTask.Yield(PlayerLoopTiming.Update);
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.Delay(TimeSpan.FromSeconds(stateInfo.length));
        }

        public async UniTask FadeOutAsync(bool withDelete)
        {
            _animator.Play("SetTransitionOut");
            await UniTask.Yield(PlayerLoopTiming.Update);
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            await UniTask.Delay(TimeSpan.FromSeconds(stateInfo.length));

            if (withDelete)
            {
                Destroy(gameObject);
            }
        }
    }
}
