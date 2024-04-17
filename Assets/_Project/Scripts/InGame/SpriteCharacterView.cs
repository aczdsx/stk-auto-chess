using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookApps.SampleTeamBattle
{
    public class SpriteCharacterView : CachedMonoBehaviour, ICharacterView
    {
        [SerializeField] private Animator animator;
        private bool cachedFlipX;

        public event Action<string, AnimationEventKey> OnAnimationEvent;

        public float Height => throw new NotImplementedException();

        public async UniTask Initialize(ICharacterStatData statData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// viewPosition을 받아 위치를 업데이트 합니다.
        /// </summary>
        /// <param name="viewPosition">뷰의 위치</param>
        public void UpdatePosition(Vector3 viewPosition)
        {
            CachedTr.localPosition = viewPosition;
        }

        public void SetAnimationSpeed(float speed)
        {
            animator.speed = speed;
        }

        public void LookAt(bool flipX)
        {
            if (cachedFlipX != flipX)
            {
                var scale = CachedTr.localScale;
                var x = scale.x;
                scale.x = flipX ? -Mathf.Abs(x) : Mathf.Abs(x);
                CachedTr.localScale = scale;
                cachedFlipX = flipX;
            }
        }

        public AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false)
        {
            throw new NotImplementedException();
        }

        public void OnHit()
        {
            throw new NotImplementedException();
        }
    }

    public class SpriteCharacterViewPool : ICharacterViewPool
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            CharacterViewPool.Initialize(new SpriteCharacterViewPool());
        }

        public ICharacterView GetCharacterView(ICharacterStatData statData)
        {
            if (!InGameResourceHolder.PlayerCharacterPrefabs.TryGetValue(statData.CharacterId, out GameObject prefab))
            {
                return default;
            }

            GameObject go = Object.Instantiate(prefab);
            var view = go.GetComponent<SpriteCharacterView>();
            return view;
        }

        public void ReturnCharacterView(ICharacterView characterView)
        {
            if (characterView is SpriteCharacterView view)
            {
                Object.Destroy(view.gameObject);
            }
        }
    }
}
