using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneTransition_Animator : SceneTransitionBase
    {
        [SerializeField] private Animator _animator;
        private GameObject _prefabInstance;

        public override void Initialize(object viewOption)
        {
            // 프리팹 로드
            var prefab = Resources.Load<GameObject>("UI/TransitionAnimationPrefab");
            if (prefab == null)
            {
                Debug.LogError("Failed to load TransitionAnimator prefab from Resources/UI/TransitionAnimationPrefab");
                return;
            }

            // 프리팹 인스턴스 생성
            _prefabInstance = Instantiate(prefab);
            DontDestroyOnLoad(_prefabInstance);

            _animator = _prefabInstance.GetComponent<Animator>();

            var prefabRect = _prefabInstance.GetComponent<RectTransform>();
            prefabRect.SetParent(this.CachedGo.transform, false);
            prefabRect.anchorMin = Vector2.zero;
            prefabRect.anchorMax = Vector2.one;
            prefabRect.offsetMin = Vector2.zero;  // Left, Bottom
            prefabRect.offsetMax = Vector2.zero;  // Right, Top
            prefabRect.anchoredPosition = Vector2.zero;
            prefabRect.localScale = Vector3.one;
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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 프리팹 인스턴스 파괴
            if (_prefabInstance != null)
            {
                Destroy(_prefabInstance);
                _prefabInstance = null;
            }
            _animator = null;
        }
    }
}
