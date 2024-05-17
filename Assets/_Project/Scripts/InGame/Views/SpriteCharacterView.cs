using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using PrimeTweenDemo;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace CookApps.AutoBattler
{
    public class SpriteCharacterView : CachedMonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        public CharacterStatData GetStatData() => _statData;
        public float Height => 1.0f;
        public event Action<string, AnimationEventKey> OnAnimationEvent;

        private GameObject _instance;
        private bool _cachedFlipX;
        private CharacterStatData _statData;

        public async UniTask Initialize(CharacterStatData statData)
        {
            Debug.LogColor($"CharView Initialize : {statData}");
            this._statData = statData;
            // _instance = await AddressableInstantiateHelper.InstantiateAsync($"Characters/{statData.CharacterId}/{statData.CharacterId}.prefab", CachedTr);
            var hpBar = InGameHpBarViewPool.Instance.GetHpBar();
            hpBar.CachedTr.SetParent(_animator.transform);
            hpBar.CachedTr.localPosition = Vector3.zero;
            hpBar.CachedTr.localRotation = Quaternion.identity;
            hpBar.CachedTr.localScale = Vector3.one;

            _spriteRenderer = _animator.transform.GetComponent<SpriteRenderer>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // AddressableInstantiateHelper.ReleaseGameObject(_instance);
        }

        /// <summary>
        /// viewPosition을 받아 위치를 업데이트 합니다.
        /// </summary>
        /// <param name="position">view의 필드 위치</param>
        /// <param name="viewPosition">에어본이나 점프등을 하기 위해 필드 위치와의 offset이 필요할 경우 사용</param>
        public void UpdatePosition(Vector3 position, Vector3 viewPosition)
        {
            CachedTr.localPosition = (Vector3)position + viewPosition;
        }

        public void SetSelected(bool isSetSelected)
        {
            //[TODO] sortingOrder ingame에서 위치에 따라 관리 해야할듯.
            if (isSetSelected)
            {
                _spriteRenderer.sortingOrder = 10;
            }
            else
            {
                _spriteRenderer.sortingOrder = 1;
            }
        }

        public void SetAnimationSpeed(float speed)
        {
            _animator.speed = speed;
        }

        public void LookAt(bool flipX)
        {
            if (_cachedFlipX != flipX)
            {
                var scale = _animator.transform.localScale;
                var x = scale.x;
                scale.x = flipX ? -Mathf.Abs(x) : Mathf.Abs(x);
                _animator.transform.localScale = scale;
                _cachedFlipX = flipX;
            }
        }

        public AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false)
        {
            //[TODO] Clip return 하는 이유 확인 필요 Animation 구조 어떻게 가져갈지 논의 필요
            _animator.SetTrigger(animationKey.ToString());
            throw new NotImplementedException();
        }

        public void OnHit()
        {
            throw new NotImplementedException();
        }

        protected void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(this.transform.position, new Vector3(0.9f, 1f));
        }
    }

    public class SpriteCharacterViewPool : Singleton<SpriteCharacterViewPool>
    {
        private Dictionary<int, UnityPool<SpriteCharacterView>> pool = new ();

        public void Clear()
        {
        }

        public SpriteCharacterView GetCharacterView(CharacterStatData statData, AllianceType allianceType)
        {
            // [TODO] PlayerCharacterPrefabs, EnemyCharacterPrefabs 왜 나눠 있는 지 확인 필요.
            GameObject prefab = null;
            if(allianceType == AllianceType.Player)
            {
                if (!InGameResourceHolder.PlayerCharacterPrefabs.TryGetValue(statData.CharacterId, out prefab))
                {
                    return null;
                }
            }
            else
            {
                if (!InGameResourceHolder.EnemyCharacterPrefabs.TryGetValue(statData.CharacterId, out prefab))
                {
                    return null;
                }
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
