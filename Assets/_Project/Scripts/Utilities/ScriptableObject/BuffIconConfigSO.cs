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
            public Sprite IconSprite;
        }

        [Serializable]
        public struct SkillMarkerIconEntry
        {
            public SkillMarkerType MarkerType;
            public Sprite IconSprite;
            [Tooltip("이 마커가 활성일 때 숨길 범용 이펙트 아이콘 (None이면 대체 안 함)")]
            public CombatVfxType ReplacesEffect;
        }

        [Header("상태효과/CC 아이콘")]
        [SerializeField] private EffectIconEntry[] _effectIcons;

        [Header("스킬마커 아이콘 (루키다 도깨비불, 테토라 분노 등)")]
        [SerializeField] private SkillMarkerIconEntry[] _skillMarkerIcons;

        private Dictionary<CombatVfxType, EffectIconEntry> _effectCache;
        private Dictionary<int, SkillMarkerIconEntry> _markerCache;

        public bool TryGetEffectIcon(CombatVfxType type, out EffectIconEntry entry)
        {
            BuildCacheIfNeeded();
            return _effectCache.TryGetValue(type, out entry);
        }

        public bool TryGetMarkerIcon(int markerValue, out SkillMarkerIconEntry entry)
        {
            BuildCacheIfNeeded();
            return _markerCache.TryGetValue(markerValue, out entry);
        }

        private void BuildCacheIfNeeded()
        {
            if (_effectCache != null) return;

            _effectCache = new Dictionary<CombatVfxType, EffectIconEntry>();
            if (_effectIcons != null)
            {
                for (int i = 0; i < _effectIcons.Length; i++)
                {
                    var e = _effectIcons[i];
                    if (e.EffectType != CombatVfxType.None)
                        _effectCache[e.EffectType] = e;
                }
            }

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
            _effectCache = null;
            _markerCache = null;
        }

        private void OnValidate()
        {
            _effectCache = null;
            _markerCache = null;

            // EffectIcons 중복 체크
            if (_effectIcons != null)
            {
                var seen = new HashSet<CombatVfxType>();
                for (int i = 0; i < _effectIcons.Length; i++)
                {
                    var t = _effectIcons[i].EffectType;
                    if (t != CombatVfxType.None && !seen.Add(t))
                        Debug.LogError($"[BuffIconConfig] EffectIcons 중복: {t} (index {i})");
                }
            }

            // SkillMarkerIcons 중복 체크
            if (_skillMarkerIcons != null)
            {
                var seen = new HashSet<SkillMarkerType>();
                for (int i = 0; i < _skillMarkerIcons.Length; i++)
                {
                    var t = _skillMarkerIcons[i].MarkerType;
                    if (t != SkillMarkerType.None && !seen.Add(t))
                        Debug.LogError($"[BuffIconConfig] SkillMarkerIcons 중복: {t} (index {i})");
                }
            }
        }

#if UNITY_EDITOR
        private const string IconFolder = "Assets/_Project/Addressables/Remote/Textures/Icon/Icon_Buffs";
        private const string IconPrefix = "BuffDebuffIcon_";

        // CombatVfxType → 스프라이트 suffix 매핑 (폴더의 실제 파일명 기준)
        private static readonly Dictionary<CombatVfxType, string> KnownEffectMap = new()
        {
            { CombatVfxType.StatBuff_Attack,       "BUFF_AD_PERCENT_UP" },
            { CombatVfxType.StatBuff_Armor,        "BUFF_DEF_UP" },
            { CombatVfxType.StatBuff_MagicResist,  "BUFF_AD_REDUCE_UP" },
            { CombatVfxType.StatBuff_AttackSpeed,  "BUFF_ATK_SPEED_UP" },
            { CombatVfxType.ContinuousHeal,        "BUFF_SPECIAL_ENKI_PASSIVE_HEALUP" },
            { CombatVfxType.CCImmunity,            "BUFF_IMMUNE" },
            { CombatVfxType.DOTImmunity,           "BUFF_IMMUNE" },
            { CombatVfxType.DebuffImmunity,        "BUFF_IMMUNE" },
            { CombatVfxType.StatDebuff_Attack,     "DEBUFF_AD_PERCENT_DOWN" },
            { CombatVfxType.StatDebuff_Armor,      "DEBUFF_DEF_PERCENT_DOWN" },
            { CombatVfxType.StatDebuff_MagicResist,"DEBUFF_CRIT_DOWN" },
            { CombatVfxType.StatDebuff_AttackSpeed,"DEBUFF_ATK_SPEED_DOWN" },
            { CombatVfxType.ContinuousDamage,      "DEBUFF_POISON" },
            { CombatVfxType.HealAmountDown,        "DEBUFF_CHILL" },
            { CombatVfxType.CC_Stun,       "CC_STUN" },
            { CombatVfxType.CC_Silence,    "CC_SLIENCE" },
            { CombatVfxType.CC_Slow,       "DEBUFF_MOVE_DOWN" },
            { CombatVfxType.CC_Freeze,     "CC_FREEZ" },
            { CombatVfxType.CC_Taunt,      "CC_TARGET_IMPOSSIBLE" },
            { CombatVfxType.CC_Airborne,   "DEBUFF_AIRBORNE" },
            { CombatVfxType.CC_KnockBack,  "CC_KNOCKBACK" },
            { CombatVfxType.CC_TargetImpossible, "CC_TARGET_IMPOSSIBLE" },
            { CombatVfxType.Shield,        "SHIELD" },
            { CombatVfxType.StatBuff_DodgeChance, "BUFF_SPECIAL_SHIRAYUKI_AVOID_AND_ATTACK" },
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

        /// <summary>Icon_Buffs 폴더를 스캔하여 EffectIcons 배열에 Sprite 직접 매핑</summary>
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

            // 2) KnownEffectMap 기준으로 EffectIcons 생성 (Sprite 직접 참조)
            var effectList = new List<EffectIconEntry>();
            var usedSuffixes = new HashSet<string>();

            foreach (var kv in KnownEffectMap)
            {
                Sprite sprite = null;
                if (!spriteMap.TryGetValue(kv.Value, out sprite))
                {
                    Debug.LogWarning($"[BuffIconConfig] 스프라이트 없음: {IconPrefix}{kv.Value} (CombatVfxType.{kv.Key})");
                }

                effectList.Add(new EffectIconEntry
                {
                    EffectType = kv.Key,
                    IconSprite = sprite,
                });
                usedSuffixes.Add(kv.Value);
            }

            _effectIcons = effectList.ToArray();

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
                });
                Debug.Log($"[BuffIconConfig] SkillMarker 후보 추가: {IconPrefix}{kv.Key} (MarkerType 미설정)");
            }

            _skillMarkerIcons = markerList.ToArray();

            _effectCache = null;
            _markerCache = null;
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[BuffIconConfig] 자동 매핑 완료: Effect {effectList.Count}개, SkillMarker {markerList.Count}개, 미매핑 {unmapped}개");
        }
#endif
    }
}
