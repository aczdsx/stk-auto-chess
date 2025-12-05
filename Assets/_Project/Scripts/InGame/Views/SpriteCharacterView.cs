using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using PrimeTween;

namespace CookApps.AutoBattler
{
    public class SpriteCharacterView : CachedMonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private List<SpriteRenderer> _spriteRendererList;

        [SerializeField]
        private Transform _rootTransform;

        [SerializeField]
        private Transform _skillRootTransform;

        [SerializeField]
        private Transform _rotateionRootTransform;

        [SerializeField]
        private Transform _projectileFrontTransform;

        [SerializeField]
        private Transform _projectileBackTransform;

        [SerializeField]
        private Material _defaultMaterial;

        [SerializeField]
        private Material _disorveMaterial;

        [SerializeField]
        private Material _hologramMaterial;

        private AnimationEventListener _animationEventListener;
        public bool CachedFlipX => _cachedFlipX;
        public bool CachedFront => _cachedFront;
        private readonly float _hitDurationTime = 0.25f;
        public event Action<AnimationKey, AnimationEventKey> OnAnimationEvent;

        private AnimationKey _currentAnimationKey;
        private GameObject _instance;
        private bool _cachedFlipX;
        private bool _cachedFront;
        private static readonly int IsFront = Animator.StringToHash("IsFront");
        private Tween _viewScaleTween;


        public Transform SkillRootTransform => _skillRootTransform;
        public Transform ProjectileFrontTransform => _projectileFrontTransform;
        public Transform ProjectileBackTransform => _projectileBackTransform;

        private void Awake()
        {
            if (_animator != null)
            {
                if (_spriteRendererList == null || _spriteRendererList.Count == 0)
                {
                    var spriteRenderer = _animator.transform.GetComponent<SpriteRenderer>();
                    _spriteRendererList = new List<SpriteRenderer> { spriteRenderer };
                }
                foreach (var spriteRenderer in _spriteRendererList)
                {
                    spriteRenderer.material = _disorveMaterial;
                }
                _animationEventListener = _animator.gameObject.GetComponent<AnimationEventListener>();
                _animationEventListener.OnAnimationEvent += OnFiredAnimationEvent;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_animator != null)
            {
                _animationEventListener.OnAnimationEvent -= OnFiredAnimationEvent;
            }
        }

        public void UpdatePosition(Vector3 position, Vector3 viewPosition, Vector3 selectedOffSet)
        {
            CachedTr.localPosition = (Vector3)position + viewPosition + selectedOffSet;
        }

        public void SetScale(Vector3 scale)
        {
            _rootTransform.localScale = scale;
        }

        public void SetSelected(bool isSetSelected)
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                spriteRenderer.sortingOrder = isSetSelected ? 10 : 1;
            }
        }

        public void SetAnimationSpeed(float speed)
        {
            if (_animator != null)
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

            SetFlipOrNot();
        }

        public AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false)
        {
            if (_animator == null)
                return null;

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

        public void SetHpBarView(HpBarView hpBarView, float height)
        {
            hpBarView.transform.SetParent(_rotateionRootTransform);
            hpBarView.CachedTr.position = CachedTr.position;
            hpBarView.transform.localPosition = new Vector3(0, height, 0);
            hpBarView.transform.localRotation = Quaternion.identity;
            hpBarView.transform.localScale = new Vector3(3, 3, 3);
        }

        public void SetHologramShader()
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                spriteRenderer.material = _hologramMaterial;
            }
        }

        public async UniTask DoDeadAction(AnimationClip clip)
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                spriteRenderer.material = _disorveMaterial;
            }

            float temp = 0;
            float duration = clip.length;

            while (temp < 1.0f)
            {
                if (_spriteRendererList == null || _spriteRendererList.Count == 0)
                    return;

                temp += Time.deltaTime / duration;

                foreach (var spriteRenderer in _spriteRendererList)
                {
                    if (spriteRenderer != null)
                        spriteRenderer.material.SetFloat("_Dissolve", temp);
                }

                await UniTask.Yield();
            }
        }

        public async UniTask DoHitAction()
        {
            float duration = _hitDurationTime;
            float elapsedTime = 0;
            if (_spriteRendererList == null || _spriteRendererList.Count == 0)
                return;

            Color initialColor = _spriteRendererList[0].color;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.PingPong(elapsedTime * 2 / duration, 1.0f);
                float gAndBValue = Mathf.Lerp(255, 130, t) / 255f;

                foreach (var spriteRenderer in _spriteRendererList)
                {
                    if (spriteRenderer)
                        spriteRenderer.material.SetColor("_TintColor",
                            new Color(initialColor.r, gAndBValue, gAndBValue, initialColor.a));
                }

                await UniTask.Yield();
            }

            foreach (var spriteRenderer in _spriteRendererList)
            {
                if (spriteRenderer)
                    spriteRenderer.material.SetColor("_TintColor",
                        new Color(1, 1, 1, 1));
            }
        }

        public void SetColor(Color color)
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                if (spriteRenderer)
                    spriteRenderer.material.SetColor("_TintColor", color);
            }
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

            SetFlipOrNot();
        }

        public void AddViewScale(float viewScale)
        {
            Ease ease = Ease.OutElastic;
            _viewScaleTween = Tween.Custom(
                _rotateionRootTransform.localScale,
                _rotateionRootTransform.localScale + new Vector3(viewScale, viewScale, viewScale),
                0.5f,
                (Vector3 value) =>
                {
                    _rotateionRootTransform.localScale = value;
                },
                ease: ease);
        }

        public void RemoveViewScale(float viewScale)
        {
            _viewScaleTween.Stop();
            Vector3 scale = _rotateionRootTransform.localScale;
            scale.x -= viewScale;
            scale.y -= viewScale;
            scale.z -= viewScale;
            _rotateionRootTransform.localScale = scale;
        }

        private void SetFlipOrNot()
        {
            Vector3 scale = _rootTransform.localScale;
            scale.x = _cachedFlipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            _rootTransform.localScale = scale;

            Vector3 skillScale = _skillRootTransform.localScale;
            skillScale.x = _cachedFlipX ? -Mathf.Abs(skillScale.x) : Mathf.Abs(skillScale.x);
            _skillRootTransform.localScale = skillScale;
        }
    }
}
