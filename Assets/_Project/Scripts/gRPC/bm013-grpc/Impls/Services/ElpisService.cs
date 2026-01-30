using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.ElpisService.ElpisServiceClient))]
    public partial class ElpisService
    {
        /// <summary>
        /// 함선 정보 가져오기
        /// </summary>
        public async UniTask<ElpisGetResponse> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            ElpisGetResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetAsync,
                new ElpisGetRequest(),
                cancellationToken: cancellationToken
            );

            // resp가 null이면 공통 에러 처리됨 (HandleCommonError가 true 리턴)
            if (resp == null)
            {
                return null;
            }

            // IsSuccess가 false면 HandleCommonError가 false를 리턴한 경우
            // 커스텀 에러 처리 가능
            if (!resp.IsSuccess)
            {
                // 여기에 특정 에러 코드에 대한 커스텀 처리 추가
                return resp;
            }

            // 성공 시 비즈니스 로직
            ServerDataManager.Instance.Elpis.SetElpisData(resp.Elpis);
            return resp;
        }

        /// <summary>
        /// 시설 건설
        /// </summary>
        public async UniTask<ElpisBuildFacilityResponse> BuildFacilityAsync(int buildId, int gridX, int gridY, CancellationToken cancellationToken = default)
        {
            ElpisBuildFacilityResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.BuildFacilityAsync,
                new ElpisBuildFacilityRequest { BuildId = (uint)buildId, GridX = gridX, GridY = gridY },
                cancellationToken: cancellationToken
            );

            if (resp == null) return null;

            if (!resp.IsSuccess)
            {
                // 커스텀 에러 처리
                return resp;
            }

            // 시설 업데이트
            if (resp.Facility != null)
            {
                ServerDataManager.Instance.Elpis.UpdateFacility(resp.Facility);
            }

            // 통화 변화 적용
            if (resp.CurrencyDeltas is { Count: > 0 })
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }

        /// <summary>
        /// 시설 건설 완료
        /// </summary>
        public async UniTask<ElpisFinishBuildingFacilityResponse> FinishBuildingFacilityAsync(int buildId, CancellationToken cancellationToken = default)
        {
            ElpisFinishBuildingFacilityResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.FinishBuildingFacilityAsync,
                new ElpisFinishBuildingFacilityRequest { BuildId = (uint)buildId },
                cancellationToken: cancellationToken
            );

            if (resp == null) return null;

            if (!resp.IsSuccess)
            {
                // 커스텀 에러 처리
                return resp;
            }

            // 시설 업데이트
            if (resp.Facility != null)
            {
                ServerDataManager.Instance.Elpis.UpdateFacility(resp.Facility);
            }

            return resp;
        }

        /// <summary>
        /// 시설 업그레이드
        /// </summary>
        public async UniTask<ElpisUpgradeFacilityResponse> UpgradeFacilityAsync(int buildId, CancellationToken cancellationToken = default)
        {
            ElpisUpgradeFacilityResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.UpgradeFacilityAsync,
                new ElpisUpgradeFacilityRequest { BuildId = (uint)buildId },
                cancellationToken: cancellationToken
            );

            if (resp == null) return null;

            if (!resp.IsSuccess)
            {
                // 커스텀 에러 처리
                return resp;
            }

            // 시설 업데이트
            if (resp.Facility != null)
            {
                ServerDataManager.Instance.Elpis.UpdateFacility(resp.Facility);
            }

            // 통화 변화 적용
            if (resp.CurrencyDeltas is { Count: > 0 })
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }

        /// <summary>
        /// 시설 업그레이드 완료
        /// </summary>
        public async UniTask<ElpisFinishUpgradingFacilityResponse> FinishUpgradingFacilityAsync(int buildId, CancellationToken cancellationToken = default)
        {
            ElpisFinishUpgradingFacilityResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.FinishUpgradingFacilityAsync,
                new ElpisFinishUpgradingFacilityRequest { BuildId = (uint)buildId },
                cancellationToken: cancellationToken
            );

            if (resp == null) return null;

            if (!resp.IsSuccess)
            {
                // 커스텀 에러 처리
                return resp;
            }

            // 시설 업데이트
            if (resp.Facility != null)
            {
                ServerDataManager.Instance.Elpis.UpdateFacility(resp.Facility);
            }

            return resp;
        }

        /// <summary>
        /// 코어 연구소 - 연구 진행
        /// </summary>
        public async UniTask<ElpisResearchCoreResponse> ResearchCoreAsync(uint groupId, uint level, CancellationToken cancellationToken = default)
        {
            ElpisResearchCoreResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ResearchCoreAsync,
                new ElpisResearchCoreRequest { UpgradeGroupId = groupId, Level = level },
                cancellationToken: cancellationToken
            );

            if (resp == null) return null;

            if (!resp.IsSuccess)
            {
                // 커스텀 에러 처리
                return resp;
            }

            // 코어 연구 업데이트
            if (resp.Research != null)
            {
                ServerDataManager.Instance.Elpis.UpdateCoreResearch(resp.Research);
            }

            // 통화 변화 적용
            if (resp.CurrencyDeltas is { Count: > 0 })
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }

        /// <summary>
        /// 전투 시뮬레이션 - 보상 수령
        /// </summary>
        public async UniTask<ElpisClaimSimulationRewardResponse> ClaimSimulationRewardAsync(CancellationToken cancellationToken = default)
        {
            ElpisClaimSimulationRewardResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClaimSimulationRewardAsync,
                new ElpisClaimSimulationRewardRequest(),
                cancellationToken: cancellationToken
            );

            if (resp == null) return null;

            if (!resp.IsSuccess)
            {
                // 커스텀 에러 처리
                return resp;
            }

            // 시뮬레이션 데이터 업데이트
            if (resp.Simulation != null)
            {
                ServerDataManager.Instance.Elpis.UpdateSimulation(resp.Simulation);
            }

            // 통화 변화 적용
            if (resp.CurrencyDeltas is { Count: > 0 })
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }
            return resp;
        }
    }
}
