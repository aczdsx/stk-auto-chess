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
        public async UniTask<ElpisGetInfoResponse> GetInfoAsync(CancellationToken cancellationToken = default)
        {
            ElpisGetInfoResponse resp = await ExecuteAsync(
                ServiceClient.GetInfoAsync,
                new ElpisGetInfoRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 함선 확장 (코어 사용)
        /// </summary>
        public async UniTask<ElpisExpandResponse> ExpandElpisAsync(CancellationToken cancellationToken = default)
        {
            ElpisExpandResponse resp = await ExecuteAsync(
                ServiceClient.ExpandElpisAsync,
                new ElpisExpandRequest(),
                cancellationToken: cancellationToken
            );
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
            return resp;
        }

        /// <summary>
        /// 전술 연구소 - 설치물 연구
        /// </summary>
        public async UniTask<ElpisResearchTacticResponse> ResearchTacticAsync(TacticResearchType researchType, uint levels, CancellationToken cancellationToken = default)
        {
            ElpisResearchTacticResponse resp = await ExecuteAsync(
                ServiceClient.ResearchTacticAsync,
                new ElpisResearchTacticRequest { ResearchType = researchType, Levels = levels },
                cancellationToken: cancellationToken
            );
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
            return resp;
        }
    }
}
