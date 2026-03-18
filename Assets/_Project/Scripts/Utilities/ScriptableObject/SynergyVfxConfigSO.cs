using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    /// <summary>시너지 VFX 태그</summary>
    public enum SynergyVfxTag
    {
        None = 0,
        BoardObject,   // 보드 위 드래그 가능 오브젝트 프리팹
        TargetVfx,     // 유닛 부여 시 원샷 이펙트
        Tier1,         // 티어1 이펙트
        Tier2,         // 티어2 이펙트
        Tier3,         // 티어3 이펙트
    }

    /// <summary>
    /// 시너지 VFX 통합 설정.
    /// SynergyType별 달성 시 원샷 이펙트 매핑.
    /// </summary>
    [CreateAssetMenu(fileName = "SynergyVfxConfig", menuName = "AutoChess/Synergy Vfx Config")]
    public class SynergyVfxConfigSO : ScriptableObject
    {
        [Serializable]
        public struct SynergyVfxEntry
        {
            public SynergyType SynergyType;

            [Header("달성 이펙트 (단계 도달 시 원샷)")]
            public AssetReferenceGameObject AchieveVfx;
            public SkillPosition AchievePosition;

            [Header("효과 발동 시 VFX")]
            public TaggedVfx[] EffectVfxList;
        }

        [Serializable]
        public struct TaggedVfx
        {
            public SynergyVfxTag Tag;
            public AssetReferenceGameObject Vfx;
            public SkillPosition Position;
            public bool Follow;
        }

        [SerializeField] private SynergyVfxEntry[] _entries;

        private Dictionary<SynergyType, SynergyVfxEntry> _cache;

        /// <summary>특정 시너지의 VFX 엔트리 조회</summary>
        public bool TryGetEntry(SynergyType synergyType, out SynergyVfxEntry entry)
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue(synergyType, out entry);
        }

        /// <summary>특정 시너지의 태그 VFX 조회</summary>
        public bool TryGetTaggedVfx(SynergyType synergyType, SynergyVfxTag tag, out TaggedVfx result)
        {
            result = default;
            if (!TryGetEntry(synergyType, out var entry)) return false;
            if (entry.EffectVfxList == null) return false;

            for (int i = 0; i < entry.EffectVfxList.Length; i++)
            {
                if (entry.EffectVfxList[i].Tag == tag)
                {
                    result = entry.EffectVfxList[i];
                    return result.Vfx != null && result.Vfx.RuntimeKeyIsValid();
                }
            }
            return false;
        }

        /// <summary>티어 번호(1~3) → SynergyVfxTag 변환</summary>
        public static SynergyVfxTag TierToTag(int tier) => tier switch
        {
            1 => SynergyVfxTag.Tier1,
            2 => SynergyVfxTag.Tier2,
            3 => SynergyVfxTag.Tier3,
            _ => SynergyVfxTag.None,
        };

        private void BuildCache()
        {
            _cache = new Dictionary<SynergyType, SynergyVfxEntry>();

            if (_entries == null) return;

            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e.SynergyType == SynergyType.NONE) continue;
                _cache[e.SynergyType] = e;
            }
        }

        private void OnEnable()
        {
            _cache = null;
        }
    }
}
