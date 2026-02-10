using UnityEngine;

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

        // ── 초기화 ──

        public void Initialize(int entityId, int championSpecId, byte starLevel)
        {
            EntityId = entityId;
            CombatId = CombatUnit.InvalidId;
            IsCombatUnit = false;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _isActive = true;
            gameObject.SetActive(true);
        }

        public void InitializeAsCombat(int combatId, int sourceEntityId, byte starLevel)
        {
            EntityId = sourceEntityId;
            CombatId = combatId;
            IsCombatUnit = true;
            StarLevel = starLevel;
            HPRatio = 1f;
            ManaRatio = 0f;
            _isActive = true;
            gameObject.SetActive(true);
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

        // ── 상태 ──

        public void SetCombatState(CombatState state)
        {
            // TODO: Spine 애니메이션 전환
            // Idle → idle, Moving → walk, Attacking → attack, Dead → death
        }

        public void PlayAttackAnimation()
        {
            // TODO: Spine attack 트리거
        }

        public void PlayHitEffect()
        {
            // TODO: 피격 이펙트 (flash, shake)
        }

        public void PlayDeathAnimation()
        {
            // TODO: 사망 애니메이션 + 페이드아웃
        }

        public void Deactivate()
        {
            _isActive = false;
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
