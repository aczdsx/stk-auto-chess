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

        [SerializeField]
        private CharacterVfxConfigSO _vfxConfig;

        [SerializeField]
        private GameObject _shadow;

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
        private static readonly Vector3 MaxViewScale = new Vector3(1.8f, 1.8f, 1.8f);
        private static readonly Vector3 MinViewScale = new Vector3(0.5f, 0.5f, 0.5f);
        private MotionHandle _viewScaleHandle;
        private Vector3 _viewScaleRaw = Vector3.one;
        private Vector3 _viewScaleTarget = Vector3.one;
        private AutoChess.AnimKeyframeInfo _atkInfo;
        private int _characterId;
        private bool _hasAtk2Clip;
        private bool _hasCritClip;


        public Transform SkillRootTransform => _skillRootTransform;
        public Transform ProjectileFrontTransform => _projectileFrontTransform;
        public Transform ProjectileBackTransform => _projectileBackTransform;

        public Transform SkillTopFXTransform => _skillTopFXTransform;
        public Transform SkillMiddleFXTransform => _skillMiddleFXTransform;
        public Transform SkillBottomFXTransform => _skillBottomFXTransform;

        public CharacterVfxConfigSO VfxConfig => _vfxConfig;
        public GameObject ProjectilePrefab => _vfxConfig != null ? _vfxConfig.ProjectilePrefab : null;
        public SkillViewData[] SkillEffectPrefabs => _vfxConfig != null ? _vfxConfig.SkillEffectPrefabs : null;

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
                CacheAttackExecuteTimes();
                CacheAnimationClipAvailability();
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

        public float AnimatorSpeed => _animator != null ? _animator.speed : 1f;

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
            {
                PlayAnimation(_currentAnimationKey);
            }
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
            {
                PlayAnimation(_currentAnimationKey);
            }
        }

        public AnimationClip PlayAnimation(AnimationKey animationKey, bool isLoop = false)
        {
            if (_animator == null)
                return null;

            var runtimeAnimatorController = _animator.runtimeAnimatorController;
            if (runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("runtimeAnimatorController is null.");
            }

            string animationTrigger = animationKey.ToString();
            string prefix = (_cachedFront) ? "Front_" : "Back_";
            string fullAnimationName = prefix + animationTrigger;

            foreach (var animationClip in runtimeAnimatorController.animationClips)
            {
                if (animationClip.name == fullAnimationName)
                {
                    _animator.SetBool(IsFront, _cachedFront);
                    _animator.SetTrigger(animationTrigger);
                    _currentAnimationKey = animationKey;
                    return animationClip;
                }
            }

            // ATK2/CRIT 클립이 없으면 ATK로 폴백
            if (animationKey == AnimationKey.ATK2 || animationKey == AnimationKey.CRIT)
                return PlayAnimation(AnimationKey.ATK, isLoop);

            // 클립이 없으면 IDLE로 fallback (IDLE 자체가 없으면 null)
            if (animationKey != AnimationKey.IDLE)
                return PlayAnimation(AnimationKey.IDLE, isLoop);
            return null;
        }

        /// <summary>현재 방향(front/back)에 따른 투사체 발사 위치 반환</summary>
        public Vector3 GetProjectileSpawnPosition()
        {
            var tr = _cachedFront ? _projectileFrontTransform : _projectileBackTransform;
            return tr != null ? tr.position : transform.position;
        }

        /// <summary>ATK2 클립이 존재하는지 여부 (Awake에서 캐싱)</summary>
        public bool HasAtk2Clip => _hasAtk2Clip;

        /// <summary>CRIT 클립이 존재하는지 여부 (Awake에서 캐싱)</summary>
        public bool HasCritClip => _hasCritClip;

        /// <summary>ATK 키프레임 정보 (ms 기반, float 없음). Awake에서 캐싱됨.</summary>
        public ref readonly AutoChess.AnimKeyframeInfo GetAtkInfo() => ref _atkInfo;

        /// <summary>Awake에서 호출 — AnimKeyframeHelper를 통해 ATK 키프레임 정보 캐싱</summary>
        private void CacheAttackExecuteTimes()
        {
            _characterId = AutoChess.AnimKeyframeHelper.ParseCharacterId(_animator.runtimeAnimatorController.name);
            _atkInfo = AutoChess.AnimKeyframeHelper.Resolve(_characterId);
        }

        /// <summary>ATK2/CRIT 클립 존재 여부를 캐싱 (AnimatorOverrideController의 override null 대응)</summary>
        private void CacheAnimationClipAvailability()
        {
            _hasAtk2Clip = false;
            _hasCritClip = false;

            var rac = _animator.runtimeAnimatorController;
            if (rac is AnimatorOverrideController overrideController)
            {
                // Override가 null이면 base clip만 남아있는 것 → 실제 클립 없음
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);
                foreach (var pair in overrides)
                {
                    if (pair.Key == null) continue;
                    if (!_hasAtk2Clip && pair.Key.name.EndsWith("_ATK2") && pair.Value != null)
                        _hasAtk2Clip = true;
                    if (!_hasCritClip && pair.Key.name.EndsWith("_CRIT") && pair.Value != null)
                        _hasCritClip = true;
                    if (_hasAtk2Clip && _hasCritClip) break;
                }
            }
            else
            {
                var clips = rac.animationClips;
                foreach (var clip in clips)
                {
                    if (!_hasAtk2Clip && clip.name.EndsWith("_ATK2"))
                        _hasAtk2Clip = true;
                    if (!_hasCritClip && clip.name.EndsWith("_CRIT"))
                        _hasCritClip = true;
                    if (_hasAtk2Clip && _hasCritClip) break;
                }
            }
        }

        /// <summary>지정된 클립 타입의 키프레임 정보 반환. 데이터 없으면 ATK로 폴백.</summary>
        public AutoChess.AnimKeyframeInfo GetAtkInfoForClip(AutoChess.AnimClipType clipType)
        {
            var info = AutoChess.AnimKeyframeHelper.Resolve(_characterId, clipType);
            // 데이터 없으면 (어느 방향이든 ExecTime==0) ATK로 폴백 — 한쪽만 있으면 delay=0 버그 방지
            if (info.FrontExecTime <= 0f || info.BackExecTime <= 0f)
                return _atkInfo;
            return info;
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
                ApplyDissolveBounds(spriteRenderer);
            }
        }

        private static readonly int BoundsMinID = Shader.PropertyToID("_BoundsMin");
        private static readonly int BoundsMaxID = Shader.PropertyToID("_BoundsMax");
        private static MaterialPropertyBlock _dissolveMpb;

        private static void ApplyDissolveBounds(SpriteRenderer spriteRenderer)
        {
            var sprite = spriteRenderer.sprite;
            if (sprite == null) return;

            _dissolveMpb ??= new MaterialPropertyBlock();
            var bounds = sprite.bounds;
            spriteRenderer.GetPropertyBlock(_dissolveMpb);
            _dissolveMpb.SetVector(BoundsMinID, new Vector4(bounds.min.x, bounds.min.y, bounds.min.z, 0));
            _dissolveMpb.SetVector(BoundsMaxID, new Vector4(bounds.max.x, bounds.max.y, bounds.max.z, 0));
            spriteRenderer.SetPropertyBlock(_dissolveMpb);
        }

        public void SpriteRendererSetActive(bool isActive)
        {
            foreach (var spriteRenderer in _spriteRendererList)
            {
                spriteRenderer.gameObject.SetActive(isActive);
            }
        }

        public void SetShadowActive(bool isActive)
        {
            if (_shadow != null)
                _shadow.SetActive(isActive);
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
                ApplyDissolveBounds(spriteRenderer);
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
            _viewScaleRaw += new Vector3(viewScale, viewScale, viewScale);
            _viewScaleTarget = Vector3.Max(MinViewScale, Vector3.Min(_viewScaleRaw, MaxViewScale));
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
            _viewScaleRaw -= new Vector3(viewScale, viewScale, viewScale);
            _viewScaleTarget = Vector3.Max(MinViewScale, Vector3.Min(_viewScaleRaw, MaxViewScale));
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
