using System;
using System.Threading;
using CookApps.NetLite;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    [GrpcService(typeof(Tech.Hive.V1.CustomLobbyService.CustomLobbyServiceClient))]
    public partial class CustomLobbyService
    {
        /// <summary>
        /// 내 정보 조회
        /// </summary>
        public async UniTask<CustomLobbyGetMyPlayerDataResponse> GetMyPlayerDataAsync(CancellationToken cancellationToken = default)
        {
            CustomLobbyGetMyPlayerDataResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.GetMyPlayerDataAsync,
                new CustomLobbyGetMyPlayerDataRequest(),
                cancellationToken: cancellationToken
            );

            // PlayerDataModel 갱신
            if (resp != null && resp.IsSuccess && resp.Data != null)
            {
                ServerDataManager.Instance.PlayerData.SetPlayerData(resp.Data);
            }

            return resp;
        }

        /// <summary>
        /// 대표 캐릭터 설정
        /// </summary>
        public async UniTask<CustomLobbySetRepresentativeCharacterResponse> SetRepresentativeCharacterAsync(uint characterId, CancellationToken cancellationToken = default)
        {
            CustomLobbySetRepresentativeCharacterResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.SetRepresentativeCharacterAsync,
                new CustomLobbySetRepresentativeCharacterRequest { CharacterId = characterId.ToString() },
                cancellationToken: cancellationToken
            );

            // 성공 시 플레이어 데이터 다시 가져오기
            if (resp != null && resp.IsSuccess)
            {
                await GetMyPlayerDataAsync(cancellationToken);
            }

            return resp;
        }

        /// <summary>
        /// 기타 보상 수령
        /// </summary>
        public async UniTask<CustomLobbyClaimOtherRewardResponse> ClaimOtherRewardAsync(uint rewardId, CancellationToken cancellationToken = default)
        {
            CustomLobbyClaimOtherRewardResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ClaimOtherRewardAsync,
                new CustomLobbyClaimOtherRewardRequest { RewardId = rewardId },
                cancellationToken: cancellationToken
            );

            // 통화 변화 적용
            if (resp != null && resp.IsSuccess && resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }

        /// <summary>
        /// AP 동기화
        /// </summary>
        public async UniTask<CustomLobbySyncApResponse> SyncApAsync(CancellationToken cancellationToken = default)
        {
            CustomLobbySyncApResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.SyncApAsync,
                new CustomLobbySyncApRequest(),
                cancellationToken: cancellationToken
            );

            // 통화 변화 적용
            if (resp != null && resp.IsSuccess && resp.CurrencyDeltas != null && resp.CurrencyDeltas.Count > 0)
            {
                ServerDataManager.Instance.Inventory.ApplyCurrencyDeltas(resp.CurrencyDeltas);
            }

            return resp;
        }

        /// <summary>
        /// 플레이어 닉네임 변경
        /// </summary>
        public async UniTask<CustomLobbyChangeNicknameResponse> ChangeNicknameAsync(string nickname, CancellationToken cancellationToken = default)
        {
            CustomLobbyChangeNicknameResponse resp = await ExecuteWithCommonErrorCheck(
                ServiceClient.ChangeNicknameAsync,
                new CustomLobbyChangeNicknameRequest { Nickname = nickname },
                cancellationToken: cancellationToken
            );

            return resp;
        }

        /// <summary>
        /// 이벤트 구독 (Bidirectional Streaming)
        /// </summary>
        private AsyncDuplexStreamingCall<CustomLobbySubscribeEventRequest, CustomLobbySubscribeEventResponse> SubscribeEvent(CancellationToken cancellationToken = default)
        {
            return ServiceClient.SubscribeEvent(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 이벤트 구독 시작 및 이벤트 수신 처리
        /// </summary>
        public async UniTask SubscribeEventAsync(Action<BM013Event> onEventReceived, CancellationToken cancellationToken = default)
        {
            using var call = SubscribeEvent(cancellationToken);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // 응답 수신 태스크
            var readTask = ReadEventStreamAsync(call, onEventReceived, linkedCts.Token);

            // Ping 전송 (연결 유지)
            var pingTask = SendPingAsync(call, linkedCts.Token);

            // 둘 중 하나가 완료되면 다른 것도 취소
            await UniTask.WhenAny(readTask, pingTask);
            linkedCts.Cancel();

            // 스트림 정리
            try
            {
                await call.RequestStream.CompleteAsync();
            }
            catch
            {
                // 이미 닫힌 경우 무시
            }
        }

        private async UniTask ReadEventStreamAsync(
            AsyncDuplexStreamingCall<CustomLobbySubscribeEventRequest, CustomLobbySubscribeEventResponse> call,
            Action<BM013Event> onEventReceived,
            CancellationToken cancellationToken)
        {
            while (await call.ResponseStream.MoveNext(cancellationToken))
            {
                var response = call.ResponseStream.Current;
                if (response.IsSuccess && response.Event != BM013Event.Unspecified)
                {
                    onEventReceived?.Invoke(response.Event);
                }
            }
        }

        private async UniTask SendPingAsync(
            AsyncDuplexStreamingCall<CustomLobbySubscribeEventRequest, CustomLobbySubscribeEventResponse> call,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await call.RequestStream.WriteAsync(new CustomLobbySubscribeEventRequest(), cancellationToken); 
                await UniTask.Delay(TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);
            }
        }
    }
}
