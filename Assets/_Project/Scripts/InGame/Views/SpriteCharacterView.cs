using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CookApps.AutoBattler
{
    public class SpriteCharacterView : CachedMonoBehaviour
    {
        [SerializeField] private Animator animator;
        private bool cachedFlipX;

        public event Action<string, AnimationEventKey> OnAnimationEvent;

        private CharacterStatData statData;
        public CharacterStatData GetStatData() => statData;
        public float Height => throw new NotImplementedException();

        private GameObject _instance;

        public async UniTask Initialize(CharacterStatData statData)
        {
            this.statData = statData;
            _instance = await AddressableInstantiateHelper.InstantiateAsync($"Characters/{statData.CharacterId}/{statData.CharacterId}.prefab", CachedTr);
            var hpBar = InGameHpBarViewPool.Instance.GetHpBar();
            hpBar.CachedTr.SetParent(CachedTr);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AddressableInstantiateHelper.ReleaseGameObject(_instance);
        }

        /// <summary>
        /// viewPosition을 받아 위치를 업데이트 합니다.
        /// </summary>
        /// <param name="position">view의 필드 위치</param>
        /// <param name="viewPosition">에어본이나 점프등을 하기 위해 필드 위치와의 offset이 필요할 경우 사용</param>
        public void UpdatePosition(Vector2 position, Vector3 viewPosition)
        {
            CachedTr.localPosition = (Vector3)position + viewPosition;
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

    public class SpriteCharacterViewPool : Singleton<SpriteCharacterViewPool>
    {
        private Dictionary<int, UnityPool<SpriteCharacterView>> pool = new ();

        public void Clear()
        {
        }

        public SpriteCharacterView GetCharacterView(CharacterStatData statData)
        {
            if (!InGameResourceHolder.PlayerCharacterPrefabs.TryGetValue(statData.CharacterId, out GameObject prefab))
            {
                return null;
            }

            if (!pool.TryGetValue(statData.CharacterId, out UnityPool<SpriteCharacterView> characterPool))
            {
                characterPool = new UnityPool<SpriteCharacterView>();
                characterPool.Initialize(prefab);
                pool.Add(statData.CharacterId, characterPool);
            }

            var characterView = characterPool.Get(null);
            characterView.Initialize(statData).Forget();
            return characterView;
        }

        public void ReturnCharacterView(SpriteCharacterView characterView)
        {
            if (characterView != null)
            {
                if (pool.TryGetValue(characterView.GetStatData().CharacterId, out UnityPool<SpriteCharacterView> characterPool))
                {
                    characterPool.Return(characterView);
                }
                else
                {
                    Object.Destroy(characterView.CachedGo);
                }
            }
        }
    }
}
