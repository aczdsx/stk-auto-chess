using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 전투 시각 이펙트 관리.
    /// 데미지 텍스트, 투사체 VFX, 스킬 이펙트 등을 처리.
    /// </summary>
    public class CombatViewManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform _vfxRoot;

        private bool _isCombatActive;

        // ── 초기화 ──

        public void Initialize()
        {
            _isCombatActive = false;
        }

        public void OnCombatStart()
        {
            _isCombatActive = true;
        }

        public void OnCombatEnd()
        {
            _isCombatActive = false;
            // TODO: 잔여 VFX 정리
        }

        // ── 이벤트 수신 (AutoChessViewBridge에서 호출) ──

        public void OnUnitAttacked(int attackerId, int targetId, int damage, bool isCrit)
        {
            if (!_isCombatActive) return;
            // TODO: 공격 VFX (슬래시, 타격 이펙트)
            // TODO: 크리티컬 시 추가 연출
        }

        public void OnUnitDamaged(int targetId, int damage, DamageType damageType)
        {
            if (!_isCombatActive) return;
            // TODO: 데미지 텍스트 팝업 (물리=흰색, 마법=파랑, 고정=보라)
            // TODO: 피격 이펙트 (flash)
        }

        public void OnUnitDied(int entityId)
        {
            if (!_isCombatActive) return;
            // TODO: 사망 VFX (파티클, 페이드아웃)
        }

        public void OnUnitCastSkill(int casterId, int skillSpecId)
        {
            if (!_isCombatActive) return;
            // TODO: 스킬 시전 VFX
            // TODO: 컷씬 연출 (카메라 줌 등)
        }

        public void OnProjectileSpawned(int sourceId, int targetId, ProjectileType projType)
        {
            if (!_isCombatActive) return;
            // TODO: 투사체 비주얼 생성 (Homing/Linear/Area 구분)
        }

        public void OnProjectileExploded(int col, int row, int radius)
        {
            if (!_isCombatActive) return;
            // TODO: 범위 폭발 VFX
            Vector3 worldPos = BoardWorldHelper.CombatGridToWorld(0, col, row);
            // TODO: worldPos에 폭발 이펙트 재생
        }
    }
}
