using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 전투 시각 이펙트 관리.
    /// 데미지 텍스트, 투사체 VFX, 스킬 이펙트 등을 처리.
    /// </summary>
    public class CombatViewManager : MonoBehaviour
    {
        [Header("VFX Prefabs")]
        [SerializeField] private GameObject _hitVfxPrefab;

        private bool _isCombatActive;
        private Transform _vfxRoot;
        private TileEffectManager _tileEffectManager;
        private UnitViewManager _unitViewManager;

        private const float FireAndForgetLifetime = 3f;

        // ── 투사체 VFX 추적 ──
        private readonly List<ActiveProjectile> _activeProjectiles = new();
        private readonly List<ActiveProjectile> _projectilesToRemove = new();

        private struct ActiveProjectile
        {
            public int ProjectileId;
            public InGameVfx Vfx;
            public InGameVfxMovementBase Movement;
            public ParticleSystem[] Particles;
            public TrailRenderer[] Trails;
            public GameObject RawGo; // InGameVfx 없는 fallback VFX용
        }

        private readonly Dictionary<int, int> _projectileIdToIndex = new();

        // ── 트레일 페이드아웃 대기 ──
        private readonly List<ActiveProjectile> _dyingProjectiles = new();

        // ── 지연 스폰 큐 (ATK 애니메이션 동기화) ──
        private readonly List<PendingProjectile> _pendingProjectiles = new();
        private readonly List<PendingMeleeAttack> _pendingMeleeAttacks = new();
        private readonly HashSet<int> _pendingMeleeTargetIds = new();

        private struct PendingProjectile
        {
            public float Delay; // 뷰 레이어 — Time.deltaTime 기반이라 float 불가피
            public int ProjectileId;
            public int SourceId;
            public int TargetId;
            public ProjectileType ProjType;
            public byte Col, Row;
            public sbyte DirCol, DirRow;
            public GameObject VfxPrefab;
        }

        private struct PendingMeleeAttack
        {
            public float Delay; // 뷰 레이어 — Time.deltaTime 기반이라 float 불가피
            public int AttackerId;
            public int TargetId;
            public int Damage;
            public bool IsCrit;
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
            _pendingMeleeAttacks.Clear();
            _pendingMeleeTargetIds.Clear();
            _tileEffectManager?.HideAll();
            ClearAllProjectiles();
        }

        // ── 이벤트 수신 (AutoChessViewBridge에서 호출) ──

        public void OnUnitAttacked(int attackerId, int targetId, int damage, bool isCrit, bool isProjectile, bool isPreTimed = false)
        {
            if (!_isCombatActive) return;

            // ATK/ATK2/CRIT 애니메이션 결정 (damage=0 공격 시작 신호 시)
            if (isPreTimed && damage == 0)
            {
                var view = _unitViewManager?.FindCombatView(attackerId);
                view?.PrepareAttackAnimation(isCrit);
                return;
            }

            // 투사체 공격: 애니메이션 타입 결정 후 OnProjectileSpawned에서 처리
            if (isProjectile)
            {
                // 원거리 공격도 ATK/ATK2/CRIT 애니메이션 결정
                var view = _unitViewManager?.FindCombatView(attackerId);
                view?.PrepareAttackAnimation(isCrit);
                return;
            }

            // 시뮬레이션에서 키프레임 타이밍 완료된 히트: 즉시 표시 (추가 딜레이 없음)
            if (isPreTimed)
            {
                if (damage > 0)
                {
                    ExecuteMeleeHit(new PendingMeleeAttack
                    {
                        AttackerId = attackerId,
                        TargetId = targetId,
                        Damage = damage,
                        IsCrit = isCrit,
                    });
                    _pendingMeleeTargetIds.Add(targetId);
                }
                return;
            }

            // 근접 공격: ATK 애니메이션 execute 타이밍에 맞춰 지연 처리
            var attackerView = _unitViewManager?.FindCombatView(attackerId);
            if (attackerView == null) return;

            var clipType = attackerView.GetCurrentAttackClipType();
            var info = attackerView.GetAtkInfo(clipType);
            bool isFront = attackerView.IsFacingFront();
            float animSpeed = attackerView.AnimatorSpeed;
            int hitCount = info.GetHitCount(isFront);
            float[] hitTimes = info.GetHitTimes(isFront);

            if (hitCount <= 1 || hitTimes == null)
            {
                // 단타
                _pendingMeleeAttacks.Add(new PendingMeleeAttack
                {
                    Delay = info.GetExecTime(isFront) / animSpeed,
                    AttackerId = attackerId,
                    TargetId = targetId,
                    Damage = damage,
                    IsCrit = isCrit,
                });
            }
            else
            {
                // 다타: 각 히트별 타이밍에 분할 데미지
                int perHitDamage = damage / hitCount;
                for (int i = 0; i < hitCount; i++)
                {
                    _pendingMeleeAttacks.Add(new PendingMeleeAttack
                    {
                        Delay = hitTimes[i] / animSpeed,
                        AttackerId = attackerId,
                        TargetId = targetId,
                        Damage = perHitDamage,
                        IsCrit = isCrit,
                    });
                }
            }
            _pendingMeleeTargetIds.Add(targetId);
        }

        public void OnUnitDamaged(int targetId, int damage, DamageType damageType, bool isCrit)
        {
            if (!_isCombatActive) return;

            // 근접 공격 데미지는 OnUnitAttacked에서 지연 처리하므로 스킵
            if (_pendingMeleeTargetIds.Remove(targetId)) return;

            var unitView = _unitViewManager?.FindCombatView(targetId);
            if (unitView == null) return;

            unitView.PlayHitEffect();

            // 피격 VFX
            SpawnFireAndForgetVfx(_hitVfxPrefab, unitView.transform.position);

            // 데미지 텍스트
            var textView = InGameTextViewPool.Instance.Get();
            if (textView != null)
            {
                float height = unitView.GetCharacterHeight();
                textView.ShowDamageText(unitView.transform.position, height, damage, isCrit).Forget();
            }

            // 데미지 사운드
            textView?.PlayDamageSound(isCrit);
        }

        public void OnUnitMissed(int attackerId, int targetId)
        {
            if (!_isCombatActive) return;

            var unitView = _unitViewManager?.FindCombatView(targetId);
            if (unitView == null) return;

            var textView = InGameTextViewPool.Instance.Get();
            if (textView != null)
            {
                float height = unitView.GetCharacterHeight();
                textView.ShowMissText(unitView.transform.position, height).Forget();
            }
        }

        public void OnUnitHealed(int targetId, int amount)
        {
            if (!_isCombatActive) return;

            var unitView = _unitViewManager?.FindCombatView(targetId);
            if (unitView == null) return;

            var textView = InGameTextViewPool.Instance.Get();
            if (textView != null)
            {
                float height = unitView.GetCharacterHeight();
                textView.ShowHealText(unitView.transform.position, height, amount).Forget();
            }
        }

        public void OnUnitDied(int entityId)
        {
            if (!_isCombatActive) return;
            // TODO: 사망 VFX
        }

        public void OnUnitCastSkill(int casterId, int targetId, int skillSpecId, SynergyType element, bool skipVfx = false, bool hasProjectile = false)
        {
            if (!_isCombatActive) return;

            var casterView = _unitViewManager?.FindCombatView(casterId);

            // 원소 타일 이펙트
            if (_tileEffectManager != null && element != SynergyType.NONE && casterView != null)
            {
                var castType = TileEffectManager.SynergyToCastType(element);
                _tileEffectManager.ShowAt(castType, casterView.transform.position, 1.0f);
            }

            // 스킬 사운드 재생
            PlaySkillSound(casterView);

            // 채널링 스킬은 SkillPhaseVfx로 VFX를 제어하므로 여기서 스킵
            if (skipVfx) return;

            // 투사체 스킬은 OnProjectileSpawned에서 VFX를 생성하므로 여기서 스킵
            if (hasProjectile) return;

            // 스킬 VFX (프리팹 직접 참조)
            var skillPrefabs = casterView?.GetSkillEffectPrefabs();
            if (skillPrefabs == null || skillPrefabs.Length == 0) return;

            // 첫 번째 VFX: 캐스터 위치에 재생
            if (casterView != null && skillPrefabs.Length > 0 && skillPrefabs[0]?.Prefab != null)
            {
                SpawnSkillVfx(casterView, skillPrefabs[0]);
            }

            // 두 번째 VFX: 타겟 위치에 재생 (있으면)
            if (skillPrefabs.Length > 1 && skillPrefabs[1]?.Prefab != null && targetId >= 0)
            {
                var targetView = _unitViewManager?.FindCombatView(targetId);
                if (targetView != null)
                {
                    SpawnSkillVfx(targetView, skillPrefabs[1]);
                }
            }
        }

        public void OnSkillPhaseVfx(int casterId, int skillSpecId, byte vfxIndex, sbyte dirCol = 0, sbyte dirRow = 0)
        {
            if (!_isCombatActive) return;

            var casterView = _unitViewManager?.FindCombatView(casterId);
            if (casterView == null) return;

            var skillPrefabs = casterView.GetSkillEffectPrefabs();
            if (skillPrefabs == null || vfxIndex >= skillPrefabs.Length) return;

            if (dirCol != 0 || dirRow != 0)
                SpawnSkillVfxDirectional(casterView, skillPrefabs[vfxIndex], dirCol, dirRow);
            else
                SpawnSkillVfx(casterView, skillPrefabs[vfxIndex]);
        }

        public void OnSkillRectAreaEffect(int col, int row, sbyte dirCol, sbyte dirRow, SynergyType element)
        {
            if (!_isCombatActive) return;
            if (_tileEffectManager == null) return;

            if (element != SynergyType.NONE)
            {
                var areaType = TileEffectManager.SynergyToAreaType(element);
                _tileEffectManager.ShowDirectionalRect(areaType, col, row, dirCol, dirRow, 0.5f);
            }
        }

        public void OnProjectileSpawned(int sourceId, int targetId, ProjectileType projType,
            byte col, byte row, sbyte dirCol, sbyte dirRow, int champSpecId, int projectileId, int skillSpecId = 0)
        {
            if (!_isCombatActive) return;

            var sourceView = _unitViewManager?.FindCombatView(sourceId);
            if (sourceView == null) return;

            var prefab = sourceView.GetProjectilePrefab();

            // fallback: 투사체 프리팹이 없으면 skillPrefabs[0]을 사용 (아트레시아 등)
            if (prefab == null)
            {
                var skillPrefabs = sourceView.GetSkillEffectPrefabs();
                if (skillPrefabs != null && skillPrefabs.Length > 0 && skillPrefabs[0]?.Prefab != null)
                    prefab = skillPrefabs[0].Prefab;
            }

            if (prefab == null) return;

            // 스킬 투사체: 시뮬레이션이 이미 SKL exec 타이밍에 맞춰 Execute를 호출하므로 추가 딜레이 불필요
            // 기본공격 투사체: ATK 애니메이션 exec 타이밍에 맞춰 딜레이 적용
            float delay = 0f;
            if (skillSpecId <= 0)
            {
                var info = sourceView.GetAtkInfo();
                bool isFront = sourceView.IsFacingFront();
                float animSpeed = sourceView.AnimatorSpeed;
                delay = info.GetExecTime(isFront) / animSpeed;
            }

            _pendingProjectiles.Add(new PendingProjectile
            {
                Delay = delay,
                ProjectileId = projectileId,
                SourceId = sourceId,
                TargetId = targetId,
                ProjType = projType,
                Col = col,
                Row = row,
                DirCol = dirCol,
                DirRow = dirRow,
                VfxPrefab = prefab,
            });
        }

        public void OnProjectileMoved(int projectileId, byte col, byte row)
        {
            if (!_isCombatActive) return;
            if (!_projectileIdToIndex.TryGetValue(projectileId, out int idx)) return;
            if (idx < 0 || idx >= _activeProjectiles.Count) return;

            var ap = _activeProjectiles[idx];
            Vector3 targetPos = BoardWorldHelper.CombatGridToWorld(0, col, row);

            if (ap.Movement is InGameVfxMovementLinear linearMov)
            {
                // 목적지를 새 타일 위치로 갱신 (속도 20f: SpawnLinearProjectile에서 사용하는 값)
                Vector3 currentPos = linearMov.CurrentPosition;
                linearMov.SetData(currentPos, targetPos, 20f);
            }
            else if (ap.RawGo != null)
            {
                // SimpleLinearMover fallback: 위치를 직접 이동
                ap.RawGo.transform.position = targetPos;
            }
        }

        public void OnProjectileExpired(int projectileId)
        {
            if (!_isCombatActive) return;
            if (!_projectileIdToIndex.TryGetValue(projectileId, out int idx)) return;
            if (idx < 0 || idx >= _activeProjectiles.Count) return;

            var ap = _activeProjectiles[idx];
            _projectileIdToIndex.Remove(projectileId);

            // 인덱스 맵 갱신: 마지막 요소를 현재 위치로 이동
            _activeProjectiles.RemoveAt(idx);
            RebuildProjectileIdIndex();

            // VFX 정리
            if (ap.Movement != null) InGameVfxMovementPool.Return(ap.Movement);
            if (ap.Vfx != null && ap.Vfx.CachedGo != null)
            {
                ap.Vfx.Clear();
                Object.Destroy(ap.Vfx.CachedGo);
            }
            if (ap.RawGo != null)
            {
                Object.Destroy(ap.RawGo);
            }
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

        public void OnSkillAreaEffect(int col, int row, int radius, SynergyType element, bool isRow, bool isBox = false)
        {
            if (!_isCombatActive) return;
            if (_tileEffectManager == null) return;

            if (element != SynergyType.NONE)
            {
                var areaType = TileEffectManager.SynergyToAreaType(element);
                if (isRow)
                    _tileEffectManager.ShowRow(areaType, row, col, radius, 0.5f);
                else if (isBox)
                    _tileEffectManager.ShowRangeBox(areaType, col, row, radius, 0.5f);
                else
                    _tileEffectManager.ShowRange(areaType, col, row, radius, 0.5f);
            }
        }

        /// <summary>타일 이펙트 표시 (ViewBridge에서 호출)</summary>
        public void ShowTileEffectAt(TileEffectType castType, Vector3 worldPos, float duration = 0.5f)
        {
            _tileEffectManager?.ShowAt(castType, worldPos, duration);
        }

        // ── 스킬 사운드 ──

        private static void PlaySkillSound(UnitView casterView)
        {
            if (casterView == null || casterView.ChampSpecId <= 0) return;
            var spec = SpecDataManager.Instance.GetSpecCharacter(casterView.ChampSpecId);
            if (spec == null) return;

            var resolver = BattleSystem.SkillSoundResolver.Create(spec.prefab_id);
            var soundNames = resolver.GetCachedSoundNames();
            if (soundNames != null && soundNames.Length > 0)
                SoundManager.Instance.PlaySkillSounds(soundNames);
        }

        // ── Fire-and-forget VFX (피격, 스킬 이펙트) ──

        private void SpawnSkillVfx(UnitView unitView, SkillViewData skillData)
        {
            if (skillData?.Prefab == null || unitView == null) return;
            var posTransform = unitView.GetSkillPositionTransform(skillData.Position);
            var vfxObj = Instantiate(skillData.Prefab, posTransform.position, posTransform.rotation);
            if (skillData.Followable)
                vfxObj.transform.SetParent(posTransform);
            else if (_vfxRoot != null)
                vfxObj.transform.SetParent(_vfxRoot);

            Destroy(vfxObj, FireAndForgetLifetime);
        }

        private static readonly Vector3 VfxFlipScale = new Vector3(1, 1, -1);

        /// <summary>방향이 있는 스킬 VFX 생성. 기존 EffectCodeSkill217613501의 rotation+scale flip 로직 재현.</summary>
        private void SpawnSkillVfxDirectional(UnitView unitView, SkillViewData skillData, sbyte dirCol, sbyte dirRow)
        {
            if (skillData?.Prefab == null || unitView == null) return;
            var posTransform = unitView.GetSkillPositionTransform(skillData.Position);
            var vfxObj = Instantiate(skillData.Prefab, posTransform.position, Quaternion.identity);

            if (_vfxRoot != null)
                vfxObj.transform.SetParent(_vfxRoot);

            // 기존 코드와 동일: 실제 타일 월드 좌표 차이로 방향 계산
            Vector3 casterWorldPos = posTransform.position;
            BoardWorldHelper.WorldToBoard(casterWorldPos, out _, out int casterCol, out int casterRow);
            int fwdCol = casterCol + dirCol;
            int fwdRow = casterRow + dirRow;
            var fwdWorldPos = BoardWorldHelper.CombatGridToWorld(0, fwdCol, fwdRow);
            Vector3 direction = (fwdWorldPos - casterWorldPos).normalized;

            if (direction != Vector3.zero)
            {
                vfxObj.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90f, 0);
            }

            // scale flip: 기존 조건 dirCol > 0 || dirRow < 0
            if (dirCol > 0 || dirRow < 0)
                vfxObj.transform.localScale = VfxFlipScale;
            else
                vfxObj.transform.localScale = Vector3.one;

            Destroy(vfxObj, FireAndForgetLifetime);
        }

        private void SpawnFireAndForgetVfx(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return;
            var go = Instantiate(prefab, position, Quaternion.identity, _vfxRoot);
            Destroy(go, FireAndForgetLifetime);
        }

        // ── 지연 후 근접 공격 실행 ──

        private void ExecuteMeleeHit(in PendingMeleeAttack m)
        {
            var targetView = _unitViewManager?.FindCombatView(m.TargetId);
            if (targetView == null) return;

            // 피격 이펙트
            targetView.PlayHitEffect();

            // 피격 VFX
            SpawnFireAndForgetVfx(_hitVfxPrefab, targetView.transform.position);

            // 데미지 텍스트
            var textView = InGameTextViewPool.Instance.Get();
            if (textView != null)
            {
                float height = targetView.GetCharacterHeight();
                textView.ShowDamageText(targetView.transform.position, height, m.Damage, m.IsCrit).Forget();
                textView.PlayDamageSound(m.IsCrit);
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
                    SpawnHomingProjectile(sourcePos, p.TargetId, p.VfxPrefab, p.ProjectileId);
                    break;
                case ProjectileType.Linear:
                    SpawnLinearProjectile(sourcePos, p.Col, p.Row, p.DirCol, p.DirRow, p.VfxPrefab, p.ProjectileId);
                    break;
                case ProjectileType.AreaTarget:
                    SpawnAreaProjectile(sourcePos, p.Col, p.Row, p.VfxPrefab, p.ProjectileId);
                    break;
            }
        }

        // ── 투사체 VFX 생성 ──

        private void SpawnHomingProjectile(Vector3 sourcePos, int targetId, GameObject prefab, int projectileId)
        {
            var targetView = _unitViewManager?.FindCombatView(targetId);
            if (targetView == null) return;

            var vfx = CreateVfx(prefab, sourcePos);
            if (vfx == null) return;

            Vector3 targetPos = targetView.transform.position + Vector3.up * 0.5f;

            var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
            movement.SetData(sourcePos, targetPos, 30f);

            // 방향 설정
            Vector3 dir = (targetPos - sourcePos).normalized;
            if (dir != Vector3.zero) vfx.CachedTr.rotation = Quaternion.LookRotation(dir);

            vfx.Initialize(false, movement);
            RegisterProjectile(vfx, movement, projectileId);
        }

        private void SpawnLinearProjectile(Vector3 sourcePos, byte startCol, byte startRow, sbyte dirCol, sbyte dirRow, GameObject prefab, int projectileId)
        {
            // 시전자 타일 월드 좌표에서 시작 (원본 코드: owner.CurrentTile.View.CachedTr.position)
            Vector3 startWorldPos = BoardWorldHelper.CombatGridToWorld(0, startCol, startRow);
            Vector3 nextTilePos = BoardWorldHelper.CombatGridToWorld(0, startCol + dirCol, startRow + dirRow);

            // 방향 계산: 인접 타일 간 월드 좌표 차이 (범위 밖 안전 처리)
            Vector3 worldDir;
            if (nextTilePos != Vector3.zero && startWorldPos != Vector3.zero)
            {
                worldDir = (nextTilePos - startWorldPos).normalized;
            }
            else
            {
                // 경계 타일인 경우: (0,0)→(1,0)과 (0,0)→(0,1)로 방향 벡터 산출
                Vector3 origin = BoardWorldHelper.CombatGridToWorld(0, 0, 0);
                Vector3 colUnit = BoardWorldHelper.CombatGridToWorld(0, 1, 0) - origin;
                Vector3 rowUnit = BoardWorldHelper.CombatGridToWorld(0, 0, 1) - origin;
                worldDir = (dirCol * colUnit + dirRow * rowUnit).normalized;
                nextTilePos = startWorldPos + worldDir * colUnit.magnitude;
            }

            var vfx = CreateVfx(prefab, startWorldPos);
            if (vfx != null)
            {
                var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
                movement.SetData(startWorldPos, nextTilePos, 20f);

                if (worldDir != Vector3.zero)
                    vfx.CachedTr.rotation = Quaternion.LookRotation(worldDir) * Quaternion.Euler(0, -90f, 0);

                vfx.Initialize(false, movement);
                // Linear 투사체는 OnReachedTarget으로 제거하지 않음 (ProjectileExpired로 제거)
                RegisterLinearProjectile(vfx, movement, projectileId);
            }
            else
            {
                // InGameVfx 컴포넌트가 없는 스킬 VFX 프리팹 fallback (아트레시아 등)
                var go = Instantiate(prefab, startWorldPos, Quaternion.identity, _vfxRoot);
                if (worldDir != Vector3.zero)
                    go.transform.rotation = Quaternion.LookRotation(worldDir) * Quaternion.Euler(0, -90f, 0);

                // projectileId로 추적하여 ProjectileMoved/Expired에서 위치 제어
                var ap = new ActiveProjectile
                {
                    ProjectileId = projectileId,
                    RawGo = go,
                };
                _activeProjectiles.Add(ap);
                _projectileIdToIndex[projectileId] = _activeProjectiles.Count - 1;
            }
        }

        private void SpawnAreaProjectile(Vector3 sourcePos, byte targetCol, byte targetRow, GameObject prefab, int projectileId = 0)
        {
            Vector3 targetPos = BoardWorldHelper.CombatGridToWorld(0, targetCol, targetRow);
            if (targetPos == Vector3.zero) return;

            var vfx = CreateVfx(prefab, sourcePos);
            if (vfx == null) return;

            var movement = InGameVfxMovementPool.Get<InGameVfxMovementBezier>();
            // Transform 전달 → 베지어 이동 중 회전 자동 적용
            movement.SetData(vfx.CachedTr, sourcePos, targetPos, 10f);

            vfx.Initialize(false, movement);
            RegisterProjectile(vfx, movement);
        }

        /// <summary>Linear 투사체 등록. OnReachedTarget으로 제거하지 않음 (ProjectileExpired로만 제거).</summary>
        private void RegisterLinearProjectile(InGameVfx vfx, InGameVfxMovementBase movement, int projectileId)
        {
            var ap = new ActiveProjectile
            {
                ProjectileId = projectileId,
                Vfx = vfx,
                Movement = movement,
                Particles = vfx.GetComponentsInChildren<ParticleSystem>(),
                Trails = vfx.GetComponentsInChildren<TrailRenderer>(),
            };
            _activeProjectiles.Add(ap);
            if (projectileId != 0)
                _projectileIdToIndex[projectileId] = _activeProjectiles.Count - 1;
            // OnReachedTarget을 등록하지 않음: 매 타일마다 목적지가 갱신되므로
        }

        private void RegisterProjectile(InGameVfx vfx, InGameVfxMovementBase movement, int projectileId = 0)
        {
            var ap = new ActiveProjectile
            {
                ProjectileId = projectileId,
                Vfx = vfx,
                Movement = movement,
                Particles = vfx.GetComponentsInChildren<ParticleSystem>(),
                Trails = vfx.GetComponentsInChildren<TrailRenderer>(),
            };
            _activeProjectiles.Add(ap);
            if (projectileId != 0)
                _projectileIdToIndex[projectileId] = _activeProjectiles.Count - 1;

            // 도착 시 제거 예약 (Update에서 일괄 처리)
            movement.OnReachedTarget += () => _projectilesToRemove.Add(ap);
        }

        private void RebuildProjectileIdIndex()
        {
            _projectileIdToIndex.Clear();
            for (int i = 0; i < _activeProjectiles.Count; i++)
            {
                int pid = _activeProjectiles[i].ProjectileId;
                if (pid != 0)
                    _projectileIdToIndex[pid] = i;
            }
        }

        // ── VFX 생성/반환 ──

        private InGameVfx CreateVfx(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;

            var go = Instantiate(prefab, position, Quaternion.identity, _vfxRoot);
            if (go == null) return null;

            var vfx = go.GetComponent<InGameVfx>();
            if (vfx == null)
            {
                Destroy(go);
                return null;
            }

            go.SetActive(true);
            return vfx;
        }

        private void ReleaseProjectile(ActiveProjectile ap)
        {
            if (ap.Movement != null) InGameVfxMovementPool.Return(ap.Movement);
            ReleaseVfx(ap);
            if (ap.RawGo != null) Object.Destroy(ap.RawGo);
        }

        private static void ReleaseVfx(ActiveProjectile ap)
        {
            if (ap.Vfx != null && ap.Vfx.CachedGo != null)
            {
                ap.Vfx.Clear();
                Object.Destroy(ap.Vfx.CachedGo);
            }
        }

        private void ClearAllProjectiles()
        {
            for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
                ReleaseProjectile(_activeProjectiles[i]);
            _activeProjectiles.Clear();
            _projectilesToRemove.Clear();
            _projectileIdToIndex.Clear();

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

            float dt = Time.unscaledDeltaTime * LocalSimulationRunner.SpeedMultiplier;

            // 1-a. 대기 중인 근접 공격 지연 처리
            for (int i = _pendingMeleeAttacks.Count - 1; i >= 0; i--)
            {
                var m = _pendingMeleeAttacks[i];
                m.Delay -= dt;
                if (m.Delay <= 0f)
                {
                    ExecuteMeleeHit(in m);
                    _pendingMeleeAttacks.RemoveAt(i);
                }
                else
                {
                    _pendingMeleeAttacks[i] = m;
                }
            }

            // 1-b. 대기 중인 투사체 지연 처리
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
                RebuildProjectileIdIndex();
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
    }
}
