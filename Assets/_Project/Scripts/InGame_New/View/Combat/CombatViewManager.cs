using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 전투 시각 이펙트 관리.
    /// 데미지 텍스트, 투사체 VFX, 스킬 이펙트 등을 처리.
    /// </summary>
    public class CombatViewManager : MonoBehaviour
    {
        private bool _isCombatActive;
        private Transform _vfxRoot;
        private TileEffectManager _tileEffectManager;
        private UnitViewManager _unitViewManager;

        // ── 투사체 VFX 추적 ──
        private readonly List<ActiveProjectile> _activeProjectiles = new();
        private readonly List<ActiveProjectile> _projectilesToRemove = new();

        private struct ActiveProjectile
        {
            public InGameVfx Vfx;
            public InGameVfxMovementBase Movement;
            public ParticleSystem[] Particles;
            public TrailRenderer[] Trails;
        }

        // ── 트레일 페이드아웃 대기 ──
        private readonly List<ActiveProjectile> _dyingProjectiles = new();

        // ── 지연 스폰 큐 (ATK 애니메이션 동기화) ──
        private readonly List<PendingProjectile> _pendingProjectiles = new();

        private struct PendingProjectile
        {
            public float Delay;
            public int SourceId;
            public int TargetId;
            public ProjectileType ProjType;
            public byte Col, Row;
            public sbyte DirCol, DirRow;
            public InGameVfxNameType VfxType;
        }

        // ── 초기화 ──

        public void Initialize()
        {
            _isCombatActive = false;

            // VFX 루트 (동적 생성이므로 코드에서 생성)
            var rootObj = new GameObject("VfxRoot");
            rootObj.transform.SetParent(transform);
            _vfxRoot = rootObj.transform;
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
            _pendingProjectiles.Clear();
            _tileEffectManager?.HideAll();
            ClearAllProjectiles();
        }

        // ── 이벤트 수신 (AutoChessViewBridge에서 호출) ──

        public void OnUnitAttacked(int attackerId, int targetId, int damage, bool isCrit)
        {
            if (!_isCombatActive) return;
            // TODO: 공격 VFX (슬래시, 타격 이펙트)
        }

        public void OnUnitDamaged(int targetId, int damage, DamageType damageType)
        {
            if (!_isCombatActive) return;

            var unitView = _unitViewManager?.FindCombatView(targetId);
            if (unitView == null) return;

            unitView.PlayHitEffect();

            // 피격 VFX
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_hit_01, unitView.transform.position);

            // 데미지 텍스트
            var textView = InGameTextViewPool.Instance.Get();
            if (textView != null)
            {
                float height = unitView.GetCharacterHeight();
                textView.ShowDamageText(unitView.transform.position, height, damage, false).Forget();
            }
        }

        public void OnUnitDied(int entityId)
        {
            if (!_isCombatActive) return;
            // TODO: 사망 VFX
        }

        public void OnUnitCastSkill(int casterId, int skillSpecId, SynergyType element)
        {
            if (!_isCombatActive) return;
            if (_tileEffectManager == null || element == SynergyType.NONE) return;

            if (_unitViewManager != null)
            {
                var unitView = _unitViewManager.FindCombatView(casterId);
                if (unitView != null)
                {
                    var castType = TileEffectManager.SynergyToCastType(element);
                    _tileEffectManager.ShowAt(castType, unitView.transform.position, 1.0f);
                }
            }
        }

        public void OnProjectileSpawned(int sourceId, int targetId, ProjectileType projType,
            byte col, byte row, sbyte dirCol, sbyte dirRow, int champSpecId)
        {
            if (!_isCombatActive) return;

            var vfxType = GetProjectileVfxType(champSpecId);
            if (vfxType == InGameVfxNameType.NONE) return;

            var sourceView = _unitViewManager?.FindCombatView(sourceId);
            if (sourceView == null) return;

            float delay = sourceView.GetAttackExecuteTime();

            _pendingProjectiles.Add(new PendingProjectile
            {
                Delay = delay,
                SourceId = sourceId,
                TargetId = targetId,
                ProjType = projType,
                Col = col,
                Row = row,
                DirCol = dirCol,
                DirRow = dirRow,
                VfxType = vfxType,
            });
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
        }

        // ── 지연 후 투사체 VFX 스폰 ──

        private void SpawnProjectileFromPending(in PendingProjectile p)
        {
            var sourceView = _unitViewManager?.FindCombatView(p.SourceId);
            if (sourceView == null) return;

            Vector3 sourcePos = sourceView.GetProjectileSpawnPosition();

            switch (p.ProjType)
            {
                case ProjectileType.Homing:
                    SpawnHomingProjectile(sourcePos, p.TargetId, p.VfxType);
                    break;
                case ProjectileType.Linear:
                    SpawnLinearProjectile(sourcePos, p.DirCol, p.DirRow, p.VfxType);
                    break;
                case ProjectileType.AreaTarget:
                    SpawnAreaProjectile(sourcePos, p.Col, p.Row, p.VfxType);
                    break;
            }
        }

        // ── 투사체 VFX 생성 ──

        private void SpawnHomingProjectile(Vector3 sourcePos, int targetId, InGameVfxNameType vfxType)
        {
            var targetView = _unitViewManager?.FindCombatView(targetId);
            if (targetView == null) return;

            var vfx = CreateVfx(vfxType, sourcePos);
            if (vfx == null) return;

            Vector3 targetPos = targetView.transform.position + Vector3.up * 0.5f;

            var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
            movement.SetData(sourcePos, targetPos, 30f);

            // 방향 설정
            Vector3 dir = (targetPos - sourcePos).normalized;
            if (dir != Vector3.zero) vfx.CachedTr.rotation = Quaternion.LookRotation(dir);

            vfx.Initialize(false, movement);
            RegisterProjectile(vfx, movement);
        }

        private void SpawnLinearProjectile(Vector3 sourcePos, sbyte dirCol, sbyte dirRow, InGameVfxNameType vfxType)
        {
            var vfx = CreateVfx(vfxType, sourcePos);
            if (vfx == null) return;

            // 방향을 월드 좌표로 변환 (col=x, row=z)
            Vector3 worldDir = new Vector3(dirCol, 0, dirRow).normalized;
            Vector3 destPos = sourcePos + worldDir * 20f;

            var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
            movement.SetData(sourcePos, destPos, 20f);

            if (worldDir != Vector3.zero) vfx.CachedTr.rotation = Quaternion.LookRotation(worldDir);

            vfx.Initialize(false, movement);
            RegisterProjectile(vfx, movement);
        }

        private void SpawnAreaProjectile(Vector3 sourcePos, byte targetCol, byte targetRow, InGameVfxNameType vfxType)
        {
            Vector3 targetPos = BoardWorldHelper.CombatGridToWorld(0, targetCol, targetRow);
            if (targetPos == Vector3.zero) return;

            var vfx = CreateVfx(vfxType, sourcePos);
            if (vfx == null) return;

            var movement = InGameVfxMovementPool.Get<InGameVfxMovementBezier>();
            // Transform 전달 → 베지어 이동 중 회전 자동 적용
            movement.SetData(vfx.CachedTr, sourcePos, targetPos, 10f);

            vfx.Initialize(false, movement);
            RegisterProjectile(vfx, movement);
        }

        private void RegisterProjectile(InGameVfx vfx, InGameVfxMovementBase movement)
        {
            var ap = new ActiveProjectile
            {
                Vfx = vfx,
                Movement = movement,
                Particles = vfx.GetComponentsInChildren<ParticleSystem>(),
                Trails = vfx.GetComponentsInChildren<TrailRenderer>(),
            };
            _activeProjectiles.Add(ap);

            // 도착 시 제거 예약 (Update에서 일괄 처리)
            movement.OnReachedTarget += () => _projectilesToRemove.Add(ap);
        }

        // ── VFX 생성/반환 ──

        private InGameVfx CreateVfx(InGameVfxNameType vfxType, Vector3 position)
        {
            var vfxData = SpecDataManager.Instance.GetInGameVfxData(vfxType);
            if (vfxData == null || string.IsNullOrEmpty(vfxData.addressable_path)) return null;

            var go = Addressables.InstantiateAsync(vfxData.addressable_path).WaitForCompletion();
            if (go == null) return null;

            var vfx = go.GetComponent<InGameVfx>();
            if (vfx == null)
            {
                Addressables.ReleaseInstance(go);
                return null;
            }

            vfx.VfxNameType = vfxType;
            vfx.CachedTr.SetParent(_vfxRoot, true);
            vfx.CachedGo.SetActive(true);
            vfx.CachedTr.position = position;
            return vfx;
        }

        private void ReleaseProjectile(ActiveProjectile ap)
        {
            if (ap.Movement != null) InGameVfxMovementPool.Return(ap.Movement);
            ReleaseVfx(ap);
        }

        private static void ReleaseVfx(ActiveProjectile ap)
        {
            if (ap.Vfx != null && ap.Vfx.CachedGo != null)
            {
                ap.Vfx.Clear();
                Addressables.ReleaseInstance(ap.Vfx.CachedGo);
            }
        }

        private void ClearAllProjectiles()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
                ReleaseProjectile(_activeProjectiles[i]);
            _activeProjectiles.Clear();
            _projectilesToRemove.Clear();

            for (int i = _dyingProjectiles.Count - 1; i >= 0; i--)
                ReleaseVfx(_dyingProjectiles[i]);
            _dyingProjectiles.Clear();
        }

        private static void ClearParticlesImmediate(ParticleSystem[] particles)
        {
            if (particles == null) return;
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                {
                    particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        private static bool IsTrailAlive(TrailRenderer[] trails)
        {
            if (trails == null || trails.Length == 0) return false;
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] != null && trails[i].positionCount > 0)
                    return true;
            }
            return false;
        }

        // ── VFX 업데이트 (매 프레임) ──

        private void Update()
        {
            if (!_isCombatActive) return;

            float dt = Time.deltaTime;

            // 1. 대기 중인 투사체 지연 처리
            for (int i = _pendingProjectiles.Count - 1; i >= 0; i--)
            {
                var p = _pendingProjectiles[i];
                p.Delay -= dt;
                if (p.Delay <= 0f)
                {
                    SpawnProjectileFromPending(in p);
                    _pendingProjectiles.RemoveAt(i);
                }
                else
                {
                    _pendingProjectiles[i] = p;
                }
            }

            // 2. 이동 업데이트 (ManagedUpdate 중 OnReachedTarget → _projectilesToRemove에 수집)
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                var ap = _activeProjectiles[i];
                if (ap.Movement == null || ap.Vfx == null) continue;
                ap.Movement.ManagedUpdate(dt);
                ap.Vfx.CachedTr.position = ap.Movement.CurrentPosition;
            }

            // 도착한 투사체 → 파티클 즉시 제거, 트레일만 페이드아웃 대기
            if (_projectilesToRemove.Count > 0)
            {
                for (int i = 0; i < _projectilesToRemove.Count; i++)
                {
                    var ap = _projectilesToRemove[i];
                    _activeProjectiles.Remove(ap);

                    // movement 풀 반환
                    if (ap.Movement != null) InGameVfxMovementPool.Return(ap.Movement);
                    ap.Movement = null;

                    // 파티클 즉시 정리 (바디/구체 파티클 제거)
                    ClearParticlesImmediate(ap.Particles);

                    if (ap.Trails != null && ap.Trails.Length > 0)
                    {
                        // 트레일이 있으면 페이드아웃 대기
                        _dyingProjectiles.Add(ap);
                    }
                    else
                    {
                        ReleaseVfx(ap);
                    }
                }
                _projectilesToRemove.Clear();
            }

            // 트레일 페이드아웃 완료 체크
            for (int i = _dyingProjectiles.Count - 1; i >= 0; i--)
            {
                var dp = _dyingProjectiles[i];
                if (!IsTrailAlive(dp.Trails))
                {
                    ReleaseVfx(dp);
                    _dyingProjectiles.RemoveAt(i);
                }
            }
        }

        // ── VFX 타입 조회 ──

        private static InGameVfxNameType GetProjectileVfxType(int champSpecId)
        {
            if (champSpecId <= 0) return InGameVfxNameType.NONE;
            var spec = SpecDataManager.Instance.GetSpecCharacter(champSpecId);
            return spec?.projectile_vfx_name_type ?? InGameVfxNameType.NONE;
        }
    }
}
