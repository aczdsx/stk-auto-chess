using System;
using System.Collections.Generic;
using R3;
using Tech.Hive.V1;
using UnityEngine.Pool;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// Elpis 데이터 브릿지
    /// ServerDataManager와 UI 사이의 중간 레이어
    /// UI가 직접 데이터 모델을 접근하지 않고 브릿지를 통해 접근
    /// </summary>
    public class ElpisDataBridge : DataBridgeBase<ElpisModel>
    {
        // R3 이벤트
        public readonly Subject<Unit> OnElpisChanged = new();
        public readonly Subject<uint> OnLevelChanged = new();
        public readonly Subject<uint> OnExpansionLevelChanged = new();
        public readonly Subject<ElpisFacility> OnFacilityChanged = new();
        public readonly Subject<CoreResearch> OnCoreResearchChanged = new();
        public readonly Subject<SimulationData> OnSimulationChanged = new();

        public ElpisDataBridge()
            : base(ServerDataManager.Instance.Elpis, ElpisModel.CATEGORY_KEY)
        {
        }

        /// <summary>
        /// 모델 이벤트 구독
        /// </summary>
        protected override void SubscribeModelEvents()
        {
            Model.OnLevelChanged.Subscribe(this, (level, self) =>
            {
                self.OnLevelChanged.OnNext(level);
                self.OnElpisChanged.OnNext(Unit.Default);
            }).AddTo(ref disposableBag);

            Model.OnExpansionLevelChanged.Subscribe(this, (level, self) =>
            {
                self.OnExpansionLevelChanged.OnNext(level);
                self.OnElpisChanged.OnNext(Unit.Default);
            }).AddTo(ref disposableBag);

            Model.OnFacilityAdded.Subscribe(this, (facility, self) => self.OnFacilityChanged.OnNext(facility)).AddTo(ref disposableBag);
            Model.OnFacilityUpdated.Subscribe(this, (facility, self) => self.OnFacilityChanged.OnNext(facility)).AddTo(ref disposableBag);
            Model.OnCoreResearchUpdated.Subscribe(this, (research, self) => self.OnCoreResearchChanged.OnNext(research)).AddTo(ref disposableBag);
            Model.OnSimulationUpdated.Subscribe(this, (simulation, self) => self.OnSimulationChanged.OnNext(simulation)).AddTo(ref disposableBag);
        }

        /// <summary>
        /// 모델 변경 감지 (전체 갱신)
        /// </summary>
        protected override void OnModelChanged()
        {
            OnElpisChanged.OnNext(Unit.Default);
        }

        #region 시설 관련

        /// <summary>
        /// 특정 시설 가져오기
        /// </summary>
        public ElpisFacility GetFacility(string instanceId)
        {
            return Model?.GetFacility(instanceId);
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
        public bool HasFacility(string instanceId)
        {
            return Model?.HasFacility(instanceId) ?? false;
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

        /// <summary>
        /// 코어 연구 가져오기
        /// </summary>
        public CoreResearch GetCoreResearch(CoreResearchType researchType)
        {
            return Model?.GetCoreResearch(researchType);
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
        public uint GetCoreResearchLevel(CoreResearchType researchType)
        {
            var research = GetCoreResearch(researchType);
            return research?.Level ?? 0;
        }

        /// <summary>
        /// 코어 연구가 최대 레벨인지 확인
        /// </summary>
        public bool IsCoreResearchMaxLevel(CoreResearchType researchType)
        {
            var research = GetCoreResearch(researchType);
            if (research == null) return false;
            return research.Level >= research.MaxLevel;
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
        public bool IsCoreResearchLevelAtLeast(CoreResearchType researchType, uint minLevel)
        {
            return GetCoreResearchLevel(researchType) >= minLevel;
        }

        #endregion
    }
}
