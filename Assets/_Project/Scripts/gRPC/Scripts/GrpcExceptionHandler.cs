using Com.Cookapps.Tech;
using Grpc.Core;

public class GrpcExceptionHandler
{
    //--------------------------------------------------------------------------------//
    //-----------------------------------FIELD----------------------------------------//
    //--------------------------------------------------------------------------------//
    //------------------- Inspector ------------------//

    //------------------- public ------------------//

    //------------------- protected ------------------//

    //------------------- private ------------------//

    //--------------------------------------------------------------------------------//
    //------------------------------------PROPERTY------------------------------------//
    //--------------------------------------------------------------------------------//

    //--------------------------------------------------------------------------------//
    //------------------------------------METHOD--------------------------------------//
    //--------------------------------------------------------------------------------//
    //───────────────────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 게임 서버로부터 Success가 아닌 StatusCode코드를 받으면 이 메서드가 트리거 됩니다. 각 에러코드 별로 알맞게 처리하세요.
    /// </summary>
    /// <param name="response"></param>
    public static async void HandleServerException(CommonResponseData response)
    {
        switch ((UniversalResponseCode) response.StatusCode)
        {
            case UniversalResponseCode.AuthWrongUid:
                //TODO : ValidAuthInfoAsync()를 호출해서 올바른 플랫폼 로그인 정보를 받으세요.
                // ValidAuthInfoAsync();
                ShowCommonErrorPopup(response.StatusCode);
                break;

            case UniversalResponseCode.AuthInfoExist:
                //로그인 시도한 플랫폼이 로그인되어 있다면
                // if (IsLoggedIn(TriedLoginAuth.AuthPlatformTriedLogin))
                // {
                // WithServerExample.Instance.Lobby.ShowSelectAccountPopup();
                // }
                // else
                // {
                //     AddAuthInfoToLocal(TriedLoginAuth.AuthPlatformTriedLogin, TriedLoginAuth.AuthIdTriedLogin);
                // }
                return;

            case UniversalResponseCode.AuthInfoNotExist:
            case UniversalResponseCode.AuthCannotDeleteAuthInfo:
                // PopupEditorUtil.DisplayDialog("알림", "로그아웃을 할 수 없습니다.", "확인");
                //TODO : ValidAuthInfoAsync() 호출해서 로그인 정보를 다시 받아오는게 좋지 않음?
                return;

            //탈퇴한 계정
            case UniversalResponseCode.AuthUnregistered:
                // DeleteLocalData();
                // if (PopupEditorUtil.DisplayDialog("알림", "탈퇴한 계정입니다. 초기화면으로 돌아갑니다.", "확인"))
                // {
                // WithServerExample.Instance.Reconnect();
                // }

                return;

            default:
                ShowCommonErrorPopup(response.StatusCode);
                break;
        }
    }

    private static void ShowCommonErrorPopup(int statusCode)
    {
        // PopupEditorUtil.DisplayDialog("알림", $"네트워크 에러({statusCode})가 발생했습니다. 다시 시도해주세요.", "확인");
    }

    /// <summary>
    /// 인터넷 연결이 안됐거나, 기타 등등의 이유로 Grpc객체로부터 받은 에러입니다.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="message"></param>
    public static void HandleGrpcException(StatusCode statusCode, string message)
    {
        switch (statusCode)
        {
        }
    }
}
