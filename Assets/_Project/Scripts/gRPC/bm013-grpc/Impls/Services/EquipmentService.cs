using System.Threading;
using CookApps.NetLite;
using Tech.Hive.V1;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.EquipmentService.EquipmentServiceClient))]
    public partial class EquipmentService
    {
        // ===== 스텔룸 (Stellum) =====

        /// <summary>
        /// 스텔룸 목록 가져오기
        /// </summary>
        public async UniTask<EquipmentListStellumResponse> ListStellumAsync(CancellationToken cancellationToken = default)
        {
            EquipmentListStellumResponse resp = await ExecuteAsync(
                ServiceClient.ListStellumAsync,
                new EquipmentListStellumRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 스텔룸 제작
        /// </summary>
        public async UniTask<EquipmentForgeStellumResponse> ForgeStellumAsync(string stellumInstanceId, CancellationToken cancellationToken = default)
        {
            EquipmentForgeStellumResponse resp = await ExecuteAsync(
                ServiceClient.ForgeStellumAsync,
                new EquipmentForgeStellumRequest { InstanceId = stellumInstanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 스텔룸 승급
        /// </summary>
        public async UniTask<EquipmentAscendStellumResponse> AscendStellumAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            EquipmentAscendStellumResponse resp = await ExecuteAsync(
                ServiceClient.AscendStellumAsync,
                new EquipmentAscendStellumRequest { InstanceId = instanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        // ===== 성유물 (Relic) =====

        /// <summary>
        /// 성유물 목록 가져오기
        /// </summary>
        public async UniTask<EquipmentListRelicResponse> ListRelicAsync(CancellationToken cancellationToken = default)
        {
            EquipmentListRelicResponse resp = await ExecuteAsync(
                ServiceClient.ListRelicAsync,
                new EquipmentListRelicRequest(),
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 성유물 강화
        /// </summary>
        public async UniTask<EquipmentEnhanceRelicResponse> EnhanceRelicAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            EquipmentEnhanceRelicResponse resp = await ExecuteAsync(
                ServiceClient.EnhanceRelicAsync,
                new EquipmentEnhanceRelicRequest { InstanceId = instanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 성유물 합성
        /// </summary>
        public async UniTask<EquipmentSynthesizeRelicResponse> SynthesizeRelicAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            EquipmentSynthesizeRelicResponse resp = await ExecuteAsync(
                ServiceClient.SynthesizeRelicAsync,
                new EquipmentSynthesizeRelicRequest { MaterialInstanceIds = { instanceId } },
                cancellationToken: cancellationToken
            );
            return resp;
        }

        /// <summary>
        /// 성유물 재조정
        /// </summary>
        public async UniTask<EquipmentRerollRelicResponse> RerollRelicAsync(string instanceId, CancellationToken cancellationToken = default)
        {
            EquipmentRerollRelicResponse resp = await ExecuteAsync(
                ServiceClient.RerollRelicAsync,
                new EquipmentRerollRelicRequest { InstanceId = instanceId },
                cancellationToken: cancellationToken
            );
            return resp;
        }
    }
}
