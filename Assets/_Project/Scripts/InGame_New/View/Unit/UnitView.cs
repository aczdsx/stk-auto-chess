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
        private CombatState _lastState = CombatState.Idle;

        // ── 초기화 ──

        public void Initialize(int entityId, int championSpecId, byte starLevel, string prefabPath)
        {
            EntityId = entityId;
            CombatId = CombatUnit.InvalidId;
            IsCombatUnit = false;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _lastState = CombatState.Idle;
            _isActive = true;
            gameObject.SetActive(true);
            LoadCharacterVisual(prefabPath).Forget();
        }

        public void InitializeAsCombat(int combatId, int sourceEntityId, byte starLevel, string prefabPath)
        {
            EntityId = sourceEntityId;
            CombatId = combatId;
            IsCombatUnit = true;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _lastState = CombatState.Idle;
            _isActive = true;
            gameObject.SetActive(true);
            LoadCharacterVisual(prefabPath).Forget();
        }

        // ── 캐릭터 프리팹 로딩 ──

        private async UniTaskVoid LoadCharacterVisual(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)) return;
            ReleaseCharacterVisual();

            _loadHandle = Addressables.InstantiateAsync(prefabPath, _modelRoot);
            await _loadHandle.WaitUntilDone();
            var go = _loadHandle.Result;

            // 로딩 중 Deactivate 된 경우
            if (!_isActive || go == null)
            {
                ReleaseCharacterVisual();
                return;
            }

            _characterView = go.GetComponent<SpriteCharacterView>();
            _characterView?.PlayAnimation(AnimationKey.IDLE);
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

        public void UpdateHP(int current, int max)
        {
            HPRatio = max > 0 ? (float)current / max : 0f;
        }

        public void UpdateMana(int current, int max)
        {
            ManaRatio = max > 0 ? (float)current / max : 0f;
        }

        public void UpdateStarLevel(byte level)
        {
            StarLevel = level;
            // TODO: 별 레벨 비주얼 업데이트 (파티클, 이펙트)
        }

        // ── 상태 + 애니메이션 ──

        public void SetCombatState(CombatState state)
        {
            if (_characterView == null || state == _lastState) return;
            _lastState = state;

            var animKey = state switch
            {
                CombatState.Idle => AnimationKey.IDLE,
                CombatState.Moving => AnimationKey.MOVE,
                CombatState.Attacking => AnimationKey.ATK,
                CombatState.CastingSkill => AnimationKey.SKL,
                CombatState.Dead => AnimationKey.DEAD,
                CombatState.CrowdControlled => AnimationKey.GROGGY,
                _ => AnimationKey.IDLE,
            };
            _characterView.PlayAnimation(animKey);
        }

        public void PlayAttackAnimation()
        {
            _characterView?.PlayAnimation(AnimationKey.ATK);
        }

        public void PlayHitEffect()
        {
            // TODO: 피격 이펙트 (flash, shake)
        }

        public void PlayDeathAnimation()
        {
            if (_characterView == null) return;
            _lastState = CombatState.Dead;
            _characterView.PlayAnimation(AnimationKey.DEAD);
        }

        // ── 방향 전환 ──

        public void UpdateFacing(Vector3 targetWorldPos)
        {
            if (_characterView == null) return;
            var myPos = transform.position;
            _characterView.LookAt(
                new Vector2(myPos.x, myPos.z),
                new Vector2(targetWorldPos.x, targetWorldPos.z));
        }

        // ── 비활성화 ──

        public void Deactivate()
        {
            _isActive = false;
            ReleaseCharacterVisual();
            gameObject.SetActive(false);
        }

        // ── Unity Lifecycle ──

        private void Update()
        {
            if (!_isActive) return;

            // 위치 보간
            if (Vector3.SqrMagnitude(transform.position - _targetPosition) > 0.001f)
            {
                transform.position = Vector3.Lerp(
                    transform.position, _targetPosition,
                    Time.deltaTime * _interpolationSpeed);
            }
        }
    }
}
