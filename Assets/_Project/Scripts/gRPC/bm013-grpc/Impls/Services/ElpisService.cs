using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

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
            ElpisGetResponse resp = await ExecuteAsync(
                ServiceClient.GetAsync,
                new ElpisGetRequest(),
                cancellationToken: cancellationToken
            );

            if (false)
            {
                // 서버 응답으로 로컬 데이터 갱신
                if (resp != null && resp.Status.Code == 0 && resp.Elpis != null)
                {
                    ServerDataManager.Instance.Elpis.SetElpisData(
                        resp.Elpis,
                        ServerDataManager.Instance.Elpis.Version + 1
                    );
                }
            }
            else // dummy
            {
                ServerDataManager.Instance.Elpis.SetElpisData(
                    new ElpisData()
                    {
                        CoreResearches = { },
                        Facilities = {  },
                        Simulation = { }
                    },
                    ServerDataManager.Instance.Elpis.Version + 1
                );
            }

            return resp;
        }

        /// <summary>
        /// 시설 건설
        /// </summary>
        public async UniTask<ElpisBuildFacilityResponse> BuildFacilityAsync(ElpisFacilityType facilityType, int gridX, int gridY, CancellationToken cancellationToken = default)
        {
            ElpisBuildFacilityResponse resp = await ExecuteAsync(
                ServiceClient.BuildFacilityAsync,
                new ElpisBuildFacilityRequest { FacilityType = facilityType, GridX = gridX, GridY = gridY },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
            {
                // 시설 업데이트
                if (resp.Facility != null)
                {
                    ServerDataManager.Instance.Elpis.UpdateFacility(resp.Facility);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Wallet.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 시설 업그레이드
        /// </summary>
        public async UniTask<ElpisUpgradeFacilityResponse> UpgradeFacilityAsync(string facilityInstanceId, CancellationToken cancellationToken = default)
        {
            ElpisUpgradeFacilityResponse resp = await ExecuteAsync(
                ServiceClient.UpgradeFacilityAsync,
                new ElpisUpgradeFacilityRequest { FacilityInstanceId = facilityInstanceId },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
            {
                // 시설 업데이트
                if (resp.Facility != null)
                {
                    ServerDataManager.Instance.Elpis.UpdateFacility(resp.Facility);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Wallet.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 코어 연구소 - 연구 진행
        /// </summary>
        public async UniTask<ElpisResearchCoreResponse> ResearchCoreAsync(CoreResearchType researchType, uint levels, CancellationToken cancellationToken = default)
        {
            ElpisResearchCoreResponse resp = await ExecuteAsync(
                ServiceClient.ResearchCoreAsync,
                new ElpisResearchCoreRequest { ResearchType = researchType, Levels = levels },
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
            {
                // 코어 연구 업데이트
                if (resp.Research != null)
                {
                    ServerDataManager.Instance.Elpis.UpdateCoreResearch(resp.Research);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Wallet.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }

        /// <summary>
        /// 전투 시뮬레이션 - 보상 수령
        /// </summary>
        public async UniTask<ElpisClaimSimulationRewardResponse> ClaimSimulationRewardAsync(CancellationToken cancellationToken = default)
        {
            ElpisClaimSimulationRewardResponse resp = await ExecuteAsync(
                ServiceClient.ClaimSimulationRewardAsync,
                new ElpisClaimSimulationRewardRequest(),
                cancellationToken: cancellationToken
            );

            // 서버 응답으로 로컬 데이터 갱신
            if (resp != null && resp.Status.Code == 0)
            {
                // 시뮬레이션 데이터 업데이트
                if (resp.Simulation != null)
                {
                    ServerDataManager.Instance.Elpis.UpdateSimulation(resp.Simulation);
                }

                // 통화 변화 적용
                if (resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
                {
                    ServerDataManager.Instance.Wallet.ApplyCurrencyDeltas(resp.CurrencyDeltas);
                }
            }

            return resp;
        }
    }
}
