using System.Collections.Generic;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public partial class ElpisModel
    {
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

        /// <summary>
        /// 특정 타입의 시설 레벨
        /// </summary>
        public uint GetFacilityLevel(ElpisFacilityType facilityType)
        {
            var facility = GetFacilityByType(facilityType);
            return facility?.Level ?? 0;
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
    }
}
