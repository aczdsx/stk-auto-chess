using System;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using LitMotion;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum Direction
    {
        Front,
        Back,
        Left,
        Right
    }
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
        private Material _disorveMaterial;

        [SerializeField]
        private Material _hologramMaterial;

        [SerializeField]
        private Transform _skillTopFXTransform;
        [SerializeField]
        private Transform _skillMiddleFXTransform;
        [SerializeField]
        private Transform _skillBottomFXTransform;


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
        private MotionHandle _viewScaleHandle;
        private Vector3 _viewScaleTarget = Vector3.one;


        public Transform SkillRootTransform => _skillRootTransform;
        public Transform ProjectileFrontTransform => _projectileFrontTransform;
        public Transform ProjectileBackTransform => _projectileBackTransform;

        public Transform SkillTopFXTransform => _skillTopFXTransform;
        public Transform SkillMiddleFXTransform => _skillMiddleFXTransform;
        public Transform SkillBottomFXTransform => _skillBottomFXTransform;

        private void Awake()
        {
            if (_animator != null)
            {
                if (_spriteRendererList == null || _spriteRendererList.Count == 0)
                {
                    var spriteRenderer = _animator.GetComponent<SpriteRenderer>();
                    _spriteRendererList = new List<SpriteRenderer> { spriteRenderer };
                }
                SetDisolveShader();
                _animationEventListener = _animator.GetComponent<AnimationEventListener>();
                _animationEventListener.OnAnimationEvent += OnFiredAnimationEvent;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            StopAllTweens();
            if (_animator != null)
            {
                _animationEventListener.OnAnimationEvent -= OnFiredAnimationEvent;
            }
        }

        public void StopAllTweens()
        {
            _viewScaleHandle.TryCancel();
            _viewScaleHandle = default;
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
            LookAt(new Vector2(currentTile.X, currentTile.Y), new Vector2(targetTile.X, targetTile.Y));
        }

        public void LookAt(int2 src, int2 dest)
        {
            LookAt(new Vector2(src.x, src.y), new Vector2(dest.x, dest.y));
        }

        public void LookAt(Vector2 src, Vector2 dest)
        {
            var prevCachedFlipX = _cachedFlipX;
            var prevCachedFront = _cachedFront;
            float deltaX = dest.x - src.x;
            float deltaY = dest.y - src.y;

            float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;

            _cachedFlipX = angle is >= -135f and < 45f;
            _cachedFront = angle is < -45f or > 135f;
            // if (angle is >= -45 and < 45)
            // {
            //     _cachedFlipX = true;
            //     _cachedFront = false;
            // }
            // else if (angle is >= 45 and < 135)
            // {
            //     _cachedFlipX = false;
            //     _cachedFront = false;
            // }
            // else if (angle is >= -135 and < -45)
            // {
            //     _cachedFlipX = true;
            //     _cachedFront = true;
            // }
            // else
            // {
            //     _cachedFlipX = false;
            //     _cachedFront = true;
            // }

            if (prevCachedFlipX != _cachedFlipX)
                SetFlipOrNot();

            if (prevCachedFront != _cachedFront && _animator != null)
                _animator.SetBool(IsFront, _cachedFront);
        }

        public void LookAt(Direction dir)
        {
            var prevCachedFlipX = _cachedFlipX;
            var prevCachedFront = _cachedFront;
            switch (dir)
            {
                case Direction.Front:
                    _cachedFlipX = false;
                    _cachedFront = true;
                    break;
                case Direction.Back:
                    _cachedFlipX = true;
                    _cachedFront = false;
                    break;
                case Direction.Left:
                    _cachedFlipX = false;
                    _cachedFront = false;
                    break;
                case Direction.Right:
                    _cachedFlipX = true;
                    _cachedFront = true;
                    break;
            }
            if (prevCachedFlipX != _cachedFlipX)
                SetFlipOrNot();

            if (prevCachedFront != _cachedFront && _animator != null)
                _animator.SetBool(IsFront, _cachedFront);
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

        public void SetDisolveShader()
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                spriteRenderer.material = _disorveMaterial;
            }
        }

        public void SpriteRendererSetActive(bool isActive)
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                spriteRenderer.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// 외부에서 지정한 Material 적용 (고스트 캐릭터 등)
        /// </summary>
        public void SetMaterial(Material material)
        {
            if (material == null) return;
            foreach (var spriteRenderer in _spriteRendererList)
            {
                if (spriteRenderer)
                    spriteRenderer.material = material;
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

            // 애니메이터에 Front/Back 상태 반영
            if (_animator != null)
                _animator.SetBool(IsFront, _cachedFront);
        }

        public void AddViewScale(float viewScale)
        {
            _viewScaleHandle.TryCancel();
            _viewScaleTarget += new Vector3(viewScale, viewScale, viewScale);
            _viewScaleHandle = LMotion.Create(
                _rotateionRootTransform.localScale,
                _viewScaleTarget,
                0.5f)
                .WithEase(Ease.OutElastic)
                .Bind(value =>
                {
                    if (_rotateionRootTransform != null)
                        _rotateionRootTransform.localScale = value;
                })
                .AddTo(this);
        }

        public void RemoveViewScale(float viewScale)
        {
            _viewScaleHandle.TryCancel();
            _viewScaleTarget -= new Vector3(viewScale, viewScale, viewScale);
            _viewScaleHandle = LMotion.Create(
                _rotateionRootTransform.localScale,
                _viewScaleTarget,
                0.5f)
                .WithEase(Ease.OutElastic)
                .Bind(value =>
                {
                    if (_rotateionRootTransform != null)
                        _rotateionRootTransform.localScale = value;
                })
                .AddTo(this);
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
