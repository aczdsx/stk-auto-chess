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
        private bool _cachedFlipFront;
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
            // 애니메이션 트리거 설정
            _animator.SetTrigger(animationKey.ToString());

            // 애니메이터 컨트롤러에서 현재 활성화된 상태를 찾고 해당 상태의 애니메이션 클립을 반환합니다.
            var runtimeAnimatorController = _animator.runtimeAnimatorController;
            if (!runtimeAnimatorController)
            {
                throw new InvalidOperationException("runtimeAnimatorController is null.");
            }

            foreach (var animationClip in runtimeAnimatorController.animationClips)
            {
                string prefix = (_cachedFlipFront) ? "Front_" : "Back_";
                if (animationClip.name == prefix + animationKey)
                {
                    return animationClip;
                }
            }

            throw new KeyNotFoundException($"[{animationKey}] is not found.");
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
}
