using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    [CreateAssetMenu(fileName = "BuffIconConfig", menuName = "AutoChess/Buff Icon Config")]
    public class BuffIconConfigSO : ScriptableObject
    {
        [Header("버프 아이콘 프리팹")]
        public AssetReferenceGameObject BuffIconPrefab;

        [Serializable]
        public struct EffectIconEntry
        {
            public CombatVfxType EffectType;
            public StatModType StatType;
            public string SpriteName;
        }

        [Serializable]
        public struct SkillMarkerIconEntry
        {
            public SkillMarkerType MarkerType;
            public string SpriteName;
            [Tooltip("이 마커가 활성일 때 숨길 범용 이펙트 아이콘 (None이면 대체 안 함)")]
            public CombatVfxType ReplacesEffect;
            public StatModType ReplacesStatType;
        }

        [Header("── 버프 (Shield, StatBuff, 지속힐, 면역류) ──")]
        [SerializeField] private EffectIconEntry[] _buffIcons;

        [Header("── 디버프 (StatDebuff, 지속데미지, 회복감소) ──")]
        [SerializeField] private EffectIconEntry[] _debuffIcons;

        [Header("── CC (Stun, Silence, Slow, Freeze …) ──")]
        [SerializeField] private EffectIconEntry[] _ccIcons;

        [Header("스킬마커 아이콘 (루키다 도깨비불, 테토라 분노 등)")]
        [SerializeField] private SkillMarkerIconEntry[] _skillMarkerIcons;

        // ── 마커 메타데이터 캐시 (ReplacesEffect 조회용) ──
        private Dictionary<int, SkillMarkerIconEntry> _markerCache;

        // ── 공개 API ──

        // 이름 캐시 (직렬화 데이터에서 직접 조회, Preload 불필요)
        private Dictionary<(CombatVfxType, StatModType), string> _effectNameCache;
        private Dictionary<int, string> _markerNameCache;

        private void BuildNameCaches()
        {
            if (_effectNameCache != null) return;
            _effectNameCache = new Dictionary<(CombatVfxType, StatModType), string>();
            BuildEffectNameCache(_buffIcons);
            BuildEffectNameCache(_debuffIcons);
            BuildEffectNameCache(_ccIcons);

            _markerNameCache = new Dictionary<int, string>();
            if (_skillMarkerIcons != null)
            {
                for (int i = 0; i < _skillMarkerIcons.Length; i++)
                {
                    var e = _skillMarkerIcons[i];
                    if (e.MarkerType != SkillMarkerType.None && !string.IsNullOrEmpty(e.SpriteName))
                        _markerNameCache[(int)e.MarkerType] = e.SpriteName;
                }
            }
        }

        private void BuildEffectNameCache(EffectIconEntry[] entries)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.EffectType != CombatVfxType.None && !string.IsNullOrEmpty(e.SpriteName))
                    _effectNameCache[(e.EffectType, e.StatType)] = e.SpriteName;
            }
        }

        public bool TryGetEffectSpriteName(CombatVfxType type, StatModType statType, out string spriteName)
        {
            BuildNameCaches();
            return _effectNameCache.TryGetValue((type, statType), out spriteName);
        }

        public bool TryGetEffectSpriteName(CombatVfxType type, out string spriteName)
        {
            return TryGetEffectSpriteName(type, default, out spriteName);
        }

        public bool TryGetMarkerSpriteName(int markerValue, out string spriteName)
        {
            BuildNameCaches();
            return _markerNameCache.TryGetValue(markerValue, out spriteName);
        }

        public bool TryGetMarkerIcon(int markerValue, out SkillMarkerIconEntry entry)
        {
            BuildMarkerCache();
            return _markerCache.TryGetValue(markerValue, out entry);
        }

        private void BuildMarkerCache()
        {
            if (_markerCache != null) return;
            _markerCache = new Dictionary<int, SkillMarkerIconEntry>();
            if (_skillMarkerIcons != null)
            {
                for (int i = 0; i < _skillMarkerIcons.Length; i++)
                {
                    var e = _skillMarkerIcons[i];
                    if (e.MarkerType != SkillMarkerType.None)
                        _markerCache[(int)e.MarkerType] = e;
                }
            }
        }

        private void OnEnable()
        {
            _markerCache = null;
            _effectNameCache = null;
            _markerNameCache = null;
        }

        private void OnValidate()
        {
            _markerCache = null;
            _effectNameCache = null;
            _markerNameCache = null;

            var seen = new HashSet<(CombatVfxType, StatModType)>();
            ValidateArray(_buffIcons, "Buff", seen);
            ValidateArray(_debuffIcons, "Debuff", seen);
            ValidateArray(_ccIcons, "CC", seen);

            if (_skillMarkerIcons != null)
            {
                var markerSeen = new HashSet<SkillMarkerType>();
                for (int i = 0; i < _skillMarkerIcons.Length; i++)
                {
                    var t = _skillMarkerIcons[i].MarkerType;
                    if (t != SkillMarkerType.None && !markerSeen.Add(t))
                        Debug.LogError($"[BuffIconConfig] SkillMarkerIcons 중복: {t} (index {i})");
                }
            }
        }

        private static void ValidateArray(EffectIconEntry[] entries, string category, HashSet<(CombatVfxType, StatModType)> seen)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.EffectType != CombatVfxType.None && !seen.Add((e.EffectType, e.StatType)))
                    Debug.LogError($"[BuffIconConfig] {category} 중복: {e.EffectType}/{e.StatType} (index {i})");
            }
        }

#if UNITY_EDITOR
        private const string IconFolder = "Assets/_Project/Addressables/Remote/Textures/Icon/Icon_Buffs";
        private const string IconPrefix = "BuffDebuffIcon_";

        private static readonly Dictionary<(CombatVfxType, StatModType), string> KnownEffectMap = new()
        {
            // ── 버프 ──
            { (CombatVfxType.StatBuff, StatModType.Attack),       "BUFF_ATK_UP" },
            { (CombatVfxType.StatBuff, StatModType.Def),          "BUFF_DEF_UP" },
            { (CombatVfxType.StatBuff, StatModType.AttackSpeed),  "BUFF_ATK_SPEED_UP" },
            { (CombatVfxType.StatBuff, StatModType.AdReduce),     "BUFF_REDUCE_UP" },
            { (CombatVfxType.StatBuff, StatModType.ApReduce),     "BUFF_REDUCE_UP" },
            { (CombatVfxType.StatBuff, StatModType.AtkPierce),    "BUFF_PIERCE_UP" },
            { (CombatVfxType.StatBuff, StatModType.ResPierce),    "BUFF_PIERCE_UP" },
            { (CombatVfxType.StatBuff, StatModType.HealPower),    "BUFF_HP_RECOVERY_UP" },
            { (CombatVfxType.StatBuff, StatModType.CritRate),     "BUFF_CRIT_UP" },
            { (CombatVfxType.StatBuff, StatModType.CritPower),    "BUFF_CRI_POWER_UP" },
            { (CombatVfxType.StatBuff, StatModType.DodgeChance),  "BUFF_AVOID_UP" },
            { (CombatVfxType.StatBuff, StatModType.LifeSteal),    "BUFF_LIFE_STEAL_UP" },
            { (CombatVfxType.StatBuff, StatModType.MaxHP),        "BUFF_MAX_HP_UP" },
            { (CombatVfxType.StatBuff, StatModType.MoveSpeed),    "BUFF_MOVE_SPEED_UP" },
            { (CombatVfxType.ContinuousHeal, default),            "BUFF_HOT_UP" },
            { (CombatVfxType.Shield, default),                    "SHIELD" },
            { (CombatVfxType.BasicAttackShield, default),          "BUFF_NORMAL_ATTACK_SHIELD" },
            { (CombatVfxType.CCImmunity, default),                "BUFF_IMMUNE" },
            { (CombatVfxType.DOTImmunity, default),               "BUFF_IMMUNE" },
            { (CombatVfxType.DebuffImmunity, default),            "BUFF_IMMUNE" },
            // ── 디버프 ──
            { (CombatVfxType.StatDebuff, StatModType.Attack),     "DEBUFF_ATK_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.Def),        "DEBUFF_DEF_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.AttackSpeed),"DEBUFF_ATK_SPEED_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.AdReduce),   "DEBUFF_REDUCE_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.ApReduce),   "DEBUFF_REDUCE_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.AtkPierce),  "DEBUFF_PIERCE_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.ResPierce),  "DEBUFF_PIERCE_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.CritRate),   "DEBUFF_CRIT_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.CritPower),  "DEBUFF_CRI_POWER_DOWN" },
            { (CombatVfxType.StatDebuff, StatModType.HitChance),  "DEBUFF_HIT_DOWN" },
            { (CombatVfxType.ContinuousDamage, default),          "DEBUFF_POISON" },
            { (CombatVfxType.HealAmountDown, default),            "DEBUFF_HP_RECOVERY_DOWN" },
            // ── CC ──
            { (CombatVfxType.CC_Stun, default),       "CC_STUN" },
            { (CombatVfxType.CC_Silence, default),    "CC_SLIENCE" },
            { (CombatVfxType.CC_Slow, default),       "DEBUFF_MOVE_DOWN" },
            { (CombatVfxType.CC_Freeze, default),     "CC_FREEZ" },
            { (CombatVfxType.CC_Taunt, default),      "CC_TARGET_IMPOSSIBLE" },
            { (CombatVfxType.CC_Airborne, default),   "CC_AIRBORNE" },
            { (CombatVfxType.CC_KnockBack, default),  "CC_KNOCKBACK" },
            { (CombatVfxType.CC_TargetImpossible, default), "CC_TARGET_IMPOSSIBLE" },
        };

        [ContextMenu("Auto-Map from Icon Folder")]
        private void AutoMapFromFolder()
        {
            var existingSuffixes = new HashSet<string>();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { IconFolder });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith(IconPrefix))
                    existingSuffixes.Add(fileName.Substring(IconPrefix.Length));
            }

            var buffList = new List<EffectIconEntry>();
            var debuffList = new List<EffectIconEntry>();
            var ccList = new List<EffectIconEntry>();
            var usedSuffixes = new HashSet<string>();

            foreach (var kv in KnownEffectMap)
            {
                string spriteName = IconPrefix + kv.Value;
                if (!existingSuffixes.Contains(kv.Value))
                    Debug.LogWarning($"[BuffIconConfig] 스프라이트 없음: {spriteName} (CombatVfxType.{kv.Key.Item1}/{kv.Key.Item2})");

                var entry = new EffectIconEntry
                {
                    EffectType = kv.Key.Item1,
                    StatType = kv.Key.Item2,
                    SpriteName = spriteName,
                };

                if (CombatVfxConfigSO.IsBuff(kv.Key.Item1))
                    buffList.Add(entry);
                else if (CombatVfxConfigSO.IsDebuff(kv.Key.Item1))
                    debuffList.Add(entry);
                else if (CombatVfxConfigSO.IsCC(kv.Key.Item1))
                    ccList.Add(entry);

                usedSuffixes.Add(kv.Value);
            }

            _buffIcons = buffList.ToArray();
            _debuffIcons = debuffList.ToArray();
            _ccIcons = ccList.ToArray();

            int unmapped = 0;
            foreach (var suffix in existingSuffixes)
            {
                if (!usedSuffixes.Contains(suffix))
                {
                    Debug.Log($"[BuffIconConfig] 미매핑 스프라이트: {IconPrefix}{suffix}");
                    unmapped++;
                }
            }

            var markerList = new List<SkillMarkerIconEntry>();
            if (_skillMarkerIcons != null)
            {
                foreach (var existing in _skillMarkerIcons)
                {
                    if (existing.MarkerType != SkillMarkerType.None)
                        markerList.Add(existing);
                }
            }

            foreach (var suffix in existingSuffixes)
            {
                bool isMarkerCandidate = suffix.StartsWith("BUFF_SPECIAL_") ||
                                         suffix.StartsWith("DEBUFF_SPECIAL_") ||
                                         suffix.StartsWith("CC_MISA_");
                if (!isMarkerCandidate || usedSuffixes.Contains(suffix)) continue;

                string spriteName = IconPrefix + suffix;
                bool alreadyRegistered = false;
                foreach (var m in markerList)
                {
                    if (m.SpriteName == spriteName) { alreadyRegistered = true; break; }
                }
                if (alreadyRegistered) continue;

                markerList.Add(new SkillMarkerIconEntry
                {
                    MarkerType = SkillMarkerType.None,
                    SpriteName = spriteName,
                    ReplacesEffect = CombatVfxType.None,
                    ReplacesStatType = default,
                });
                Debug.Log($"[BuffIconConfig] SkillMarker 후보 추가: {spriteName} (MarkerType 미설정)");
            }

            _skillMarkerIcons = markerList.ToArray();

            _markerCache = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[BuffIconConfig] 자동 매핑 완료: 버프 {buffList.Count} / 디버프 {debuffList.Count} / CC {ccList.Count} / SkillMarker {markerList.Count}개, 미매핑 {unmapped}개");
        }
#endif
    }
}
