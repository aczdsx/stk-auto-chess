using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 전투 VFX 통합 설정. CombatVfxType별 VFX AssetReference 매핑.
    /// 효과 적용 시 유닛에 부착, 해제 시 제거.
    /// StatBuff/StatDebuff는 StatModType으로 세분화하여 (CombatVfxType, StatModType) 복합키로 관리.
    /// _savedMappings: enum name(string) 기준 VFX 참조 저장소 — enum 값이 밀려도 안전.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatVfxConfig", menuName = "AutoChess/Combat Vfx Config")]
    public class CombatVfxConfigSO : ScriptableObject
    {
        [Serializable]
        public struct VfxEntry
        {
            public CombatVfxType Type;
            public StatModType StatType;

            [Header("OneShot")]
            public AssetReferenceGameObject OneShotVfx;
            public SkillPosition OneShotPosition;
            public bool OneShotFollowable;

            [Header("Loop")]
            public AssetReferenceGameObject LoopVfx;
            public SkillPosition LoopPosition;
            public bool LoopFollowable;
        }

        /// <summary>enum name 기준 VFX 참조 저장 레코드 (enum 값 변경에 안전, 프리팹명 저장)</summary>
        [Serializable]
        public struct VfxMappingRecord
        {
            public string TypeName;
            public string StatTypeName;
            public string OneShotPrefabName;
            public SkillPosition OneShotPosition;
            public bool OneShotFollowable;
            public string LoopPrefabName;
            public SkillPosition LoopPosition;
            public bool LoopFollowable;
        }

        [Header("── 버프 (Shield, StatBuff, 지속힐, 면역류) ──")]
        [SerializeField] private VfxEntry[] _buffEntries;

        [Header("── 디버프 (StatDebuff, 지속데미지) ──")]
        [SerializeField] private VfxEntry[] _debuffEntries;

        [Header("── CC (Stun, Silence, Slow, Freeze …) ──")]
        [SerializeField] private VfxEntry[] _ccEntries;

        [Header("── VFX 매핑 저장소 (enum name 기준, 자동 관리) ──")]
        [SerializeField] private List<VfxMappingRecord> _savedMappings = new();

        private Dictionary<(CombatVfxType, StatModType), VfxEntry> _cache;

        public bool TryGetEntry(CombatVfxType type, StatModType statType, out VfxEntry entry)
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue((type, statType), out entry);
        }

        // 하위호환: statType 없이 호출 시 default 사용
        public bool TryGetEntry(CombatVfxType type, out VfxEntry entry)
        {
            return TryGetEntry(type, default, out entry);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<(CombatVfxType, StatModType), VfxEntry>();
            AddToCache(_buffEntries);
            AddToCache(_debuffEntries);
            AddToCache(_ccEntries);
        }

        private void AddToCache(VfxEntry[] entries)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.Type != CombatVfxType.None)
                    _cache[(e.Type, e.StatType)] = e;
            }
        }

        private void OnEnable()
        {
            _cache = null;
        }

        // ── 카테고리 분류 ──

        public static bool IsBuff(CombatVfxType type)
        {
            switch (type)
            {
                case CombatVfxType.StatBuff:
                case CombatVfxType.ContinuousHeal:
                case CombatVfxType.CCImmunity:
                case CombatVfxType.DOTImmunity:
                case CombatVfxType.DebuffImmunity:
                case CombatVfxType.Shield:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDebuff(CombatVfxType type)
        {
            switch (type)
            {
                case CombatVfxType.StatDebuff:
                case CombatVfxType.ContinuousDamage:
                case CombatVfxType.HealAmountDown:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsCC(CombatVfxType type)
        {
            switch (type)
            {
                case CombatVfxType.CC_Stun:
                case CombatVfxType.CC_Silence:
                case CombatVfxType.CC_Slow:
                case CombatVfxType.CC_Freeze:
                case CombatVfxType.CC_Taunt:
                case CombatVfxType.CC_Airborne:
                case CombatVfxType.CC_KnockBack:
                case CombatVfxType.CC_TargetImpossible:
                    return true;
                default:
                    return false;
            }
        }

#if UNITY_EDITOR
        /// <summary>현재 엔트리의 VFX 참조를 enum name 기준으로 저장</summary>
        [ContextMenu("1. Save Mappings")]
        private void SaveMappings()
        {
            var dict = new Dictionary<string, VfxMappingRecord>();
            CollectMappings(dict, _buffEntries);
            CollectMappings(dict, _debuffEntries);
            CollectMappings(dict, _ccEntries);

            _savedMappings = new List<VfxMappingRecord>(dict.Values);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CombatVfxConfig] {_savedMappings.Count}개 매핑 저장 완료");
        }

        /// <summary>기존 엔트리의 구 byte 값을 새 카테고리 Type + StatType으로 인플레이스 변환</summary>
        [ContextMenu("0. Migrate Entries In-Place")]
        private void MigrateEntriesInPlace()
        {
            // 구 byte 값 → (새 CombatVfxType, StatModType) 매핑
            // 삭제된 enum 값들은 Inspector에서 (CombatVfxType)2 등으로 보임
            var byteMap = new Dictionary<byte, (CombatVfxType type, StatModType stat)>
            {
                { 1,  (CombatVfxType.StatBuff,   StatModType.Attack) },
                { 2,  (CombatVfxType.StatBuff,   StatModType.Def) },
                { 3,  (CombatVfxType.StatBuff,   StatModType.ApReduce) },
                { 4,  (CombatVfxType.StatBuff,   StatModType.AttackSpeed) },
                { 32, (CombatVfxType.StatBuff,   StatModType.DodgeChance) },
                { 9,  (CombatVfxType.StatDebuff, StatModType.Attack) },
                { 10, (CombatVfxType.StatDebuff, StatModType.Def) },
                { 11, (CombatVfxType.StatDebuff, StatModType.ApReduce) },
                { 12, (CombatVfxType.StatDebuff, StatModType.AttackSpeed) },
            };

            int migrated = 0;
            migrated += MigrateArray(ref _buffEntries, byteMap);
            migrated += MigrateArray(ref _debuffEntries, byteMap);
            migrated += MigrateArray(ref _ccEntries, byteMap);

            _cache = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CombatVfxConfig] 인플레이스 마이그레이션 완료: {migrated}개 변환");
        }

        private static int MigrateArray(ref VfxEntry[] entries, Dictionary<byte, (CombatVfxType type, StatModType stat)> byteMap)
        {
            if (entries == null) return 0;
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                byte raw = (byte)entries[i].Type;
                if (byteMap.TryGetValue(raw, out var mapped))
                {
                    var e = entries[i];
                    Debug.Log($"[CombatVfxConfig]   {raw} → {mapped.type}/{mapped.stat}");
                    e.Type = mapped.type;
                    e.StatType = mapped.stat;
                    entries[i] = e;
                    count++;
                }
            }
            return count;
        }

        /// <summary>저장된 매핑 기준으로 enum 재생성 + VFX 참조 복원</summary>
        [ContextMenu("2. Generate All Entries (from Saved Mappings)")]
        private void GenerateAllEntries()
        {
            // 저장된 매핑을 (typeName, statTypeName) → record 딕셔너리로 변환
            var saved = new Dictionary<(string, string), VfxMappingRecord>();
            if (_savedMappings != null)
            {
                for (int i = 0; i < _savedMappings.Count; i++)
                {
                    var r = _savedMappings[i];
                    if (!string.IsNullOrEmpty(r.TypeName))
                        saved[(r.TypeName, r.StatTypeName ?? "")] = r;
                }
            }

            var types = (CombatVfxType[])Enum.GetValues(typeof(CombatVfxType));
            var buffs = new List<VfxEntry>();
            var debuffs = new List<VfxEntry>();
            var ccs = new List<VfxEntry>();

            int restored = 0;
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == CombatVfxType.None) continue;

                // StatBuff/StatDebuff는 StatModType 전체를 순회하며 엔트리 복수 생성
                if (t == CombatVfxType.StatBuff || t == CombatVfxType.StatDebuff)
                {
                    var statTypes = (StatModType[])Enum.GetValues(typeof(StatModType));
                    for (int s = 0; s < statTypes.Length; s++)
                    {
                        var st = statTypes[s];
                        var entry = CreateDefaultEntry(t, st);
                        if (saved.TryGetValue((t.ToString(), st.ToString()), out var record))
                        {
                            RestoreEntry(ref entry, record);
                            restored++;
                        }

                        if (IsBuff(t)) buffs.Add(entry);
                        else debuffs.Add(entry);
                    }
                    continue;
                }

                // 비-스탯 타입: StatModType=default
                {
                    var entry = CreateDefaultEntry(t, default);
                    if (saved.TryGetValue((t.ToString(), ""), out var record))
                    {
                        RestoreEntry(ref entry, record);
                        restored++;
                    }

                    if (IsBuff(t)) buffs.Add(entry);
                    else if (IsDebuff(t)) debuffs.Add(entry);
                    else if (IsCC(t)) ccs.Add(entry);
                }
            }

            _buffEntries = buffs.ToArray();
            _debuffEntries = debuffs.ToArray();
            _ccEntries = ccs.ToArray();
            _cache = null;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CombatVfxConfig] 버프 {buffs.Count} / 디버프 {debuffs.Count} / CC {ccs.Count} 엔트리 생성 (매핑 복원: {restored}개)");
        }

        private static VfxEntry CreateDefaultEntry(CombatVfxType type, StatModType statType)
        {
            return new VfxEntry
            {
                Type = type,
                StatType = statType,
                OneShotPosition = SkillPosition.SKILL_TOP,
                OneShotFollowable = true,
                LoopPosition = SkillPosition.SKILL_TOP,
                LoopFollowable = true,
            };
        }

        private static void RestoreEntry(ref VfxEntry entry, VfxMappingRecord record)
        {
            entry.OneShotVfx = FindPrefabAssetRef(record.OneShotPrefabName);
            entry.OneShotPosition = record.OneShotPosition;
            entry.OneShotFollowable = record.OneShotFollowable;
            entry.LoopVfx = FindPrefabAssetRef(record.LoopPrefabName);
            entry.LoopPosition = record.LoopPosition;
            entry.LoopFollowable = record.LoopFollowable;
        }

        private static string GetPrefabName(AssetReferenceGameObject assetRef)
        {
            if (assetRef == null) return null;
            string guid = assetRef.AssetGUID;
            if (string.IsNullOrEmpty(guid)) return null;
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return System.IO.Path.GetFileNameWithoutExtension(path);
        }

        private static AssetReferenceGameObject FindPrefabAssetRef(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:GameObject");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
                    return new AssetReferenceGameObject(guids[i]);
            }
            Debug.LogWarning($"[CombatVfxConfig] 프리팹 '{prefabName}' 를 찾을 수 없음");
            return null;
        }

        private static void CollectMappings(Dictionary<string, VfxMappingRecord> dict, VfxEntry[] entries)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.Type == CombatVfxType.None) continue;

                string oneShotName = GetPrefabName(e.OneShotVfx);
                string loopName = GetPrefabName(e.LoopVfx);
                if (oneShotName == null && loopName == null) continue;

                // StatBuff/StatDebuff는 StatType으로 구분된 키 사용
                string statTypeName = (e.Type == CombatVfxType.StatBuff || e.Type == CombatVfxType.StatDebuff)
                    ? e.StatType.ToString()
                    : "";
                string dictKey = string.IsNullOrEmpty(statTypeName)
                    ? e.Type.ToString()
                    : $"{e.Type}_{statTypeName}";

                dict[dictKey] = new VfxMappingRecord
                {
                    TypeName = e.Type.ToString(),
                    StatTypeName = statTypeName,
                    OneShotPrefabName = oneShotName,
                    OneShotPosition = e.OneShotPosition,
                    OneShotFollowable = e.OneShotFollowable,
                    LoopPrefabName = loopName,
                    LoopPosition = e.LoopPosition,
                    LoopFollowable = e.LoopFollowable,
                };
            }
        }
#endif
    }
}
