using System;
using CookApps.gRPC;
using Grpc.Core;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public class GrpcExceptionHandler
    {
        //───────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// 게임 서버로부터 Success가 아닌 Code를 받으면 이 메서드가 트리거 됩니다. 각 에러코드 별로 알맞게 처리하세요.
        ///
        /// 계정 정책은 바뀔 소지가 있습니다. 프로젝트의 계정 정책에 따라 에러코드를 다르게 처리해야할 수 있습니다.
        /// 현재 적용된 정책은 1개의 uid에 애플,구글,페이스북 등이 멀티로 연결될 수 있습니다.
        /// 어떤 게임은 1개의 uid에 절대적인 플랫폼 1개만 연결될 수 있습니다.
        /// </summary>
        /// <param name="responseStatus">서버로부터 받은 응답</param>
        /// <param name="dontCallMe">GrpcFailAction을 전달할 콜백</param>
        public static async void HandleServerException(ResponseStatus responseStatus, Action<GrpcFailAction> dontCallMe)
        {
            using var callback = new ActionWrapper(dontCallMe);

            //GrpcConsts.ResponseStatusFailByClient은 서버에서 사용하지 않거나, 클라이언트에서 네트워크에 접속할 수 없을 때 ResponseStatusExceptionGenerator에서 사용하는 값입니다.
            if (responseStatus.Code == GrpcConsts.ResponseStatusFailByClient)
            {
                callback.Invoke(GrpcFailAction.Skip);
                return;
            }

            // TODO-tech : 적절한 처리.
            switch ((PlayerDataResponseStatus)responseStatus.Code)
            {
                case PlayerDataResponseStatus.PlayerDataNotFound:
                    callback.Invoke(GrpcFailAction.Skip);
                    // await PlayerDataManager.Instance.InitSetPlayerDatas();  // 플레이어 데이터가 아직 존재하지 않습니다. PlayerData를 먼저 서버에 저장해주세요.
                    break;
            }

            switch ((AuthResponseStatus)responseStatus.Code)
            {
                case AuthResponseStatus.InvalidAuthPlatform:
                    break;
                case AuthResponseStatus.Banned:
                    break;
                case AuthResponseStatus.CannotDeleteAuth:
                    break;
                case AuthResponseStatus.InactiveUser:
                    // TODO : 탈퇴한 유저의 경우 처리
                    // if (await DialogWindow.DisplayDialog("알림", "이미 탈퇴한 유저입니다.", "확인"))
                    // {
                    //     callback.Invoke(GrpcFailAction.Skip);
                    // }
                    break;
                case AuthResponseStatus.NotFoundUser:
                    break;
            }

            switch ((ChatResponseStatus)responseStatus.Code)
            {
                case ChatResponseStatus.ChatServerNotConfigured:
                    break;
            }

            switch ((HardCurrencyResponseStatus)responseStatus.Code)
            {
                case HardCurrencyResponseStatus.HardCurrencyInvalidError:
                    break;
                case HardCurrencyResponseStatus.HardCurrencyNotFound:
                    break;
            }

            switch ((IapResponseStatus)responseStatus.Code)
            {
                case IapResponseStatus.IapError:
                    break;
                case IapResponseStatus.IapInvalidError:
                    break;
                case IapResponseStatus.DuplicateOrderId:
                    break;
                case IapResponseStatus.InvalidOrderId:
                    break;
            }

            switch ((PlayerResponseStatus)responseStatus.Code)
            {
                case PlayerResponseStatus.PlayerUidIsNull:
                    break;
                case PlayerResponseStatus.PlayerNotFound:
                    break;
                case PlayerResponseStatus.PlayerAlreadyDeleted:
                    break;
                case PlayerResponseStatus.PlayerServerIdNotMatched:
                    break;
                case PlayerResponseStatus.PlayerNicknameNotChanged:
                    // TODO : 닉네임 변경이 되지 않은 경우 처리
                    // if (await DialogWindow.DisplayDialog("알림", "닉네임이 변경되지 않았습니다.", "확인"))
                    // {
                    //     callback.Invoke(GrpcFailAction.Skip);
                    // }
                    break;
                case PlayerResponseStatus.PlayerNicknameAlreadyExist:
                    // TODO : 이미 존재하는 닉네임인 경우 처리
                    // if (await DialogWindow.DisplayDialog("알림", "이미 존재하는 닉네임입니다.", "확인"))
                    // {
                    //     callback.Invoke(GrpcFailAction.Skip);
                    // }
                    break;
            }

            switch ((PlayerDataResponseStatus)responseStatus.Code)
            {
                case PlayerDataResponseStatus.PlayerDataNotFound:
                    break;
            }

            switch ((PostResponseStatus)responseStatus.Code)
            {
                case PostResponseStatus.PostAlreadyRead:
                    break;
            }

            switch ((ServerResponseStatus)responseStatus.Code)
            {
                case ServerResponseStatus.ServerListEmpty:
                    break;
                case ServerResponseStatus.ServerNotJoinable:
                    break;
                case ServerResponseStatus.ServerPlayerNotFound:
                    break;
                case ServerResponseStatus.ServerPlayerDeleted:
                    break;
            }

            switch ((ServerRankingResponseStatus)responseStatus.Code)
            {
                case ServerRankingResponseStatus.ServerRankNotFound:
                    break;
                case ServerRankingResponseStatus.NotFoundPlayer:
                    break;
                case ServerRankingResponseStatus.ServerRankReadonly:
                    break;
            }

            switch ((SpecResponseStatus)responseStatus.Code)
            {
                case SpecResponseStatus.SpecNotFound:
                    break;
            }

            switch ((CurrencyResponseStatus)responseStatus.Code)
            {
                case CurrencyResponseStatus.CurrencyInvalidError:
                    break;
            }

            switch ((PremiumCurrencyResponseStatus)responseStatus.Code)
            {
                case PremiumCurrencyResponseStatus.PremiumCurrencyInvalidError:
                    break;
                case PremiumCurrencyResponseStatus.PremiumCurrencyNotFound:
                    break;
            }

            switch ((HardCurrencyResponseStatus)responseStatus.Code)
            {
                case HardCurrencyResponseStatus.HardCurrencyInvalidError:
                    break;
                case HardCurrencyResponseStatus.HardCurrencyNotFound:
                    break;
            }

            //어떠한 case에도 속하지 않는다면 기본처리
            if (callback.IsInvoked == false)
            {
                //기본적인 처리
                var isRetry = true; // TODO : 상황에 맞게 처리하는 로직을 추가한 후 완료되면 callback을 호출하세요.
                if (isRetry)
                    callback.Invoke(GrpcFailAction.Retry);
                else
                    callback.Invoke(GrpcFailAction.Skip);
            }
        }

        /// <summary>
        /// 1. 인터넷 연결이 안됐거나, 기타 등등의 이유로 Grpc객체에서 반환해준 에러입니다.
        /// 2. 몇몇 케이스의 경우 게임서버에서 반환해준 에러입니다. 이 경우는 아래 case를 확인하세요.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        /// <param name="methodName">에러가 발생한 메서드</param>
        /// <param name="dontCallMe">GrpcFailAction을 전달할 콜백</param>
        internal static async void HandleGrpcException(StatusCode statusCode, string message, string methodName, Action<GrpcFailAction> dontCallMe)
        {
            using var callback = new ActionWrapper(dontCallMe);

            //TODO : 상황에 맞게 처리하는 로직을 추가하세요.
            switch (statusCode)
            {
                // 1. metadata에 sessionId가 없음.
                // 2. sessionId에 해당하는 데이터가 없거나 이상하거나 Expire됨.
                // 3. 로그인 당시 사용한 deviceId, authPlatform 형태가 metadata와 다른 경우
                case StatusCode.Unauthenticated:
                // 1. 다른 기기로 로그인할 경우 이전 기기 오류 발생
                // 2. SessionId 탈취를 통해 로그인 시도할 경우 오류 발생
                case StatusCode.PermissionDenied:
                // 1. Server에 Join하지 않고 플레이어 데이터 접근한 경우
                case StatusCode.NotFound:
                // Metadata 형식이 다름
                case StatusCode.InvalidArgument:
                    // TODO : 상황에 맞게 처리하는 로직을 추가한 후 완료되면 callback을 호출하세요.
                    callback.Invoke(GrpcFailAction.CancelAll);
                    MoveToSplash();
                    break;
            }

            //어떠한 case에도 속하지 않는다면 기본처리
            if (callback.IsInvoked == false)
            {
                var isRetry = true; // TODO : 상황에 맞게 처리하는 로직을 추가한 후 완료되면 callback을 호출하세요.

                //확인 버튼 후 다음 처리
                if (isRetry)
                {
                    callback.Invoke(GrpcFailAction.Retry);
                }
                else
                {
                    MoveToSplash();

                    callback.Invoke(GrpcFailAction.CancelAll);
                }
            }
        }

        internal static void HandleSuccess(string methodName)
        {
            // TODO : 서버와 프로시저 호출 후 성공적으로 응답을 받은 후의 콜백입니다. 예를 들어 OnHandleGrpcException를 통해 일반적인 인디케이터를 띄웠을 경우, 이 이벤트를 통해 인디케이터를 사라지게 하는데 사용할 수 있습니다.
        }

        private static void MoveToSplash()
        {
            GrpcManager.Instance.Shutdown();
            // TODO : 초기화면으로 보내는 로직을 추가하세요.
        }
    }
}