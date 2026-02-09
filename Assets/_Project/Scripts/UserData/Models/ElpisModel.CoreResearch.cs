using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public partial class ElpisModel
    {
        #region 코어 연구 관련

        /// <summary>
        /// 코어 연구 가져오기
        /// </summary>
        public CoreResearch GetCoreResearch(uint groupId)
        {
            return _coreResearchCache.GetValueOrDefault(groupId);
        }

        /// <summary>
        /// 모든 코어 연구 가져오기
        /// </summary>
        public void GetAllCoreResearches(List<CoreResearch> output)
        {
            if (output == null) return;

            output.Clear();
            if (_elpisData?.CoreResearches == null) return;

            output.Capacity = Math.Max(output.Capacity, _elpisData.CoreResearches.Count);

            for (var i = 0; i < _elpisData.CoreResearches.Count; i++)
            {
                var research = _elpisData.CoreResearches[i];
                output.Add(research);
            }
        }

        /// <summary>
        /// 코어 연구 개수
        /// </summary>
        public int CoreResearchCount => _elpisData?.CoreResearches?.Count ?? 0;

        #endregion

        #region 디멘션 랩 캐시 관련

        /// <summary>
        /// 캐시된 디멘션 랩 데이터 (유저 레벨에 맞는 스펙 데이터, 0레벨 제외)
        /// </summary>
        public IReadOnlyList<ElpisDimensionLab> CachedElpisDimensionLabs => _cachedElpisDimensionLabs;
        public const string CoreResearchBadgePathPrefix = "CoreResearch";

        /// <summary>
        /// 디멘션 랩 캐시 재구성
        ///
        /// [데이터 흐름]
        /// 1. 서버에서 데이터 수신: ElpisService.GetInfoAsync() → ElpisGetResponse.Elpis (ElpisData 타입)
        /// 2. SetElpisData() 호출: elpisData.CoreResearches → _coreResearchCache에 저장 (359-364줄)
        /// 3. RebuildDimensionLabCache() 호출: _coreResearchCache → _cachedElpisDimensionLabs 구성
        ///
        /// [주의사항]
        /// - _coreResearchCache는 SetElpisData() 또는 UpdateCoreResearch()에서만 채워짐
        /// - 이 함수가 호출될 때 _coreResearchCache가 비어있으면 _cachedElpisDimensionLabs도 비어있게 됨
        /// </summary>
        public void RebuildDimensionLabCache()
        {
            _cachedElpisDimensionLabs.Clear();

            // _coreResearchCache가 비어있으면 아무것도 추가하지 않음
            if (_coreResearchCache.Count == 0)
            {
                Debug.LogWarning("[ElpisModel] RebuildDimensionLabCache: _coreResearchCache가 비어있습니다. SetElpisData()가 먼저 호출되어야 합니다.");
                return;
            }

            var allSpecs = SpecDataManager.Instance.GetAllElpisDimensionLab();

            foreach (var coreResearch in _coreResearchCache.Values)
            {
                // 0레벨은 제외
                if (coreResearch.Level <= 0)
                    continue;

                // 해당 UpgradeGroupId와 Level에 맞는 스펙 데이터 찾기
                for (var i = 0; i < allSpecs.Count; i++)
                {
                    var spec = allSpecs[i];
                    if (spec.upgrade_group_id == coreResearch.UpgradeGroupId && spec.lv == coreResearch.Level)
                    {
                        _cachedElpisDimensionLabs.Add(spec);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 단일 디멘션 랩 캐시 업데이트
        /// </summary>
        private void UpdateDimensionLabCache(CoreResearch coreResearch)
        {
            // 기존 캐시에서 같은 UpgradeGroupId 제거
            for (var i = _cachedElpisDimensionLabs.Count - 1; i >= 0; i--)
            {
                if (_cachedElpisDimensionLabs[i].upgrade_group_id == coreResearch.UpgradeGroupId)
                {
                    _cachedElpisDimensionLabs.RemoveAt(i);
                    break;
                }
            }

            // 0레벨은 추가하지 않음
            if (coreResearch.Level <= 0)
                return;

            // 해당 레벨의 스펙 데이터 찾아서 추가
            var allSpecs = SpecDataManager.Instance.GetAllElpisDimensionLab();
            for (var i = 0; i < allSpecs.Count; i++)
            {
                var spec = allSpecs[i];
                if (spec.upgrade_group_id == coreResearch.UpgradeGroupId && spec.lv == coreResearch.Level)
                {
                    _cachedElpisDimensionLabs.Add(spec);
                    break;
                }
            }
        }

        #endregion

        #region 뱃지 갱신
        /// <summary>
        /// 코어 연구 뱃지 경로
        /// </summary>
        public static string GetCoreResearchBadgePath(int itemId)
        {
            return $"{CoreResearchBadgePathPrefix}/{itemId}";
        }
        
        /// <summary>
        /// CoreResearch 관련 뱃지 갱신
        /// </summary>
        public void RefreshCoreResearchBadges()
        {
            var inventoryModel = ServerDataManager.Instance.Inventory;
            var allSpecs = SpecDataManager.Instance.GetAllElpisDimensionLab();

            // upgrade_group_id 별로 처리
            var processedGroups = new HashSet<int>();

            foreach (var spec in allSpecs)
            {
                if (processedGroups.Contains(spec.upgrade_group_id))
                    continue;

                processedGroups.Add(spec.upgrade_group_id);

                var coreResearch = GetCoreResearch((uint)spec.upgrade_group_id);
                var currentLevel = coreResearch?.Level ?? 0;

                // 다음 레벨 스펙 찾기
                ElpisDimensionLab nextLevelSpec = null;
                foreach (var s in allSpecs)
                {
                    if (s.upgrade_group_id == spec.upgrade_group_id && s.lv == currentLevel + 1)
                    {
                        nextLevelSpec = s;
                        break;
                    }
                }

                // 다음 레벨이 없으면 Max 상태
                if (nextLevelSpec == null)
                {
                    // Max 상태일 때는 현재 레벨의 item_id로 Badge 제거
                    var currentSpec = GetCurrentLevelSpec(allSpecs, spec.upgrade_group_id, currentLevel);
                    if (currentSpec != null)
                    {
                        var maxPath = $"{CoreResearchBadgePathPrefix}/{currentSpec.item_id}";
                        BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, maxPath);
                    }
                    continue;
                }

                // item_id 기준 path
                var path = $"{CoreResearchBadgePathPrefix}/{nextLevelSpec.item_id}";

                // 재화 확인
                var currentAsset = inventoryModel.GetCurrency((uint)nextLevelSpec.item_id);
                var canUpgrade = currentAsset >= (ulong)nextLevelSpec.item_INT;

                if (canUpgrade)
                    BadgeManager.Instance.AddBadge(BadgeType.RedDot, path);
                else
                    BadgeManager.Instance.RemoveBadge(BadgeType.RedDot, path);
            }
        }

        private ElpisDimensionLab GetCurrentLevelSpec(IReadOnlyList<ElpisDimensionLab> allSpecs, int upgradeGroupId, uint level)
        {
            foreach (var s in allSpecs)
            {
                if (s.upgrade_group_id == upgradeGroupId && s.lv == level)
                    return s;
            }
            return null;
        }

        #endregion
    }
}
