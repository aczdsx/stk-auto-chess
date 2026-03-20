using System.Collections.Generic;
using CookApps.AutoBattler;
using UnityEngine;

namespace CookApps.AutoChess.View
{
    public class BuffIconTracker
    {
        private readonly UnitViewManager _unitViewManager;
        private readonly float _secondsPerFrame;
        private readonly BuffIconConfigSO _config;

        private struct ActiveBuff
        {
            public Sprite IconSprite;
            public float TotalDuration;
            public float AddedTime;
            public int RefCount;
            public bool IsSkillMarker;
            public int MarkerId;   // encoded (CombatVfxType+StatModType) or (int)SkillMarkerType
        }

        private readonly Dictionary<int, List<ActiveBuff>> _activeBuffs = new();
        private readonly List<HpBarView.NewBuffIconData> _tempBuffList = new();

        public BuffIconTracker(UnitViewManager unitViewManager, int tickRate, BuffIconConfigSO config)
        {
            _unitViewManager = unitViewManager;
            _secondsPerFrame = 1f / tickRate;
            _config = config;
        }

        // ── CombatVfxType 기반 (StatusEffect/CC) ──

        public void OnEffectAdded(int combatId, CombatVfxType type, int totalFrames, StatModType statType = default)
        {
            if (_config == null || !_config.TryGetEffectIcon(type, statType, out var entry)) return;
            if (entry.IconSprite == null) return;

            var list = GetOrCreateList(combatId);
            int key = SimEventHelper.EncodeVfxStat(type, statType);

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].IsSkillMarker && list[i].MarkerId == key)
                {
                    var e = list[i];
                    e.RefCount++;
                    e.TotalDuration = totalFrames * _secondsPerFrame;
                    e.AddedTime = Time.time;
                    list[i] = e;
                    UpdateUnitBuffIcons(combatId);
                    return;
                }
            }

            list.Add(new ActiveBuff
            {
                IconSprite = entry.IconSprite,
                TotalDuration = totalFrames * _secondsPerFrame,
                AddedTime = Time.time,
                RefCount = 1,
                IsSkillMarker = false,
                MarkerId = key,
            });
            UpdateUnitBuffIcons(combatId);
        }

        public void OnEffectRemoved(int combatId, CombatVfxType type, StatModType statType = default)
        {
            if (!_activeBuffs.TryGetValue(combatId, out var list)) return;
            int key = SimEventHelper.EncodeVfxStat(type, statType);

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].IsSkillMarker && list[i].MarkerId == key)
                {
                    var e = list[i];
                    e.RefCount--;
                    if (e.RefCount <= 0)
                        list.RemoveAt(i);
                    else
                        list[i] = e;
                    UpdateUnitBuffIcons(combatId);
                    return;
                }
            }
        }

        // ── SkillMarker 기반 ──

        public void OnSkillMarkerAdded(int combatId, int markerId, int totalFrames)
        {
            if (_config == null || !_config.TryGetMarkerIcon(markerId, out var entry)) return;
            if (entry.IconSprite == null) return;

            var list = GetOrCreateList(combatId);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsSkillMarker && list[i].MarkerId == markerId)
                {
                    var e = list[i];
                    e.RefCount++;
                    e.TotalDuration = totalFrames * _secondsPerFrame;
                    e.AddedTime = Time.time;
                    list[i] = e;
                    UpdateUnitBuffIcons(combatId);
                    return;
                }
            }

            list.Add(new ActiveBuff
            {
                IconSprite = entry.IconSprite,
                TotalDuration = totalFrames * _secondsPerFrame,
                AddedTime = Time.time,
                RefCount = 1,
                IsSkillMarker = true,
                MarkerId = markerId,
            });
            UpdateUnitBuffIcons(combatId);
        }

        public void OnSkillMarkerRemoved(int combatId, int markerId, int remainingCount)
        {
            if (!_activeBuffs.TryGetValue(combatId, out var list)) return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].IsSkillMarker && list[i].MarkerId == markerId)
                {
                    if (remainingCount <= 0)
                        list.RemoveAt(i);
                    else
                    {
                        var e = list[i];
                        e.RefCount = remainingCount;
                        list[i] = e;
                    }
                    UpdateUnitBuffIcons(combatId);
                    return;
                }
            }
        }

        /// <summary>뷰 생성 후 호출: 이미 추적 중인 버프 아이콘을 뷰에 반영</summary>
        public void RefreshIconsForUnit(int combatId)
        {
            if (_activeBuffs.ContainsKey(combatId))
                UpdateUnitBuffIcons(combatId);
        }

        public void OnCombatEnd()
        {
            _activeBuffs.Clear();
        }

        // ── 내부 ──

        private static bool IsShieldType(int encodedKey)
        {
            var vfxType = SimEventHelper.DecodeVfxType(encodedKey);
            // Shield + 향후 NormalAttackShield 등 쉴드 계열 추가 시 여기에 포함
            return vfxType == CombatVfxType.Shield;
        }

        private List<ActiveBuff> GetOrCreateList(int combatId)
        {
            if (!_activeBuffs.TryGetValue(combatId, out var list))
            {
                list = new List<ActiveBuff>();
                _activeBuffs[combatId] = list;
            }
            return list;
        }

        private readonly Dictionary<int, int> _tempReplacedCounts = new();

        private void UpdateUnitBuffIcons(int combatId)
        {
            var unitView = _unitViewManager?.FindCombatView(combatId);
            if (unitView == null) return;

            _tempBuffList.Clear();
            if (!_activeBuffs.TryGetValue(combatId, out var list))
            {
                unitView.UpdateBuffIcons(_tempBuffList);
                return;
            }

            // 1) 마커별 대체 카운트 수집
            _tempReplacedCounts.Clear();
            foreach (var buff in list)
            {
                if (!buff.IsSkillMarker) continue;
                if (_config != null && _config.TryGetMarkerIcon(buff.MarkerId, out var markerEntry))
                {
                    if (markerEntry.ReplacesEffect != CombatVfxType.None)
                    {
                        int key = SimEventHelper.EncodeVfxStat(markerEntry.ReplacesEffect, markerEntry.ReplacesStatType);
                        _tempReplacedCounts.TryGetValue(key, out int count);
                        _tempReplacedCounts[key] = count + buff.RefCount;
                    }
                }
            }

            // 2) 아이콘 목록 생성 (마커 RefCount만큼 차감)
            foreach (var buff in list)
            {
                if (!buff.IsSkillMarker && _tempReplacedCounts.TryGetValue(buff.MarkerId, out int replaceCount))
                {
                    if (buff.RefCount <= replaceCount) continue;
                    _tempBuffList.Add(new HpBarView.NewBuffIconData
                    {
                        IconSprite = buff.IconSprite,
                        Duration = buff.TotalDuration,
                        ElapsedTime = Time.time - buff.AddedTime,
                        StackCount = 1,
                        IsSide = false,
                    });
                    continue;
                }

                _tempBuffList.Add(new HpBarView.NewBuffIconData
                {
                    IconSprite = buff.IconSprite,
                    Duration = buff.TotalDuration,
                    ElapsedTime = Time.time - buff.AddedTime,
                    StackCount = buff.RefCount,
                    IsSide = !buff.IsSkillMarker && IsShieldType(buff.MarkerId),
                });
            }

            unitView.UpdateBuffIcons(_tempBuffList);
        }
    }
}
