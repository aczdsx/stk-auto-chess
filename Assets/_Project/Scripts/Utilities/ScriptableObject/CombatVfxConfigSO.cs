using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 전투 VFX 통합 설정. CombatVfxType별 VFX AssetReference 매핑.
    /// 효과 적용 시 유닛에 부착, 해제 시 제거.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatVfxConfig", menuName = "AutoChess/Combat Vfx Config")]
    public class CombatVfxConfigSO : ScriptableObject
    {
        [Serializable]
        public struct VfxEntry
        {
            public CombatVfxType Type;
            public AssetReferenceGameObject OneShotVfx;  // 적용 순간 1회 재생 (null 가능)
            public AssetReferenceGameObject LoopVfx;     // 지속 중 루프 재생 (null 가능)
        }

        [Header("── 버프 (Shield, StatBuff, 지속힐, 면역류) ──")]
        [SerializeField] private VfxEntry[] _buffEntries;

        [Header("── 디버프 (StatDebuff, 지속데미지) ──")]
        [SerializeField] private VfxEntry[] _debuffEntries;

        [Header("── CC (Stun, Silence, Slow, Freeze …) ──")]
        [SerializeField] private VfxEntry[] _ccEntries;

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
        [ContextMenu("Generate All Entries")]
        private void GenerateAllEntries()
        {
            var types = (CombatVfxType[])Enum.GetValues(typeof(CombatVfxType));

            var existing = new Dictionary<CombatVfxType, VfxEntry>();
            CollectExisting(existing, _buffEntries);
            CollectExisting(existing, _debuffEntries);
            CollectExisting(existing, _ccEntries);

            var buffs = new List<VfxEntry>();
            var debuffs = new List<VfxEntry>();
            var ccs = new List<VfxEntry>();

            for (int i = 0; i < types.Length; i++)
            {
                var t = types[i];
                if (t == CombatVfxType.None) continue;

                var entry = existing.TryGetValue(t, out var prev)
                    ? prev
                    : new VfxEntry { Type = t };

                if (IsBuff(t)) buffs.Add(entry);
                else if (IsDebuff(t)) debuffs.Add(entry);
                else if (IsCC(t)) ccs.Add(entry);
            }

            _buffEntries = buffs.ToArray();
            _debuffEntries = debuffs.ToArray();
            _ccEntries = ccs.ToArray();
            _cache = null;

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CombatVfxConfig] 버프 {buffs.Count} / 디버프 {debuffs.Count} / CC {ccs.Count} 엔트리 생성 완료");
        }

        private static void CollectExisting(Dictionary<CombatVfxType, VfxEntry> dict, VfxEntry[] entries)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Type != CombatVfxType.None)
                    dict[entries[i].Type] = entries[i];
            }
        }
#endif
    }
}
