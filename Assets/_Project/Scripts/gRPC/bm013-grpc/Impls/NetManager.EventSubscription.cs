using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Grpc.Core;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public partial class NetManager
    {
        private CancellationTokenSource _eventSubscriptionCts;

        /// <summary>
        /// 이벤트 구독 시작 (InitializeAsync 완료 후 호출)
        /// </summary>
        public void StartEventSubscription()
        {
            StopEventSubscription();
            _eventSubscriptionCts = new CancellationTokenSource();
            RunEventSubscriptionLoop(_eventSubscriptionCts.Token).Forget();
        }

        /// <summary>
        /// 이벤트 구독 중지
        /// </summary>
        public void StopEventSubscription()
        {
            _eventSubscriptionCts?.Cancel();
            _eventSubscriptionCts?.Dispose();
            _eventSubscriptionCts = null;
        }

        private async UniTaskVoid RunEventSubscriptionLoop(CancellationToken cancellationToken)
        {
            const int MaxRetryDelay = 30;
            int retryDelay = 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Debug.Log("[EventSubscription] Connecting...");
                    await CustomLobby.SubscribeEventAsync(OnServerEventReceived, cancellationToken);

                    // 정상 종료 시 (서버가 스트림 종료)
                    retryDelay = 1;
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("[EventSubscription] Cancelled");
                    break;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
                {
                    Debug.Log("[EventSubscription] Cancelled by RPC");
                    break;
                }
                catch (RpcException ex)
                {
                    Debug.LogWarning($"[EventSubscription] RpcException: {ex.StatusCode}, retrying in {retryDelay}s");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventSubscription] Error: {ex.Message}");
                }

                // 재연결 대기 (Exponential backoff)
                await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken: cancellationToken);
                retryDelay = Math.Min(retryDelay * 2, MaxRetryDelay);
            }
        }

        private void OnServerEventReceived(BM013Event evt)
        {
            Debug.Log($"[EventSubscription] Received: {evt}");

            // 이벤트 타입에 따라 데이터 갱신
            switch (evt)
            {
                case BM013Event.Bm013GuideMissionClear:
                    GuideMission.GetAsync().Forget();
                    break;
                case BM013Event.Bm013DailyQuestClear:
                    Event.ListAsync().Forget();
                    break;
            }
        }
    }
}
