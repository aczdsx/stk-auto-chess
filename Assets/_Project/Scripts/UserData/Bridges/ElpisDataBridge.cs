using System.Collections.Generic;
using R3;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Elpis 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class ElpisDataBridge : DataBridgeBase
    {
        private ElpisModel Model;
        // Public Observable 노출
        public Observable<FacilityChangeInfo> OnFacilityAdded;
        public Observable<FacilityChangeInfo> OnFacilityChanged;
        public Observable<CoreResearchChangeInfo> OnCoreResearchChanged;
        public Observable<SimulationChangeInfo> OnSimulationChanged;

        public ElpisDataBridge()
        {
            Model = ServerDataManager.Instance.Elpis;
            OnFacilityAdded = Model.OnFacilityAdded;
            OnFacilityChanged = Model.OnFacilityUpdated;
            OnCoreResearchChanged = Model.OnCoreResearchUpdated;
            OnSimulationChanged = Model.OnSimulationUpdated;
        }

        #region 시설 관련

        /// <summary>
        /// 특정 시설 가져오기
        /// </summary>
        public ElpisFacility GetFacility(uint buildId)
        {
            return Model?.GetFacility(buildId);
        }

        /// <summary>
        /// 모든 시설 가져오기
        /// </summary>
        public IReadOnlyList<ElpisFacility> GetAllFacilities()
        {
            return Model?.GetAllFacilities();
        }

        /// <summary>
        /// 시설 개수
        /// </summary>
        public int FacilityCount => Model?.FacilityCount ?? 0;

        /// <summary>
        /// 시설 존재 여부
        /// </summary>
        public bool HasFacility(uint buildId)
        {
            return Model?.HasFacility(buildId) ?? false;
        }

        /// <summary>
        /// 시설이 존재하고 건설이 완료된 상태인지
        /// (건설 중이 아님 = 완료됨)
        /// </summary>
        public bool IsFacilityBuildComplete(uint buildId)
        {
            var facility = GetFacility(buildId);
            return facility != null && !facility.IsBuilding;
        }

        /// <summary>
        /// 특정 타입의 시설들 가져오기
        /// </summary>
        public void GetFacilitiesByType(List<ElpisFacility> output, ElpisFacilityType facilityType)
        {
            Model?.GetFacilitiesByType(output, facilityType);
        }

        /// <summary>
        /// 특정 타입의 시설 가져오기 (첫 번째)
        /// </summary>
        public ElpisFacility GetFacilityByType(ElpisFacilityType facilityType)
        {
            return Model?.GetFacilityByType(facilityType);
        }

        /// <summary>
        /// 특정 타입의 시설 레벨
        /// </summary>
        public uint GetFacilityLevel(ElpisFacilityType facilityType)
        {
            var facility = GetFacilityByType(facilityType);
            return facility?.Level ?? 0;
        }

        /// <summary>
        /// 특정 타입의 시설이 최대 레벨인지 확인
        /// </summary>
        public bool IsFacilityMaxLevel(ElpisFacilityType facilityType)
        {
            var facility = GetFacilityByType(facilityType);
            if (facility == null) return false;
            return facility.Level >= facility.MaxLevel;
        }

        /// <summary>
        /// 특정 타입의 시설 보유 여부
        /// </summary>
        public bool HasFacilityType(ElpisFacilityType facilityType)
        {
            return GetFacilityByType(facilityType) != null;
        }

        /// <summary>
        /// 최소 레벨 이상의 시설들 가져오기
        /// </summary>
        public void GetFacilitiesByMinLevel(List<ElpisFacility> output, uint minLevel)
        {
            if (Model == null || output == null) return;

            output.Clear();
            var allFacilities = Model.GetAllFacilities();

            for (int i = 0; i < allFacilities.Count; i++)
            {
                if (allFacilities[i].Level >= minLevel)
                {
                    output.Add(allFacilities[i]);
                }
            }
        }

        #endregion

        #region 코어 연구 관련

        public IReadOnlyList<ElpisDimensionLab> GetCurrentDimensionCoreLabs()
        {
            return Model?.CachedElpisDimensionLabs;
        }

        /// <summary>
        /// 코어 연구 가져오기
        /// </summary>
        public CoreResearch GetCoreResearch(uint groupId)
        {
            return Model?.GetCoreResearch(groupId);
        }

        /// <summary>
        /// 모든 코어 연구 가져오기
        /// </summary>
        public void GetAllCoreResearches(List<CoreResearch> output)
        {
            Model?.GetAllCoreResearches(output);
        }

        /// <summary>
        /// 코어 연구 개수
        /// </summary>
        public int CoreResearchCount => Model?.CoreResearchCount ?? 0;

        /// <summary>
        /// 코어 연구 레벨
        /// </summary>
        public uint GetCoreResearchLevel(uint groupId)
        {
            var research = GetCoreResearch(groupId);
            return research?.Level ?? 0;
        }
        #endregion

        #region 시뮬레이션 관련

        /// <summary>
        /// 시뮬레이션 데이터
        /// </summary>
        public SimulationData Simulation => Model?.Simulation;

        /// <summary>
        /// 시뮬레이션 레벨
        /// </summary>
        public uint SimulationLevel => Model?.SimulationLevel ?? 0;

        /// <summary>
        /// 누적된 보상 상자 수
        /// </summary>
        public uint AccumulatedBoxes => Model?.AccumulatedBoxes ?? 0;

        /// <summary>
        /// 최대 상자 수
        /// </summary>
        public uint MaxBoxes => Model?.MaxBoxes ?? 0;

        /// <summary>
        /// 수령 가능한 상자가 있는지 여부
        /// </summary>
        public bool HasClaimableBoxes => Model?.HasClaimableBoxes ?? false;

        /// <summary>
        /// 시뮬레이션 최대 레벨 여부
        /// </summary>
        public bool IsSimulationMaxLevel
        {
            get
            {
                var sim = Simulation;
                if (sim == null) return false;
                return sim.Level >= sim.MaxLevel;
            }
        }

        /// <summary>
        /// 상자가 가득 찼는지 여부
        /// </summary>
        public bool IsBoxesFull => AccumulatedBoxes >= MaxBoxes;

        #endregion

        #region 유틸리티

        /// <summary>
        /// 특정 시설 타입이 특정 레벨 이상인지 확인
        /// </summary>
        public bool IsFacilityLevelAtLeast(ElpisFacilityType facilityType, uint minLevel)
        {
            return GetFacilityLevel(facilityType) >= minLevel;
        }

        /// <summary>
        /// 특정 코어 연구가 특정 레벨 이상인지 확인
        /// </summary>
        public bool IsCoreResearchLevelAtLeast(uint groupId, uint minLevel)
        {
            return GetCoreResearchLevel(groupId) >= minLevel;
        }

        #endregion
    }
}
