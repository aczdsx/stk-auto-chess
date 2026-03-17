using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 시너지 VFX 통합 설정.
    /// SynergyType별, 단계(Tier)별 VFX AssetReference 매핑.
    /// - Achieve: 시너지 단계 달성 시 원샷 이펙트
    /// - Apply: 해당 시너지 유닛에 적용되는 루프 이펙트
    /// </summary>
    [CreateAssetMenu(fileName = "SynergyVfxConfig", menuName = "AutoChess/Synergy Vfx Config")]
    public class SynergyVfxConfigSO : ScriptableObject
    {
        [Serializable]
        public struct TierVfxEntry
        {
            [Tooltip("시너지 단계 인덱스 (0-based, SynergySpec.Tiers 인덱스와 대응)")]
            public int TierIndex;

            [Header("달성 이펙트 (단계 도달 시 원샷)")]
            public AssetReferenceGameObject AchieveVfx;
            public SkillPosition AchievePosition;
            public bool AchieveFollowable;

            [Header("적용 이펙트 (해당 단계 동안 유닛에 루프)")]
            public AssetReferenceGameObject ApplyVfx;
            public SkillPosition ApplyPosition;
            public bool ApplyFollowable;
        }

        [Serializable]
        public struct SynergyVfxEntry
        {
            public SynergyType SynergyType;
            public TierVfxEntry[] Tiers;
        }

        [SerializeField] private SynergyVfxEntry[] _entries;

        // SynergyType → (TierIndex → TierVfxEntry) 캐시
        private Dictionary<SynergyType, Dictionary<int, TierVfxEntry>> _cache;

        /// <summary>특정 시너지 + 단계의 VFX 엔트리 조회</summary>
        public bool TryGetTierEntry(SynergyType synergyType, int tierIndex, out TierVfxEntry entry)
        {
            if (_cache == null) BuildCache();

            if (_cache.TryGetValue(synergyType, out var tierMap))
                return tierMap.TryGetValue(tierIndex, out entry);

            entry = default;
            return false;
        }

        /// <summary>특정 시너지의 모든 단계 VFX 조회</summary>
        public bool TryGetAllTiers(SynergyType synergyType, out Dictionary<int, TierVfxEntry> tierMap)
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue(synergyType, out tierMap);
        }

        private void BuildCache()
        {
            _cache = new Dictionary<SynergyType, Dictionary<int, TierVfxEntry>>();

            if (_entries == null) return;

            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (e.SynergyType == SynergyType.NONE || e.Tiers == null) continue;

                var tierMap = new Dictionary<int, TierVfxEntry>();
                for (int t = 0; t < e.Tiers.Length; t++)
                {
                    tierMap[e.Tiers[t].TierIndex] = e.Tiers[t];
                }

                _cache[e.SynergyType] = tierMap;
            }
        }

        private void OnEnable()
        {
            _cache = null;
        }
    }
}
