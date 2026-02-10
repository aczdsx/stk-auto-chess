using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using R3;
using Tech.Hive.V1;

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
    ///
    /// [변경 이력]
    /// - ElpisDataBridge 제거: 1:1 래퍼 삭제, GetFacilityLevel을 Model로 이동
    /// - 삭제된 미사용 헬퍼: IsBuildedFacilityExists, IsFacilityMaxLevel, HasFacilityType,
    ///   GetFacilitiesByMinLevel, GetCoreResearchLevel, IsSimulationMaxLevel, IsBoxesFull,
    ///   IsFacilityLevelAtLeast, IsCoreResearchLevelAtLeast
    ///
    /// [파일 구성]
    /// - ElpisModel.cs: 필드, 이벤트, Reset, Validate, 내부용 메서드
    /// - ElpisModel.Facility.cs: 시설 조회, 시뮬레이션 조회
    /// - ElpisModel.CoreResearch.cs: 코어 연구, 디멘션 랩 캐시, 뱃지 갱신
    /// </summary>
    public partial class ElpisModel
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

        // InventoryModel 구독
        private IDisposable _inventorySubscription;

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            _elpisData = new ElpisData();
            _facilitiesCache.Clear();
            _coreResearchCache.Clear();
            _cachedElpisDimensionLabs.Clear();
            _inventorySubscription?.Dispose();
            _inventorySubscription = null;
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

            // InventoryModel 구독 (재화 변경 시 Badge 갱신)
            _inventorySubscription ??= ServerDataManager.Instance.Inventory.OnCurrencyChanged
                .Subscribe(this, (_, self) => self.RefreshCoreResearchBadges());

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
            RefreshCoreResearchBadges();
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
