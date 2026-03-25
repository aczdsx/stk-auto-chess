using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 직업 패시브 VFX 통합 설정 SO.
    /// 투사체 오버라이드 + 유닛 부착 이펙트(버프/발동)를 하나에서 관리.
    /// CombatVfxConfigSO와 분리 — 직업 패시브는 여기서만 관리.
    /// 모든 VFX 프리팹은 AssetReference로 참조 — 번들 중복 방지.
    /// 사용 전 PreloadAsync() 호출 필수, 사용 후 ReleaseAll() 호출.
    /// </summary>
    [CreateAssetMenu(fileName = "JobPassiveVfxConfig", menuName = "AutoChess/Job Passive Vfx Config")]
    public class JobPassiveVfxConfigSO : ScriptableObject
    {
        // ═══════════════════════════════════
        //  투사체 VFX 오버라이드 (Sharpshooter 등)
        // ═══════════════════════════════════

        [Serializable]
        public struct ProjectileOverrideEntry
        {
            [Tooltip("ProjectileVfxId 값 (Enums.cs 참조)")]
            public byte Id;
            public string Label;
            public AssetReferenceGameObject Prefab;
        }

        [Header("── 투사체 VFX 오버라이드 ──")]
        [SerializeField] private ProjectileOverrideEntry[] _projectileOverrides;

        private Dictionary<byte, GameObject> _projectileCache;

        public GameObject GetProjectileOverride(byte id)
        {
            if (id == 0) return null;
            if (_projectileCache == null) return null;
            _projectileCache.TryGetValue(id, out var prefab);
            return prefab;
        }

        // ═══════════════════════════════════
        //  유닛 부착 VFX (Guardian 베리어, Striker CC면역/방어 등)
        // ═══════════════════════════════════

        [Serializable]
        public struct UnitVfxEntry
        {
            public CombatVfxType Type;

            [Header("OneShot (1회 재생)")]
            public AssetReferenceGameObject OneShotVfx;
            public SkillPosition OneShotPosition;
            public bool OneShotFollowable;

            [Header("Loop (지속 부착)")]
            public AssetReferenceGameObject LoopVfx;
            public SkillPosition LoopPosition;
            public bool LoopFollowable;
        }

        [Header("── 유닛 부착 VFX (StatusEffectAdded/Removed 이벤트 연동) ──")]
        [SerializeField] private UnitVfxEntry[] _unitVfxEntries;

        private Dictionary<CombatVfxType, UnitVfxEntry> _unitVfxCache;

        public bool TryGetUnitVfx(CombatVfxType type, out UnitVfxEntry entry)
        {
            if (_unitVfxCache == null) BuildUnitVfxCache();
            return _unitVfxCache.TryGetValue(type, out entry);
        }

        private void BuildUnitVfxCache()
        {
            _unitVfxCache = new Dictionary<CombatVfxType, UnitVfxEntry>();
            if (_unitVfxEntries == null) return;
            for (int i = 0; i < _unitVfxEntries.Length; i++)
            {
                var e = _unitVfxEntries[i];
                if (e.Type != CombatVfxType.None)
                    _unitVfxCache[e.Type] = e;
            }
        }

        // ═══════════════════════════════════
        //  위치 기반 VFX (Esper 폭발 등 — 그리드 좌표에 1회 소환)
        // ═══════════════════════════════════

        [Serializable]
        public struct AreaVfxEntry
        {
            public CombatVfxType Type;
            public AssetReferenceGameObject Prefab;
        }

        [Header("── 위치 기반 VFX (그리드 좌표에 1회 소환) ──")]
        [SerializeField] private AreaVfxEntry[] _areaVfxEntries;

        private Dictionary<CombatVfxType, GameObject> _areaVfxCache;

        public GameObject GetAreaVfx(CombatVfxType type)
        {
            if (_areaVfxCache == null) return null;
            _areaVfxCache.TryGetValue(type, out var prefab);
            return prefab;
        }

        // ═══════════════════════════════════
        //  프리로드 / 해제
        // ═══════════════════════════════════

        private readonly List<AsyncOperationHandle<GameObject>> _loadedHandles = new();

        /// <summary>
        /// 모든 AssetReference를 미리 로드하여 동기 접근 가능하게 함.
        /// 전투 시작 전 한 번 호출.
        /// </summary>
        public async UniTask PreloadAsync()
        {
            _projectileCache = new Dictionary<byte, GameObject>();
            _areaVfxCache = new Dictionary<CombatVfxType, GameObject>();

            // 투사체 오버라이드 프리로드
            if (_projectileOverrides != null)
            {
                for (int i = 0; i < _projectileOverrides.Length; i++)
                {
                    var e = _projectileOverrides[i];
                    if (e.Id == 0 || e.Prefab == null || !e.Prefab.RuntimeKeyIsValid()) continue;
                    var handle = e.Prefab.LoadAssetAsync();
                    _loadedHandles.Add(handle);
                    var go = await handle;
                    if (go != null)
                        _projectileCache[e.Id] = go;
                }
            }

            // 위치 기반 VFX 프리로드
            if (_areaVfxEntries != null)
            {
                for (int i = 0; i < _areaVfxEntries.Length; i++)
                {
                    var e = _areaVfxEntries[i];
                    if (e.Type == CombatVfxType.None || e.Prefab == null || !e.Prefab.RuntimeKeyIsValid()) continue;
                    var handle = e.Prefab.LoadAssetAsync();
                    _loadedHandles.Add(handle);
                    var go = await handle;
                    if (go != null)
                        _areaVfxCache[e.Type] = go;
                }
            }
        }

        /// <summary>로드된 AssetReference 핸들을 모두 해제. 전투 종료 시 호출.</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < _loadedHandles.Count; i++)
            {
                if (_loadedHandles[i].IsValid())
                    Addressables.Release(_loadedHandles[i]);
            }
            _loadedHandles.Clear();
            _projectileCache = null;
            _areaVfxCache = null;
        }

        // ═══════════════════════════════════

        private void OnEnable()
        {
            _projectileCache = null;
            _unitVfxCache = null;
            _areaVfxCache = null;
        }

#if UNITY_EDITOR
        // 레거시 VFX 매핑 (Generate All Entries용)
        private static readonly (CombatVfxType type, string oneShot, string loop,
            SkillPosition oneShotPos, bool oneShotFollow, SkillPosition loopPos, bool loopFollow)[] KnownUnitVfx =
        {
            // GUARDIAN: 베리어 부여 — OneShot, SKILL_ROOT, Follow
            (CombatVfxType.BasicAttackShield, "fx_common_job_guardian_01", null,
                SkillPosition.SKILL_ROOT, true, default, false),
            // STRIKER: CC 면역 부여 — OneShot, SKILL_MIDDLE, Follow
            (CombatVfxType.JobStriker, "fx_common_job_striker_01", null,
                SkillPosition.SKILL_MIDDLE, true, default, false),
            // STRIKER: CC 방어 — OneShot, SKILL_MIDDLE, No Follow
            (CombatVfxType.JobStrikerBlock, "fx_common_job_striker_02", null,
                SkillPosition.SKILL_MIDDLE, false, default, false),
            // GHOST: 백라인 점프 출발 — OneShot, SKILL_BOTTOM, No Follow
            (CombatVfxType.JobGhostJumpStart, "fx_common_assassin_awful", null,
                SkillPosition.SKILL_BOTTOM, false, default, false),
            // GHOST: 백라인 점프 도착 — OneShot, SKILL_BOTTOM, No Follow
            (CombatVfxType.JobGhostJumpEnd, "fx_common_summon_awful", null,
                SkillPosition.SKILL_BOTTOM, false, default, false),
        };

        private static readonly (CombatVfxType type, string prefabName)[] KnownAreaVfx =
        {
            (CombatVfxType.JobEsper, "fx_common_job_espar_01"),
        };

        private static readonly (byte id, string label, string prefabName)[] KnownProjectileOverrides =
        {
            (ProjectileVfxId.SharpshooterAD, "Sharpshooter AD", "fx_common_job_sharpshooter_01"),
            (ProjectileVfxId.SharpshooterAP, "Sharpshooter AP", "fx_common_job_sharpshooter_02"),
        };

        [ContextMenu("Generate All Entries")]
        private void GenerateAllEntries()
        {
            // 유닛 부착 VFX
            var unitEntries = new List<UnitVfxEntry>();
            for (int i = 0; i < KnownUnitVfx.Length; i++)
            {
                var k = KnownUnitVfx[i];
                var entry = new UnitVfxEntry
                {
                    Type = k.type,
                    OneShotPosition = k.oneShotPos,
                    OneShotFollowable = k.oneShotFollow,
                    LoopPosition = k.loopPos,
                    LoopFollowable = k.loopFollow,
                };
                if (!string.IsNullOrEmpty(k.oneShot))
                    entry.OneShotVfx = FindPrefabAssetRef(k.oneShot);
                if (!string.IsNullOrEmpty(k.loop))
                    entry.LoopVfx = FindPrefabAssetRef(k.loop);
                unitEntries.Add(entry);
            }
            _unitVfxEntries = unitEntries.ToArray();

            // 투사체 오버라이드
            var projEntries = new List<ProjectileOverrideEntry>();
            for (int i = 0; i < KnownProjectileOverrides.Length; i++)
            {
                var k = KnownProjectileOverrides[i];
                projEntries.Add(new ProjectileOverrideEntry
                {
                    Id = k.id,
                    Label = k.label,
                    Prefab = FindPrefabAssetRef(k.prefabName),
                });
            }
            _projectileOverrides = projEntries.ToArray();

            // 위치 기반 VFX
            var areaEntries = new List<AreaVfxEntry>();
            for (int i = 0; i < KnownAreaVfx.Length; i++)
            {
                var k = KnownAreaVfx[i];
                areaEntries.Add(new AreaVfxEntry
                {
                    Type = k.type,
                    Prefab = FindPrefabAssetRef(k.prefabName),
                });
            }
            _areaVfxEntries = areaEntries.ToArray();

            _projectileCache = null;
            _unitVfxCache = null;
            _areaVfxCache = null;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[JobPassiveVfxConfig] 유닛VFX {unitEntries.Count}개 / 투사체오버라이드 {projEntries.Count}개 / 위치VFX {areaEntries.Count}개 생성");
        }

        private static AssetReferenceGameObject FindPrefabAssetRef(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            if (guids.Length == 0)
                guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:GameObject");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
                    return new AssetReferenceGameObject(guids[i]);
            }
            Debug.LogWarning($"[JobPassiveVfxConfig] AssetRef 프리팹 '{prefabName}' 를 찾을 수 없음");
            return null;
        }

#endif
    }
}
