using CookApps.gRPC.Universal;
using Grpc.Core;
using Tech.Hatchery.V2;
using Tech.Universal.V2;

namespace CookApps.gRPC
{
    public class GrpcExceptionHandler
    {
        //───────────────────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// * Client Only일때는 사용하지 않아도 됩니다.
        /// * 게임 서버로부터 Success가 아닌 StatusCode코드를 받으면 이 메서드가 트리거 됩니다. 각 에러코드 별로 알맞게 처리하세요.
        ///
        /// 계정 정책은 바뀔 소지가 있습니다. 프로젝트의 계정 정책에 따라 에러코드를 다르게 처리해야할 수 있습니다.
        /// 현재 적용된 정책은 1개의 uid에 애플,구글,페이스북 등이 멀티로 연결될 수 있습니다.
        /// 어떤 게임은 1개의 uid에 절대적인 플랫폼 1개만 연결될 수 있습니다.
        /// </summary>
        /// <param name="response">서버로부터 받은 응답</param>
        public static async void HandleServerException(CommonResponseData response)
        {
            //유니버설 에러코드
            switch ((UniversalResponseCode) response.StatusCode)
            {
                //네트워크 에러가 발생하여 gRPC stub에서 에러를 내려주면 내부로직에 의해 Unspecified 콜백이 오기 때문에 무시.
                case UniversalResponseCode.Unspecified: return;

                //에러팝업이 필요없음
                case UniversalResponseCode.InvalidUid:
                    break;

                //AuthInfo에 대해 UID가 일치하지 않음
                case UniversalResponseCode.AuthWrongUid:
                    break;

                //Add하고자 하는 AuthInfo가 이미 있음. 계정 선택하는 팝업 노출
                case UniversalResponseCode.AuthInfoExist:
                    return;

                // 계정 삭제 기능은 아직 지원하지 않습니다.
                // case UniversalResponseCode.AuthCannotDeleteAuthInfo:
                //     await DialogWindow.DisplayDialog("알림", "계정이 1개여서 계정을 더 이상 지울 수 없습니다.", "확인");
                //     return;

                // 계정 삭제 기능은 아직 지원하지 않습니다.
                // case UniversalResponseCode.AuthInfoNotExist:
                //     if (await DialogWindow.DisplayDialog("알림", "삭제하려는 계정이 없습니다.", "확인"))
                //     {
                //         await ValidAuthInfoAsync();
                //     }
                //     return;

                //탈퇴한 계정
                case UniversalResponseCode.AuthUnregistered:
                    UniversalGrpcManager.Instance.DeleteLocalData();
                    UniversalGrpcManager.Instance.ClearToken();
                    return;

                //두개 이상의 기기에서 중복 로그인 시도
                case UniversalResponseCode.AuthDuplicatedLogin:
                    break;

                case UniversalResponseCode.AuthIncludingNotRegisteredPlatform:
                    break;

                default:
                    break;
            }

            //게임서버 에러코드
            switch ((GameErrorCode) response.StatusCode)
            {
                case GameErrorCode.SetRankingDataFailed:
                    break;

                case GameErrorCode.GetRankingDataFailed:
                    break;

                default:
                    // TODO : 기본적인 처리
                    break;
            }
        }

        /// <summary>
        /// 인터넷 연결이 안됐거나, 기타 등등의 이유로 Grpc객체로부터 받은 에러입니다.
        /// </summary>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        /// <param name="methodName">에러가 발생한</param>
        internal static async void HandleGrpcException(StatusCode statusCode, string message, string methodName)
        {
            //TODO : 상황에 맞게 처리하는 로직을 추가하세요.
            switch (statusCode)
            {
                default:
                    // TODO : 기본적인 처리
                    break;
            }
        }

        internal static void HandleServerSuccess(string methodName)
        {
            //TODO : 서버와 프로시저 호출 후 성공적으로 응답을 받은 후의 콜백입니다. 예를 들어 OnHandleGrpcException를 통해 일반적인 인디케이터를 띄웠을 경우, 이 이벤트를 통해 인디케이터를 사라지게 하는데 사용할 수 있습니다.
        }
    }
}
