using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 개별 유닛 비주얼. 보드/벤치/전투 유닛의 시각적 표현.
    /// 위치 보간, HP/마나 바, 상태 애니메이션 관리.
    /// </summary>
    public class UnitView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private SpriteRenderer _selectionIndicator;

        // ── 상태 ──
        public int EntityId { get; private set; }
        public int CombatId { get; private set; } = CombatUnit.InvalidId;
        public bool IsCombatUnit { get; private set; }
        public bool IsReady => _characterView != null;

        private Vector3 _targetPosition;
        private float _interpolationSpeed = 15f;
        private bool _isActive = true;

        // HP/Mana (0-1 정규화)
        public float HPRatio { get; private set; } = 1f;
        public float ManaRatio { get; private set; }
        public byte StarLevel { get; private set; } = 1;

        // ── 캐릭터 비주얼 ──
        private SpriteCharacterView _characterView;
        private AsyncOperationHandle<GameObject> _loadHandle;

        // ── HP 바 ──
        private HpBarView _hpBarView;
        private int _champSpecId;
        private bool _isPlayer;

        // ── Desired State (로딩 전 호출된 상태를 추적, 로딩 후 ApplyDeferredState로 일괄 적용) ──
        private CombatState _lastState = CombatState.Idle;
        private float _attackAnimEndTime;
        private bool _isHologram;
        private Vector3? _facingTarget;

        // ── 초기화 ──

        public void Initialize(int entityId, byte starLevel, string prefabPath, int champSpecId = 0)
        {
            EntityId = entityId;
            CombatId = CombatUnit.InvalidId;
            IsCombatUnit = false;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _champSpecId = champSpecId;
            _isPlayer = true;
            _lastState = CombatState.Idle;
            _attackAnimEndTime = 0f;
            _isHologram = false;
            _facingTarget = null;
            _isActive = true;
            ReleaseHpBar();
            gameObject.SetActive(true);
            LoadCharacterVisual(prefabPath).Forget();
        }

        public void InitializeAsCombat(int combatId, int sourceEntityId, byte starLevel, string prefabPath,
            int champSpecId = 0, bool isPlayer = true)
        {
            EntityId = sourceEntityId;
            CombatId = combatId;
            IsCombatUnit = true;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _champSpecId = champSpecId;
            _isPlayer = isPlayer;
            _lastState = CombatState.Idle;
            _attackAnimEndTime = 0f;
            _isHologram = false;
            _facingTarget = null;
            _isActive = true;
            ReleaseHpBar();
            gameObject.SetActive(true);
            LoadCharacterVisual(prefabPath).Forget();
        }

        // ── 캐릭터 프리팹 로딩 ──

        private async UniTaskVoid LoadCharacterVisual(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) return;
            ReleaseCharacterVisual();

            var handle = Addressables.InstantiateAsync(prefabPath, _modelRoot);
            _loadHandle = handle;
            await handle.WaitUntilDone();

            // await 완료 후: _loadHandle이 교체되었으면 이 결과는 버림
            if (!_loadHandle.Equals(handle))
            {
                if (handle.IsValid())
                    Addressables.ReleaseInstance(handle);
                return;
            }

            var go = handle.Result;

            if (!_isActive || go == null)
            {
                ReleaseCharacterVisual();
                return;
            }

            _characterView = go.GetComponent<SpriteCharacterView>();
            _characterView?.PlayAnimation(AnimationKey.IDLE);
            ApplyDeferredState();
        }

        /// <summary>로딩 완료 후 지연된 상태를 일괄 적용</summary>
        private void ApplyDeferredState()
        {
            if (_characterView == null) return;

            if (_isHologram)
                _characterView.SetHologramShader();

            if (_facingTarget.HasValue)
                ApplyFacing();

            // 전투 유닛이면 HP 바 부착
            if (IsCombatUnit && _champSpecId > 0)
                AttachHpBar();
        }

        private void AttachHpBar()
        {
            if (_hpBarView != null || _characterView == null) return;

            _hpBarView = InGameHpBarViewPool.Instance.Get();
            if (_hpBarView == null) return;

            _hpBarView.Initialize(_champSpecId, _isPlayer);
            _hpBarView.SetHpBarType(HpBarType.HpBar | HpBarType.Buff);

            var spec = SpecDataManager.Instance.GetSpecCharacter(_champSpecId);
            float height = spec?.height ?? 1.5f;
            _characterView.SetHpBarView(_hpBarView, height);
        }

        private void ReleaseHpBar()
        {
            if (_hpBarView == null) return;
            _hpBarView.OnPreReturn();
            InGameHpBarViewPool.Instance.Return(_hpBarView);
            _hpBarView = null;
        }

        private void ReleaseCharacterVisual()
        {
            _characterView = null;
            if (_loadHandle.IsValid())
            {
                Addressables.ReleaseInstance(_loadHandle);
                _loadHandle = default;
            }
        }

        // ── 위치 업데이트 ──

        /// <summary>부드러운 보간 이동 (활성 보드)</summary>
        public void SetTargetPosition(Vector3 pos)
        {
            _targetPosition = pos;
        }

        /// <summary>즉시 이동 (비활성 보드 / 초기 배치)</summary>
        public void SetPositionImmediate(Vector3 pos)
        {
            _targetPosition = pos;
            transform.position = pos;
        }

        // ── 스탯 업데이트 ──

        public void UpdateHP(int current, int max, int shield = 0)
        {
            HPRatio = max > 0 ? (float)current / max : 0f;
            _hpBarView?.SetValue(current, max, shield);
        }

        public void UpdateMana(int current, int max)
        {
            ManaRatio = max > 0 ? (float)current / max : 0f;
            _hpBarView?.OnCoolTimeUpdated(0, current, max);
        }

        public void UpdateStarLevel(byte level)
        {
            StarLevel = level;
            // TODO: 별 레벨 비주얼 업데이트 (파티클, 이펙트)
        }

        // ── 상태 + 애니메이션 ──

        public void SetCombatState(CombatState state)
        {
            // 공격/스킬 애니메이션 재생 중이면 Idle 전환 차단 (트리거가 애니메이션을 중단시키는 것 방지)
            if (state == CombatState.Idle && Time.time < _attackAnimEndTime)
                return;

            if (state == _lastState) return;
            if (_characterView == null) return;
            _lastState = state;

            var clip = _characterView.PlayAnimation(StateToAnimKey(state));

            if ((state == CombatState.Attacking || state == CombatState.CastingSkill) && clip != null)
                _attackAnimEndTime = Time.time + clip.length;
        }

        /// <summary>전투 종료 시 강제 Idle 전환 (_attackAnimEndTime 보호 무시)</summary>
        public void ForceIdle()
        {
            if (_lastState == CombatState.Dead) return;
            if (_characterView == null) return;
            _attackAnimEndTime = 0f;
            _lastState = CombatState.Idle;
            _characterView.PlayAnimation(AnimationKey.IDLE);
        }

        private static AnimationKey StateToAnimKey(CombatState state)
        {
            return state switch
            {
                CombatState.Idle => AnimationKey.IDLE,
                CombatState.Moving => AnimationKey.MOVE,
                CombatState.Attacking => AnimationKey.ATK,
                CombatState.CastingSkill => AnimationKey.SKL,
                CombatState.Dead => AnimationKey.DEAD,
                CombatState.CrowdControlled => AnimationKey.GROGGY,
                _ => AnimationKey.IDLE,
            };
        }

        /// <summary>ATK 키프레임 정보 (ms 기반, float 없음) 반환</summary>
        public AnimKeyframeInfo GetAtkInfo()
        {
            if (_characterView == null) return default;
            return _characterView.GetAtkInfo();
        }

        /// <summary>현재 방향(front/back) 반환</summary>
        public bool IsFacingFront()
        {
            return _characterView != null && _characterView.CachedFront;
        }

        /// <summary>현재 Animator 재생 속도 (슬로우 디버프 등 반영)</summary>
        public float AnimatorSpeed => _characterView != null ? _characterView.AnimatorSpeed : 1f;

        public Vector3 GetProjectileSpawnPosition()
        {
            if (_characterView == null) return transform.position;
            return _characterView.GetProjectileSpawnPosition();
        }

        public GameObject GetProjectilePrefab()
        {
            return _characterView != null ? _characterView.ProjectilePrefab : null;
        }

        public SkillViewData[] GetSkillEffectPrefabs()
        {
            return _characterView != null ? _characterView.SkillEffectPrefabs : null;
        }

        public Transform GetSkillPositionTransform(SkillPosition pos)
        {
            if (_characterView == null) return transform;
            switch (pos)
            {
                case SkillPosition.SKILL_ROOT:       return _characterView.SkillRootTransform;
                case SkillPosition.SKILL_TOP:        return _characterView.SkillTopFXTransform;
                case SkillPosition.SKILL_MIDDLE:     return _characterView.SkillMiddleFXTransform;
                case SkillPosition.SKILL_BOTTOM:     return _characterView.SkillBottomFXTransform;
                default:                             return transform;
            }
        }

        public float GetCharacterHeight()
        {
            if (_champSpecId <= 0) return 1.5f;
            var spec = SpecDataManager.Instance.GetSpecCharacter(_champSpecId);
            return spec?.height ?? 1.5f;
        }

        public void PlayAttackAnimation()
        {
            _characterView?.PlayAnimation(AnimationKey.ATK);
        }

        public void PlayHitEffect()
        {
            _characterView?.OnHit();
        }

        public void PlayDeathAnimation()
        {
            if (_lastState == CombatState.Dead) return;
            if (_characterView == null) return;
            _lastState = CombatState.Dead;
            _attackAnimEndTime = 0f;
            ReleaseHpBar();
            _characterView.SetShadowActive(false);
            var clip = _characterView.PlayAnimation(AnimationKey.DEAD);
            if (clip != null)
                _characterView.SetDeadSprite(clip);
        }

        // ── 방향 전환 ──

        public void UpdateFacing(Vector3 targetWorldPos)
        {
            _facingTarget = targetWorldPos;
            if (_characterView == null) return;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (_characterView == null || !_facingTarget.HasValue) return;
            var myPos = transform.position;
            _characterView.LookAt(
                new Vector2(myPos.z, myPos.x),
                new Vector2(_facingTarget.Value.z, _facingTarget.Value.x));
        }

        // ── 홀로그램 ──

        public void SetHologram(bool isHologram)
        {
            _isHologram = isHologram;
            if (_characterView == null) return;
            if (isHologram)
                _characterView.SetHologramShader();
            else
                _characterView.SetDisolveShader();
        }

        // ── 비활성화 ──

        public void Deactivate()
        {
            _isActive = false;
            ReleaseHpBar();
            ReleaseCharacterVisual();
            gameObject.SetActive(false);
        }

        // ── Unity Lifecycle ──

        private void Update()
        {
            if (!_isActive) return;

            // Preparation 페이즈 전용 보간 (전투 중은 SyncCombatUnits에서 SetPositionImmediate 사용)
            if (!IsCombatUnit && Vector3.SqrMagnitude(transform.position - _targetPosition) > 0.001f)
            {
                transform.position = Vector3.Lerp(
                    transform.position, _targetPosition,
                    Time.deltaTime * _interpolationSpeed);
            }
        }
    }
}
