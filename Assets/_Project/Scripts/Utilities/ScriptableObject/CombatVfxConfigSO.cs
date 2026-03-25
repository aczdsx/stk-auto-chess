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

        [Header("── 버프 (Shield, StatBuff, 지속힐, 면역류) ──")]
        [SerializeField] private VfxEntry[] _buffEntries;

        [Header("── 디버프 (StatDebuff, 지속데미지) ──")]
        [SerializeField] private VfxEntry[] _debuffEntries;

        [Header("── CC (Stun, Silence, Slow, Freeze …) ──")]
        [SerializeField] private VfxEntry[] _ccEntries;

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
                case CombatVfxType.BasicAttackShield:
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
        // (CombatVfxType, StatModType) → (OneShotPrefabName, LoopPrefabName) 기본 매핑
        // 전용 VFX가 없는 스탯은 계열이 같은 범용 VFX를 공유
        private static readonly Dictionary<(CombatVfxType, StatModType), (string oneShot, string loop)> KnownVfxMap = new()
        {
            // ── 버프 ──
            { (CombatVfxType.StatBuff, StatModType.Attack),      ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.StatBuff, StatModType.Def),         ("fx_common_buff_dfup",      "fx_common_buff_dfup_01") },
            { (CombatVfxType.StatBuff, StatModType.AttackSpeed), ("fx_common_buff_spdup_01",  "fx_common_buff_spdup_02") },
            { (CombatVfxType.StatBuff, StatModType.AdReduce),    ("fx_common_buff_dfup",      "fx_common_buff_dfup_01") },
            { (CombatVfxType.StatBuff, StatModType.ApReduce),    ("fx_common_buff_dfup",      "fx_common_buff_dfup_01") },
            { (CombatVfxType.StatBuff, StatModType.AtkPierce),   ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.StatBuff, StatModType.ResPierce),   ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.StatBuff, StatModType.HealPower),   ("fx_common_buff_heal",      null) },
            { (CombatVfxType.StatBuff, StatModType.CritRate),    ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.StatBuff, StatModType.CritPower),   ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.StatBuff, StatModType.DodgeChance), ("fx_common_buff_dfup",      "fx_common_buff_dfup_01") },
            { (CombatVfxType.StatBuff, StatModType.LifeSteal),   ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.StatBuff, StatModType.HitChance),   ("fx_common_buff_atkup_01",  "fx_common_buff_atkup_02") },
            { (CombatVfxType.ContinuousHeal, default),           ("Skill_406011",             null) },
            { (CombatVfxType.Shield, default),                   ("fx_common_buff_shield_01", "fx_common_buff_shield_02") },
            { (CombatVfxType.CCImmunity, default),               ("fx_common_buff_immune_02", null) },
            { (CombatVfxType.DOTImmunity, default),              ("fx_common_buff_immune_02", null) },
            { (CombatVfxType.DebuffImmunity, default),           ("fx_common_buff_immune_02", null) },
            // ── 디버프 ──
            { (CombatVfxType.StatDebuff, StatModType.Attack),      ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.StatDebuff, StatModType.Def),         ("fx_common_debuff_dfdown",      "fx_common_debuff_dfdown_01") },
            { (CombatVfxType.StatDebuff, StatModType.AttackSpeed), ("fx_common_debuff_spddown_01",  "fx_common_debuff_spddown_02") },
            { (CombatVfxType.StatDebuff, StatModType.AdReduce),    ("fx_common_debuff_dfdown",      "fx_common_debuff_dfdown_01") },
            { (CombatVfxType.StatDebuff, StatModType.ApReduce),    ("fx_common_debuff_dfdown",      "fx_common_debuff_dfdown_01") },
            { (CombatVfxType.StatDebuff, StatModType.AtkPierce),   ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.StatDebuff, StatModType.ResPierce),   ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.StatDebuff, StatModType.CritRate),    ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.StatDebuff, StatModType.CritPower),   ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.StatDebuff, StatModType.HitChance),   ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.StatDebuff, StatModType.DodgeChance), ("fx_common_debuff_dfdown",      "fx_common_debuff_dfdown_01") },
            { (CombatVfxType.StatDebuff, StatModType.HealPower),   ("fx_common_debuff_healdown",    "fx_common_debuff_healdown_01") },
            { (CombatVfxType.StatDebuff, StatModType.LifeSteal),   ("fx_common_debuff_atkdown_01",  "fx_common_debuff_atkdown_02") },
            { (CombatVfxType.HealAmountDown, default),             ("fx_common_debuff_healdown",    "fx_common_debuff_healdown_01") },
            // ── CC ──
            { (CombatVfxType.CC_Stun, default),              (null, "fx_common_debuff_stun") },
            { (CombatVfxType.CC_Silence, default),           (null, "fx_common_debuff_silence") },
            { (CombatVfxType.CC_Slow, default),              (null, "fx_common_debuff_spddown_02") },
            { (CombatVfxType.CC_Taunt, default),             ("fx_common_debuff_provoke", "fx_common_debuff_provoke_01") },
            { (CombatVfxType.CC_Airborne, default),          (null, "fx_common_commander_skill_03") },
        };

        /// <summary>KnownVfxMap 기준으로 전체 엔트리 재생성 + VFX 자동 매핑</summary>
        [ContextMenu("Generate All Entries")]
        private void GenerateAllEntries()
        {
            var types = (CombatVfxType[])Enum.GetValues(typeof(CombatVfxType));
            var buffs = new List<VfxEntry>();
            var debuffs = new List<VfxEntry>();
            var ccs = new List<VfxEntry>();

            int mapped = 0;
            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == CombatVfxType.None) continue;
                // 직업 패시브는 JobPassiveVfxConfigSO에서 관리
                if (IsJobPassive(t)) continue;

                // StatBuff/StatDebuff는 StatModType 전체를 순회하며 엔트리 복수 생성
                if (t == CombatVfxType.StatBuff || t == CombatVfxType.StatDebuff)
                {
                    var statTypes = (StatModType[])Enum.GetValues(typeof(StatModType));
                    for (int s = 0; s < statTypes.Length; s++)
                    {
                        var st = statTypes[s];
                        if (st == default) continue; // None 스킵
                        var entry = CreateDefaultEntry(t, st);
                        mapped += TryApplyKnownVfx(ref entry, t, st);

                        if (IsBuff(t)) buffs.Add(entry);
                        else debuffs.Add(entry);
                    }
                    continue;
                }

                // 비-스탯 타입: StatModType=default
                {
                    var entry = CreateDefaultEntry(t, default);
                    mapped += TryApplyKnownVfx(ref entry, t, default);

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
            Debug.Log($"[CombatVfxConfig] 버프 {buffs.Count} / 디버프 {debuffs.Count} / CC {ccs.Count} 엔트리 생성 (VFX 매핑: {mapped}개)");
        }

        /// <summary>직업 패시브 VFX인지 판별 (JobPassiveVfxConfigSO에서 관리하므로 여기서 스킵)</summary>
        private static bool IsJobPassive(CombatVfxType type)
        {
            switch (type)
            {
                case CombatVfxType.JobSharpshooter:
                case CombatVfxType.JobGhost:
                case CombatVfxType.BasicAttackShield:
                case CombatVfxType.JobStriker:
                case CombatVfxType.JobStrikerBlock:
                case CombatVfxType.JobEsper:
                case CombatVfxType.JobGhostJumpStart:
                case CombatVfxType.JobGhostJumpEnd:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>KnownVfxMap에서 VFX 프리팹을 찾아 엔트리에 적용</summary>
        private static int TryApplyKnownVfx(ref VfxEntry entry, CombatVfxType type, StatModType statType)
        {
            if (!KnownVfxMap.TryGetValue((type, statType), out var vfxNames))
                return 0;

            if (!string.IsNullOrEmpty(vfxNames.oneShot))
                entry.OneShotVfx = FindPrefabAssetRef(vfxNames.oneShot);
            if (!string.IsNullOrEmpty(vfxNames.loop))
                entry.LoopVfx = FindPrefabAssetRef(vfxNames.loop);

            return (entry.OneShotVfx != null || entry.LoopVfx != null) ? 1 : 0;
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

        private static AssetReferenceGameObject FindPrefabAssetRef(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            // t:Prefab 또는 t:GameObject 둘 다 시도
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:Prefab");
            if (guids.Length == 0)
                guids = UnityEditor.AssetDatabase.FindAssets($"{prefabName} t:GameObject");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == prefabName)
                    return new AssetReferenceGameObject(guids[i]);
            }
            Debug.LogWarning($"[CombatVfxConfig] 프리팹 '{prefabName}' 를 찾을 수 없음");
            return null;
        }
#endif
    }
}
