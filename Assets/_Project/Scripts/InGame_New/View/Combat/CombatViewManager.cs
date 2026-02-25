using CookApps.AutoBattler;
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
        private TileEffectManager _tileEffectManager;
        private UnitViewManager _unitViewManager;

        // ── 초기화 ──

        public void Initialize()
        {
            _isCombatActive = false;
        }

        public void SetTileEffectManager(TileEffectManager manager) => _tileEffectManager = manager;
        public void SetUnitViewManager(UnitViewManager manager) => _unitViewManager = manager;

        public void OnCombatStart()
        {
            _isCombatActive = true;
        }

        public void OnCombatEnd()
        {
            _isCombatActive = false;
            _tileEffectManager?.HideAll();
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

        public void OnUnitCastSkill(int casterId, int skillSpecId, SynergyType element)
        {
            if (!_isCombatActive) return;
            if (_tileEffectManager == null || element == SynergyType.NONE) return;

            // 시전자 위치에 캐스트 이펙트 재생
            if (_unitViewManager != null)
            {
                var unitView = _unitViewManager.FindCombatView(casterId);
                if (unitView != null)
                {
                    var castType = TileEffectManager.SynergyToCastType(element);
                    _tileEffectManager.ShowAt(castType, unitView.transform.position, 1.0f);
                }
            }

            // TODO: 스킬 시전 VFX
            // TODO: 컷씬 연출 (카메라 줌 등)
        }

        public void OnProjectileSpawned(int sourceId, int targetId, ProjectileType projType)
        {
            if (!_isCombatActive) return;
            // TODO: 투사체 비주얼 생성 (Homing/Linear/Area 구분)
        }

        public void OnProjectileExploded(int col, int row, int radius, SynergyType element)
        {
            if (!_isCombatActive) return;
            if (_tileEffectManager == null) return;

            if (element != SynergyType.NONE)
            {
                var areaType = TileEffectManager.SynergyToAreaType(element);
                _tileEffectManager.ShowRange(areaType, col, row, radius, 1.5f);
            }

            // TODO: 범위 폭발 기본 VFX (원소 무관)
        }
    }
}
