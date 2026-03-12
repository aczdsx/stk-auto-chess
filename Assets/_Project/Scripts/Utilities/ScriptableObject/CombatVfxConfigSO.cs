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
    /// _savedMappings: enum name(string) 기준 VFX 참조 저장소 — enum 값이 밀려도 안전.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatVfxConfig", menuName = "AutoChess/Combat Vfx Config")]
    public class CombatVfxConfigSO : ScriptableObject
    {
        [Serializable]
        public struct VfxEntry
        {
            public CombatVfxType Type;

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

        private Dictionary<CombatVfxType, VfxEntry> _cache;

        public bool TryGetEntry(CombatVfxType type, out VfxEntry entry)
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue(type, out entry);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<CombatVfxType, VfxEntry>();
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
                    _cache[e.Type] = e;
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
                case CombatVfxType.StatBuff_Attack:
                case CombatVfxType.StatBuff_Armor:
                case CombatVfxType.StatBuff_MagicResist:
                case CombatVfxType.StatBuff_AttackSpeed:
                case CombatVfxType.ContinuousHeal:
                case CombatVfxType.CCImmunity:
                case CombatVfxType.DOTImmunity:
                case CombatVfxType.DebuffImmunity:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDebuff(CombatVfxType type)
        {
            switch (type)
            {
                case CombatVfxType.StatDebuff_Attack:
                case CombatVfxType.StatDebuff_Armor:
                case CombatVfxType.StatDebuff_MagicResist:
                case CombatVfxType.StatDebuff_AttackSpeed:
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

        /// <summary>저장된 매핑 기준으로 enum 재생성 + VFX 참조 복원</summary>
        [ContextMenu("2. Generate All Entries (from Saved Mappings)")]
        private void GenerateAllEntries()
        {
            // 저장된 매핑을 name → record 딕셔너리로 변환
            var saved = new Dictionary<string, VfxMappingRecord>();
            if (_savedMappings != null)
            {
                for (int i = 0; i < _savedMappings.Count; i++)
                {
                    var r = _savedMappings[i];
                    if (!string.IsNullOrEmpty(r.TypeName))
                        saved[r.TypeName] = r;
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

                var entry = new VfxEntry
                {
                    Type = t,
                    OneShotPosition = SkillPosition.SKILL_TOP,
                    OneShotFollowable = true,
                    LoopPosition = SkillPosition.SKILL_TOP,
                    LoopFollowable = true,
                };

                // 저장된 매핑에서 name 매칭으로 VFX 참조 복원 (프리팹명 → AssetReference)
                if (saved.TryGetValue(t.ToString(), out var record))
                {
                    entry.OneShotVfx = FindPrefabAssetRef(record.OneShotPrefabName);
                    entry.OneShotPosition = record.OneShotPosition;
                    entry.OneShotFollowable = record.OneShotFollowable;
                    entry.LoopVfx = FindPrefabAssetRef(record.LoopPrefabName);
                    entry.LoopPosition = record.LoopPosition;
                    entry.LoopFollowable = record.LoopFollowable;
                    restored++;
                }

                if (IsBuff(t)) buffs.Add(entry);
                else if (IsDebuff(t)) debuffs.Add(entry);
                else if (IsCC(t)) ccs.Add(entry);
            }

            _buffEntries = buffs.ToArray();
            _debuffEntries = debuffs.ToArray();
            _ccEntries = ccs.ToArray();
            _cache = null;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CombatVfxConfig] 버프 {buffs.Count} / 디버프 {debuffs.Count} / CC {ccs.Count} 엔트리 생성 (매핑 복원: {restored}개)");
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

                dict[e.Type.ToString()] = new VfxMappingRecord
                {
                    TypeName = e.Type.ToString(),
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
