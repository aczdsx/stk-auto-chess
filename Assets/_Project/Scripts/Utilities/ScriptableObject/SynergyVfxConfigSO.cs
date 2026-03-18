using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CookApps.AutoChess.View
{
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
            public string Tag;
            public AssetReferenceGameObject Vfx;
        }

        [SerializeField] private SynergyVfxEntry[] _entries;

        private Dictionary<SynergyType, SynergyVfxEntry> _cache;

        /// <summary>특정 시너지의 VFX 엔트리 조회</summary>
        public bool TryGetEntry(SynergyType synergyType, out SynergyVfxEntry entry)
        {
            if (_cache == null) BuildCache();
            return _cache.TryGetValue(synergyType, out entry);
        }

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
