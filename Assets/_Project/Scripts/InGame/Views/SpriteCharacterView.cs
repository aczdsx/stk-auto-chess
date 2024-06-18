using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class SpriteCharacterView : CachedMonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _rootTransform;
        [SerializeField] private Transform _skillRootTransform;
        [SerializeField] private Transform _rotateionRootTransform;

        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _disorveMaterial;
        private AnimationEventListener _animationEventListener;
        public CharacterStatData GetStatData() => _statData;
        public bool CachedFlipX => _cachedFlipX;
        public bool CachedFront => _cachedFront;
        public float Height => 2.0f;
        private readonly float _hitDurationTime = 0.2f;
        public event Action<AnimationKey, AnimationEventKey> OnAnimationEvent;

        private AnimationKey _currentAnimationKey;
        private GameObject _instance;
        private bool _cachedFlipX;
        private bool _cachedFront;
        private CharacterStatData _statData;
        private static readonly int IsFront = Animator.StringToHash("IsFront");

        public Transform SkillRootTransform => _skillRootTransform;

        private void Awake()
        {
            _spriteRenderer = _animator.transform.GetComponent<SpriteRenderer>();
            _spriteRenderer.material = _disorveMaterial;
            _animationEventListener = _animator.gameObject.GetComponent<AnimationEventListener>();
            _animationEventListener.OnAnimationEvent += OnFiredAnimationEvent;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // AddressableInstantiateHelper.ReleaseGameObject(_instance);
            _animationEventListener.OnAnimationEvent -= OnFiredAnimationEvent;
        }

        /// <summary>
        /// viewPosition을 받아 위치를 업데이트 합니다.
        /// </summary>
        /// <param name="position">view의 필드 위치</param>
        /// <param name="viewPosition">에어본이나 점프등을 하기 위해 필드 위치와의 offset이 필요할 경우 사용</param>
        public void UpdatePosition(Vector3 position, Vector3 viewPosition)
        {
            CachedTr.localPosition = (Vector3) position + viewPosition;
        }

        public void SetSelected(bool isSetSelected)
        {
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

        public void LookAt(InGameTile currentTile, InGameTile targetTile)
        {
            float deltaX = targetTile.X - currentTile.X;
            float deltaY = targetTile.Y - currentTile.Y;

            float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;

            if (angle >= -45 && angle < 45)
            {
                _cachedFlipX = true;
                _cachedFront = false;
            }
            else if (angle >= 45 && angle < 135)
            {
                _cachedFlipX = false;
                _cachedFront = false;
            }
            else if (angle >= -135 && angle < -45)
            {
                _cachedFlipX = true;
                _cachedFront = true;
            }
            else
            {
                _cachedFlipX = false;
                _cachedFront = true;
            }

            Vector3 scale = _rootTransform.localScale;
            scale.x = _cachedFlipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            _rootTransform.localScale = scale;

            Vector3 skillScale = _skillRootTransform.localScale;
            skillScale.x = _cachedFlipX ? -Mathf.Abs(skillScale.x) : Mathf.Abs(skillScale.x);
            _skillRootTransform.localScale = skillScale;
        }

        public AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false)
        {
            string animationTrigger = animationKey.ToString();
            _animator.SetBool(IsFront, _cachedFront);
            _animator.SetTrigger(animationTrigger);

            var runtimeAnimatorController = _animator.runtimeAnimatorController;
            if (runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("runtimeAnimatorController is null.");
            }

            string prefix = (_cachedFront) ? "Front_" : "Back_";
            string fullAnimationName = prefix + animationTrigger;

            foreach (var animationClip in runtimeAnimatorController.animationClips)
            {
                if (animationClip.name == fullAnimationName)
                {
                    _currentAnimationKey = animationKey;
                    return animationClip;
                }
            }

            throw new KeyNotFoundException($"[{fullAnimationName}] is not found.");
        }

        public void OnFiredAnimationEvent(AnimationEventKey animationEventKey)
        {
            OnAnimationEvent?.Invoke(_currentAnimationKey, animationEventKey);
        }

        public void OnHit()
        {
            DoHitAction().Forget();
        }

        public void SetDeadSprite(AnimationClip clip)
        {
            DoDeadAction(clip).Forget();
        }

        public void SetHpBarView(HpBarView hpBarView)
        {
            hpBarView.transform.SetParent(_rotateionRootTransform);
            hpBarView.CachedTr.position = CachedTr.position;
            hpBarView.transform.localPosition = new Vector3(0, Height, 0);
            hpBarView.transform.localRotation = Quaternion.identity;
            hpBarView.transform.localScale = new Vector3(3, 3, 3);
        }

        public async UniTask DoDeadAction(AnimationClip clip)
        {
            _spriteRenderer.material = _disorveMaterial;
            float temp = 0;
            float duration = clip.length;

            while (temp < 1.0f)
            {
                if (_spriteRenderer == null)
                    return;

                temp += Time.deltaTime / duration;

                _spriteRenderer.material.SetFloat("_Dissolve", temp);

                await UniTask.Yield();
            }
        }

        public async UniTask DoHitAction()
        {
            float duration = _hitDurationTime;
            float elapsedTime = 0;
            Color initialColor = _spriteRenderer.color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.PingPong(elapsedTime * 2 / duration, 1.0f);
                float gAndBValue = Mathf.Lerp(255, 130, t) / 255f;

                if (_spriteRenderer)
                    _spriteRenderer.color = new Color(initialColor.r, gAndBValue, gAndBValue, initialColor.a);

                await UniTask.Yield();
            }

            if (_spriteRenderer)
                _spriteRenderer.color = initialColor;
        }

        protected void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(this.transform.position, new Vector3(0.9f, 1f));
        }

        public void SetFirstDirection(AllianceType type)
        {
            if (type == AllianceType.Enemy)
            {
                _cachedFlipX = true;
                _cachedFront = true;
            }
            else
            {
                _cachedFlipX = false;
                _cachedFront = false;
            }

            Vector3 scale = _rootTransform.localScale;
            scale.x = _cachedFlipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            _rootTransform.localScale = scale;

            Vector3 skillScale = _skillRootTransform.localScale;
            skillScale.x = _cachedFlipX ? -Mathf.Abs(skillScale.x) : Mathf.Abs(skillScale.x);
            _skillRootTransform.localScale = skillScale;
        }
    }
}
