using System;
using System.Collections.Generic;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Elpis (영지) 데이터 모델
    /// 서버의 ElpisData 프로토콜을 래핑
    /// 델타 업데이트 지원
    /// </summary>
    public class ElpisModel : IDataModel
    {
        public const string CATEGORY_KEY = "elpis";

        // 프로토콜 데이터 (서버에서 받은 원본)
        private ElpisData _elpisData;

        // 빠른 조회를 위한 캐시
        private readonly Dictionary<string, ElpisFacility> _facilitiesCache;
        private readonly Dictionary<CoreResearchType, CoreResearch> _coreResearchCache;
        
        // 버전 정보
        private int _version;

        public string CategoryKey => CATEGORY_KEY;
        public int Version => _version;

        // 이벤트
        public event Action OnChanged;
        public event Action<uint> OnLevelChanged;
        public event Action<uint> OnExpansionLevelChanged;
        public event Action<ElpisFacility> OnFacilityAdded;
        public event Action<ElpisFacility> OnFacilityUpdated;
        public event Action<CoreResearch> OnCoreResearchUpdated;
        public event Action<SimulationData> OnSimulationUpdated;

        public ElpisModel()
        {
            _elpisData = new ElpisData();
            _facilitiesCache = new Dictionary<string, ElpisFacility>(32);
            _coreResearchCache = new Dictionary<CoreResearchType, CoreResearch>(16);
#if EXCEPT_1ST_MILESTONE
            _tacticResearchCache = new Dictionary<TacticResearchType, TacticResearch>(32);
#endif
            _version = 0;
        }

        /// <summary>
        /// 델타 업데이트 적용
        /// </summary>
        public void ApplyDelta(IDataModel delta)
        {
            if (delta is not ElpisModel elpisDelta)
            {
                Debug.LogError("[ElpisModel] Invalid delta type");
                return;
            }

            if (elpisDelta._elpisData == null)
            {
                Debug.LogError("[ElpisModel] Delta data is null");
                return;
            }

            // 시설 업데이트
            foreach (var facility in elpisDelta._elpisData.Facilities)
            {
                bool isNew = !_facilitiesCache.ContainsKey(facility.InstanceId);
                _facilitiesCache[facility.InstanceId] = facility;

                if (isNew)
                    OnFacilityAdded?.Invoke(facility);
                else
                    OnFacilityUpdated?.Invoke(facility);
            }

            // 코어 연구 업데이트
            foreach (var research in elpisDelta._elpisData.CoreResearches)
            {
                _coreResearchCache[research.Type] = research;
                OnCoreResearchUpdated?.Invoke(research);
            }

            // 시뮬레이션 업데이트
            if (elpisDelta._elpisData.Simulation != null)
            {
                _elpisData.Simulation = elpisDelta._elpisData.Simulation;
                OnSimulationUpdated?.Invoke(_elpisData.Simulation);
            }

            _version = elpisDelta._version;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _elpisData = new ElpisData();
            _facilitiesCache.Clear();
            _coreResearchCache.Clear();
            _version = 0;
            OnChanged?.Invoke();
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
            foreach (var facility in _elpisData.Facilities)
            {
                if (string.IsNullOrEmpty(facility.InstanceId))
                {
                    Debug.LogError("[ElpisModel] Invalid facility: missing InstanceId");
                    return false;
                }
            }

            return true;
        }

        #region 시설 관련

        /// <summary>
        /// 시설 가져오기
        /// </summary>
        public ElpisFacility GetFacility(string instanceId)
        {
            return _facilitiesCache.TryGetValue(instanceId, out var facility) ? facility : null;
        }

        /// <summary>
        /// 모든 시설 가져오기
        /// </summary>
        public void GetAllFacilities(List<ElpisFacility> output)
        {
            if (output == null) return;

            output.Clear();
            if (_elpisData?.Facilities == null) return;

            output.Capacity = Math.Max(output.Capacity, _elpisData.Facilities.Count);

            foreach (var facility in _elpisData.Facilities)
            {
                output.Add(facility);
            }
        }

        /// <summary>
        /// 시설 개수
        /// </summary>
        public int FacilityCount => _elpisData?.Facilities?.Count ?? 0;

        /// <summary>
        /// 시설 존재 여부
        /// </summary>
        public bool HasFacility(string instanceId)
        {
            return _facilitiesCache.ContainsKey(instanceId);
        }

        /// <summary>
        /// 특정 타입의 시설들 가져오기
        /// </summary>
        public void GetFacilitiesByType(List<ElpisFacility> output, ElpisFacilityType facilityType)
        {
            if (output == null) return;

            output.Clear();
            if (_elpisData?.Facilities == null) return;

            foreach (var facility in _elpisData.Facilities)
            {
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

            foreach (var facility in _elpisData.Facilities)
            {
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
        public CoreResearch GetCoreResearch(CoreResearchType researchType)
        {
            return _coreResearchCache.TryGetValue(researchType, out var research) ? research : null;
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

            foreach (var research in _elpisData.CoreResearches)
            {
                output.Add(research);
            }
        }

        /// <summary>
        /// 코어 연구 개수
        /// </summary>
        public int CoreResearchCount => _elpisData?.CoreResearches?.Count ?? 0;

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
        /// 서버 응답으로 Elpis 데이터 설정 (내부용)
        /// </summary>
        internal void SetElpisData(ElpisData elpisData, int version)
        {
            if (elpisData == null)
            {
                Debug.LogError("[ElpisModel] ElpisData is null");
                return;
            }

            _elpisData = elpisData;

            // 캐시 재구성
            _facilitiesCache.Clear();
            foreach (var facility in elpisData.Facilities)
            {
                if (!string.IsNullOrEmpty(facility.InstanceId))
                {
                    _facilitiesCache[facility.InstanceId] = facility;
                }
            }

            _coreResearchCache.Clear();
            foreach (var research in elpisData.CoreResearches)
            {
                _coreResearchCache[research.Type] = research;
            }

            _version = version;
            OnChanged?.Invoke();
        }

        /// <summary>
        /// 시설 업데이트 (서버 응답용)
        /// </summary>
        internal void UpdateFacility(ElpisFacility facility)
        {
            if (facility == null || string.IsNullOrEmpty(facility.InstanceId))
            {
                Debug.LogError("[ElpisModel] Invalid facility data");
                return;
            }

            bool isNew = !_facilitiesCache.ContainsKey(facility.InstanceId);
            _facilitiesCache[facility.InstanceId] = facility;

            // 프로토콜 데이터도 업데이트
            bool found = false;
            for (int i = 0; i < _elpisData.Facilities.Count; i++)
            {
                if (_elpisData.Facilities[i].InstanceId == facility.InstanceId)
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

            if (isNew)
                OnFacilityAdded?.Invoke(facility);
            else
                OnFacilityUpdated?.Invoke(facility);

            _version++;
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

            _coreResearchCache[research.Type] = research;

            // 프로토콜 데이터도 업데이트
            bool found = false;
            for (int i = 0; i < _elpisData.CoreResearches.Count; i++)
            {
                if (_elpisData.CoreResearches[i].Type == research.Type)
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

            OnCoreResearchUpdated?.Invoke(research);
            _version++;
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

            _elpisData.Simulation = simulation;
            OnSimulationUpdated?.Invoke(simulation);
            _version++;
        }

        #endregion
    }
}