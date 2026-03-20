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
            public Sprite IconSprite;
        }

        [Serializable]
        public struct SkillMarkerIconEntry
        {
            public SkillMarkerType MarkerType;
            public Sprite IconSprite;
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

        private Dictionary<(CombatVfxType, StatModType), EffectIconEntry> _effectCache;
        private Dictionary<int, SkillMarkerIconEntry> _markerCache;

        public bool TryGetEffectIcon(CombatVfxType type, StatModType statType, out EffectIconEntry entry)
        {
            BuildCacheIfNeeded();
            return _effectCache.TryGetValue((type, statType), out entry);
        }

        // 하위호환: statType 없이 호출 시 default 사용
        public bool TryGetEffectIcon(CombatVfxType type, out EffectIconEntry entry)
        {
            return TryGetEffectIcon(type, default, out entry);
        }

        public bool TryGetMarkerIcon(int markerValue, out SkillMarkerIconEntry entry)
        {
            BuildCacheIfNeeded();
            return _markerCache.TryGetValue(markerValue, out entry);
        }

        private void BuildCacheIfNeeded()
        {
            if (_effectCache != null) return;

            _effectCache = new Dictionary<(CombatVfxType, StatModType), EffectIconEntry>();
            AddToCache(_buffIcons);
            AddToCache(_debuffIcons);
            AddToCache(_ccIcons);

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

        private void AddToCache(EffectIconEntry[] entries)
        {
            if (entries == null) return;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.EffectType != CombatVfxType.None)
                    _effectCache[(e.EffectType, e.StatType)] = e;
            }
        }

        private void OnEnable()
        {
            _effectCache = null;
            _markerCache = null;
        }

        private void OnValidate()
        {
            _effectCache = null;
            _markerCache = null;

            // EffectIcons 중복 체크 (전체 카테고리)
            var seen = new HashSet<(CombatVfxType, StatModType)>();
            ValidateArray(_buffIcons, "Buff", seen);
            ValidateArray(_debuffIcons, "Debuff", seen);
            ValidateArray(_ccIcons, "CC", seen);

            // SkillMarkerIcons 중복 체크
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

        // (CombatVfxType, StatModType) → 스프라이트 suffix 매핑 (폴더의 실제 파일명 기준)
        // Confluence D+0 기획문서 기준 전체 매핑
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

        private static Sprite FindSpriteInFolder(string suffix)
        {
            string targetName = IconPrefix + suffix;
            var guids = UnityEditor.AssetDatabase.FindAssets(targetName + " t:Sprite", new[] { IconFolder });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.Equals(fileName, targetName, System.StringComparison.OrdinalIgnoreCase))
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return null;
        }

        /// <summary>Icon_Buffs 폴더를 스캔하여 카테고리별 배열에 Sprite 직접 매핑</summary>
        [ContextMenu("Auto-Map from Icon Folder")]
        private void AutoMapFromFolder()
        {
            // 1) 폴더 내 존재하는 스프라이트 suffix → Sprite 수집
            var spriteMap = new Dictionary<string, Sprite>();
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { IconFolder });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith(IconPrefix))
                {
                    var suffix = fileName.Substring(IconPrefix.Length);
                    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sprite != null)
                        spriteMap[suffix] = sprite;
                }
            }

            // 2) KnownEffectMap 기준으로 카테고리별 리스트 생성
            var buffList = new List<EffectIconEntry>();
            var debuffList = new List<EffectIconEntry>();
            var ccList = new List<EffectIconEntry>();
            var usedSuffixes = new HashSet<string>();

            foreach (var kv in KnownEffectMap)
            {
                Sprite sprite = null;
                if (!spriteMap.TryGetValue(kv.Value, out sprite))
                {
                    Debug.LogWarning($"[BuffIconConfig] 스프라이트 없음: {IconPrefix}{kv.Value} (CombatVfxType.{kv.Key.Item1}/{kv.Key.Item2})");
                }

                var entry = new EffectIconEntry
                {
                    EffectType = kv.Key.Item1,
                    StatType = kv.Key.Item2,
                    IconSprite = sprite,
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

            // 3) 매핑되지 않은 스프라이트 로그 출력
            int unmapped = 0;
            foreach (var suffix in spriteMap.Keys)
            {
                if (!usedSuffixes.Contains(suffix))
                {
                    Debug.Log($"[BuffIconConfig] 미매핑 스프라이트: {IconPrefix}{suffix}");
                    unmapped++;
                }
            }

            // 4) SkillMarker 자동 탐색: BUFF_SPECIAL_*, DEBUFF_SPECIAL_*, CC_MISA_* 패턴
            var markerList = new List<SkillMarkerIconEntry>();
            if (_skillMarkerIcons != null)
            {
                // 기존 항목 유지 (MarkerType이 이미 설정된 것들)
                foreach (var existing in _skillMarkerIcons)
                {
                    if (existing.MarkerType != SkillMarkerType.None)
                        markerList.Add(existing);
                }
            }

            foreach (var kv in spriteMap)
            {
                bool isMarkerCandidate = kv.Key.StartsWith("BUFF_SPECIAL_") ||
                                         kv.Key.StartsWith("DEBUFF_SPECIAL_") ||
                                         kv.Key.StartsWith("CC_MISA_");
                if (!isMarkerCandidate || usedSuffixes.Contains(kv.Key)) continue;

                // 기존에 같은 Sprite로 등록된 항목이 있으면 스킵
                bool alreadyRegistered = false;
                foreach (var m in markerList)
                {
                    if (m.IconSprite == kv.Value) { alreadyRegistered = true; break; }
                }
                if (alreadyRegistered) continue;

                markerList.Add(new SkillMarkerIconEntry
                {
                    MarkerType = SkillMarkerType.None, // 인스펙터에서 드롭다운 선택
                    IconSprite = kv.Value,
                    ReplacesEffect = CombatVfxType.None,
                    ReplacesStatType = default,
                });
                Debug.Log($"[BuffIconConfig] SkillMarker 후보 추가: {IconPrefix}{kv.Key} (MarkerType 미설정)");
            }

            _skillMarkerIcons = markerList.ToArray();

            _effectCache = null;
            _markerCache = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[BuffIconConfig] 자동 매핑 완료: 버프 {buffList.Count} / 디버프 {debuffList.Count} / CC {ccList.Count} / SkillMarker {markerList.Count}개, 미매핑 {unmapped}개");
        }
#endif
    }
}
