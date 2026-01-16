using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 시설 변경 정보
    /// </summary>
    public readonly struct FacilityChangeInfo
    {
        public ElpisFacility Current { get; init; }
        public ElpisFacility Previous { get; init; }

        // 편의 속성들
        public bool IsLevelChanged => Previous?.Level != Current?.Level;
        public bool HasPrevious => Previous != null;

        public FacilityChangeInfo(ElpisFacility current, ElpisFacility previous = null)
        {
            Current = current;
            Previous = previous;
        }
    }

    /// <summary>
    /// 코어 연구 변경 정보
    /// </summary>
    public readonly struct CoreResearchChangeInfo
    {
        public CoreResearch Current { get; init; }
        public CoreResearch Previous { get; init; }

        // 편의 속성들
        public bool IsLevelChanged => Previous?.Level != Current?.Level;
        public bool HasPrevious => Previous != null;

        public CoreResearchChangeInfo(CoreResearch current, CoreResearch previous = null)
        {
            Current = current;
            Previous = previous;
        }
    }

    /// <summary>
    /// 시뮬레이션 변경 정보
    /// </summary>
    public readonly struct SimulationChangeInfo
    {
        public SimulationData Current { get; init; }
        public SimulationData Previous { get; init; }

        // 편의 속성들
        public bool IsLevelChanged => Previous?.Level != Current?.Level;
        public bool IsAccumulatedBoxesChanged => Previous?.AccumulatedBoxes != Current?.AccumulatedBoxes;
        public bool HasPrevious => Previous != null;

        public SimulationChangeInfo(SimulationData current, SimulationData previous = null)
        {
            Current = current;
            Previous = previous;
        }
    }

    /// <summary>
    /// Elpis (영지) 데이터 모델
    /// 서버의 ElpisData 프로토콜을 래핑
    /// 델타 업데이트 지원
    /// </summary>
    public class ElpisModel
    {
        public const string CATEGORY_KEY = "elpis";

        // 프로토콜 데이터 (서버에서 받은 원본)
        private ElpisData _elpisData = new ();

        // 빠른 조회를 위한 캐시
        private readonly Dictionary<uint, ElpisFacility> _facilitiesCache = new (32);
        private readonly Dictionary<uint, CoreResearch> _coreResearchCache = new (16);

        // 디멘션 랩 캐시 데이터 (유저 레벨에 맞는 스펙 데이터, 0레벨 제외)
        private readonly List<ElpisDimensionLab> _cachedElpisDimensionLabs = new ();

        // R3 이벤트
        public Subject<Unit> OnChanged { get; } = new();
        public readonly Subject<FacilityChangeInfo> OnFacilityAdded = new();
        public readonly Subject<FacilityChangeInfo> OnFacilityUpdated = new();
        public readonly Subject<CoreResearchChangeInfo> OnCoreResearchUpdated = new();
        public readonly Subject<SimulationChangeInfo> OnSimulationUpdated = new();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _elpisData = new ElpisData();
            _facilitiesCache.Clear();
            _coreResearchCache.Clear();
            _cachedElpisDimensionLabs.Clear();
            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool Validate()
        {
            if (_elpisData == null)
            {
                Debug.LogError("[ElpisModel] ElpisData is null");
                return false;
            }

            // 시설 검증
            for (var i = 0; i < _elpisData.Facilities.Count; i++)
            {
                var facility = _elpisData.Facilities[i];
                if (facility.BuildId == 0)
                {
                    Debug.LogError("[ElpisModel] Invalid facility: missing BuildId");
                    return false;
                }
            }

            return true;
        }

        #region 시설 관련

        /// <summary>
        /// 시설 가져오기
        /// </summary>
        public ElpisFacility GetFacility(uint buildId)
        {
            return _facilitiesCache.GetValueOrDefault(buildId);
        }

        /// <summary>
        /// 모든 시설 가져오기
        /// </summary>
        public IReadOnlyList<ElpisFacility> GetAllFacilities()
        {
            return _elpisData?.Facilities;
        }

        /// <summary>
        /// 시설 개수
        /// </summary>
        public int FacilityCount => _elpisData?.Facilities?.Count ?? 0;

        /// <summary>
        /// 시설 존재 여부
        /// </summary>
        public bool HasFacility(uint buildId)
        {
            return _facilitiesCache.ContainsKey(buildId);
        }

        /// <summary>
        /// 특정 타입의 시설들 가져오기
        /// </summary>
        public void GetFacilitiesByType(List<ElpisFacility> output, ElpisFacilityType facilityType)
        {
            if (output == null) return;

            output.Clear();
            if (_elpisData?.Facilities == null) return;

            for (var i = 0; i < _elpisData.Facilities.Count; i++)
            {
                var facility = _elpisData.Facilities[i];
                if (facility.Type == facilityType)
                {
                    output.Add(facility);
                }
            }
        }

        /// <summary>
        /// 특정 타입의 시설 가져오기 (첫 번째)
        /// </summary>
        public ElpisFacility GetFacilityByType(ElpisFacilityType facilityType)
        {
            if (_elpisData?.Facilities == null) return null;

            for (var i = 0; i < _elpisData.Facilities.Count; i++)
            {
                var facility = _elpisData.Facilities[i];
                if (facility.Type == facilityType)
                {
                    return facility;
                }
            }

            return null;
        }

        #endregion

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

        #region 시뮬레이션 관련

        /// <summary>
        /// 시뮬레이션 데이터
        /// </summary>
        public SimulationData Simulation => _elpisData?.Simulation;

        /// <summary>
        /// 시뮬레이션 레벨
        /// </summary>
        public uint SimulationLevel => _elpisData?.Simulation?.Level ?? 0;

        /// <summary>
        /// 누적된 보상 상자 수
        /// </summary>
        public uint AccumulatedBoxes => _elpisData?.Simulation?.AccumulatedBoxes ?? 0;

        /// <summary>
        /// 최대 상자 수
        /// </summary>
        public uint MaxBoxes => _elpisData?.Simulation?.MaxBoxes ?? 0;

        /// <summary>
        /// 수령 가능한 상자가 있는지 여부
        /// </summary>
        public bool HasClaimableBoxes => AccumulatedBoxes > 0;

        #endregion

        #region 내부용 메서드

        /// <summary>
        /// 서버 응답으로 Elpis 데이터 설정
        /// 
        /// [호출 위치]
        /// - ElpisService.GetInfoAsync()에서 서버 응답 받은 후 호출 (37줄)
        /// - ElpisGetResponse.Elpis (ElpisData 타입)에 CoreResearches 필드 포함
        /// 
        /// [데이터 흐름]
        /// 1. elpisData.CoreResearches → _coreResearchCache에 저장
        /// 2. RebuildDimensionLabCache() 호출하여 _cachedElpisDimensionLabs 구성
        /// </summary>
        internal void SetElpisData(ElpisData elpisData)
        {
            if (elpisData == null)
            {
                Debug.LogError("[ElpisModel] ElpisData is null");
                return;
            }

            _elpisData = elpisData;

            // 캐시 재구성
            _facilitiesCache.Clear();
            for (var i = 0; i < elpisData.Facilities.Count; i++)
            {
                var facility = elpisData.Facilities[i];
                _facilitiesCache[facility.BuildId] = facility;
            }

            // 서버에서 받은 CoreResearches를 _coreResearchCache에 저장
            _coreResearchCache.Clear();
            for (var i = 0; i < elpisData.CoreResearches.Count; i++)
            {
                var research = elpisData.CoreResearches[i];
                _coreResearchCache[research.UpgradeGroupId] = research;
            }

            // _coreResearchCache를 기반으로 _cachedElpisDimensionLabs 재구성
            RebuildDimensionLabCache();

            OnChanged.OnNext(Unit.Default);
        }

        /// <summary>
        /// 시설 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateFacility(ElpisFacility facility)
        {
            if (facility == null)
            {
                Debug.LogError("[ElpisModel] Invalid facility data");
                return;
            }

            var previous = _facilitiesCache.GetValueOrDefault(facility.BuildId);
            bool isNew = previous == null;

            _facilitiesCache[facility.BuildId] = facility;

            // 프로토콜 데이터도 업데이트
            bool found = false;
            for (int i = 0; i < _elpisData.Facilities.Count; i++)
            {
                if (_elpisData.Facilities[i].BuildId == facility.BuildId)
                {
                    _elpisData.Facilities[i] = facility;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _elpisData.Facilities.Add(facility);
            }

            var changeInfo = new FacilityChangeInfo(facility, previous);
            if (isNew)
                OnFacilityAdded.OnNext(changeInfo);
            else
                OnFacilityUpdated.OnNext(changeInfo);
        }

        /// <summary>
        /// 코어 연구 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateCoreResearch(CoreResearch research)
        {
            if (research == null)
            {
                Debug.LogError("[ElpisModel] Invalid core research data");
                return;
            }

            var previous = _coreResearchCache.GetValueOrDefault(research.UpgradeGroupId);
            _coreResearchCache[research.UpgradeGroupId] = research;

            // 프로토콜 데이터도 업데이트
            bool found = false;
            for (int i = 0; i < _elpisData.CoreResearches.Count; i++)
            {
                if (_elpisData.CoreResearches[i].UpgradeGroupId == research.UpgradeGroupId)
                {
                    _elpisData.CoreResearches[i] = research;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _elpisData.CoreResearches.Add(research);
            }

            UpdateDimensionLabCache(research);

            var changeInfo = new CoreResearchChangeInfo(research, previous);
            OnCoreResearchUpdated.OnNext(changeInfo);
        }

        /// <summary>
        /// 시뮬레이션 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateSimulation(SimulationData simulation)
        {
            if (simulation == null)
            {
                Debug.LogError("[ElpisModel] Invalid simulation data");
                return;
            }

            var previous = _elpisData.Simulation;
            _elpisData.Simulation = simulation;

            var changeInfo = new SimulationChangeInfo(simulation, previous);
            OnSimulationUpdated.OnNext(changeInfo);
        }

        #endregion
    }
}