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
        /// 엘피스 정보 가져오기
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

        // /// <summary>
        // /// 코어 연구 목록 가져오기
        // /// </summary>
        // public async UniTask<ElpisListCoreResearchResponse> ListCoreResearchAsync(CancellationToken cancellationToken = default)
        // {
        //     ElpisListCoreResearchResponse resp = await ExecuteAsync(
        //         ServiceClient.ListCoreResearchAsync,
        //         new ElpisListCoreResearchRequest(),
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }
        //
        // /// <summary>
        // /// 코어 연구 시작
        // /// </summary>
        // public async UniTask<ElpisStartCoreResearchResponse> StartCoreResearchAsync(uint researchId, CancellationToken cancellationToken = default)
        // {
        //     ElpisStartCoreResearchResponse resp = await ExecuteAsync(
        //         ServiceClient.StartCoreResearchAsync,
        //         new ElpisStartCoreResearchRequest { ResearchId = researchId },
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }
        //
        // /// <summary>
        // /// 전술 연구 목록 가져오기
        // /// </summary>
        // public async UniTask<ElpisListTacticResearchResponse> ListTacticResearchAsync(CancellationToken cancellationToken = default)
        // {
        //     ElpisListTacticResearchResponse resp = await ExecuteAsync(
        //         ServiceClient.ListTacticResearchAsync,
        //         new ElpisListTacticResearchRequest(),
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }
        //
        // /// <summary>
        // /// 전술 연구 시작
        // /// </summary>
        // public async UniTask<ElpisStartTacticResearchResponse> StartTacticResearchAsync(uint researchId, CancellationToken cancellationToken = default)
        // {
        //     ElpisStartTacticResearchResponse resp = await ExecuteAsync(
        //         ServiceClient.StartTacticResearchAsync,
        //         new ElpisStartTacticResearchRequest { ResearchId = researchId },
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }
        //
        // /// <summary>
        // /// 시설 목록 가져오기
        // /// </summary>
        // public async UniTask<ElpisListFacilityResponse> ListFacilityAsync(CancellationToken cancellationToken = default)
        // {
        //     ElpisListFacilityResponse resp = await ExecuteAsync(
        //         ServiceClient.ListFacilityAsync,
        //         new ElpisListFacilityRequest(),
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }

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

        // /// <summary>
        // /// 시뮬레이션 시작
        // /// </summary>
        // public async UniTask<ElpisStartSimulationResponse> StartSimulationAsync(uint simulationId, CancellationToken cancellationToken = default)
        // {
        //     ElpisStartSimulationResponse resp = await ExecuteAsync(
        //         ServiceClient.StartSimulationAsync,
        //         new ElpisStartSimulationRequest { SimulationId = simulationId },
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }
        //
        // /// <summary>
        // /// 시뮬레이션 종료
        // /// </summary>
        // public async UniTask<ElpisEndSimulationResponse> EndSimulationAsync(string sessionId, bool isWin, CancellationToken cancellationToken = default)
        // {
        //     ElpisEndSimulationResponse resp = await ExecuteAsync(
        //         ServiceClient.EndSimulationAsync,
        //         new ElpisEndSimulationRequest { SessionId = sessionId, IsWin = isWin },
        //         cancellationToken: cancellationToken
        //     );
        //     return resp;
        // }
    }
}
