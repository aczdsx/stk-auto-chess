#define USE_LOCAL_SERVER

using System;
using System.Linq;
using System.Text.RegularExpressions;
using CookApps.Auth;
using CookApps.Build;
using CookApps.gRPC;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    public class TitleTask_ConnectWithServer : ITitleTask
    {
        private bool isLogin = false;

        private bool isComplete;
        private bool hasError;
        private bool serverConnectFail;
        private CheckVersionResponse checkVersionResponse;

        public ITitleTaskPriority Priority => ITitleTaskPriority.Step_2;

        private ProgressCallback progressCallback;

        public void Initialize(TitleMain titleMainUI, ProgressCallback progressCallback)
        {
            this.progressCallback = progressCallback;
            progressCallback.Invoke(GetHashCode(), 0f);
        }

        public async UniTask RunTask()
        {
            var matches = Regex.Matches(BuildInfo.GetVersionCode(), @"\d+");
            var res = ZString.Join("", matches.Select(x => x.Value));
            if (!int.TryParse(res, out var versionCode)) versionCode = 1000;

#if SERVER_REAL
            var initializeParam = new GrpcInitializeParam(
                url : "stkauto.prod.cookappsgames.com",
                port: 443,
                channelCredentials: ChannelCredentials.SecureSSL,
                // channelCredentials: EnumChannelCredentials.INSECURE,
                bundleVersion: versionCode,
#else
            var initializeParam = new GrpcInitializeParam(
                "stkauto-adfjk.dev.cookappsgames.com",
                443,
                ChannelCredentials.SecureSSL,
                versionCode,
#endif

#if UNITY_IOS
                store:StoreMap.AppStore,
#else
                StoreMap.GooglePlay,
#endif
                GrpcExceptionHandler.HandleSuccess,
                GrpcExceptionHandler.HandleServerException,
                GrpcExceptionHandler.HandleGrpcException,
                true
            );

            GrpcManager.Instance.StartUp(initializeParam);

            // 버전 체크
            var tryCount = 0;
            do
            {
                checkVersionResponse = await GrpcManager.Instance.Lobby.CheckVersionAsync();
                if (checkVersionResponse.IsError)
                    Debug.LogError("CheckVersionAsync Error!!");
                tryCount++;
            } while (checkVersionResponse.IsError && tryCount < 3);

            progressCallback.Invoke(GetHashCode(), 1f);

            // 로그인 체크
            if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Apple))
                isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            else if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Google))
                isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            else if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Facebook))
                isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            else if (GrpcManager.Instance.Auth.IsLoggedIn(AuthPlatform.Guest)) isLogin = await GrpcManager.Instance.Auth.AuthenticateAsync();
            // else
            // {
            //     // login.SetActive(true);
            //     // taskSlider.gameObject.SetActive(false);
            //
            //     while (!LoginManager.Instance.CheckIsLoggedIn())
            //     {
            //         await UniTask.Yield();
            //     }
            //
            //     isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            // }

            // 앱 이벤트 Auth 설정
            CAppAuth.SetUID(GrpcManager.Instance.Auth.AuthenticateData.Uid);

#if SERVER_REAL
            CAppAuth.SetServer(EnumServer.PRODUCTION);
#else
            CAppAuth.SetServer(EnumServer.DEV);
#endif
        }

        public (bool, string) HasError()
        {
            if (!isComplete) return (true, "아직 통신중");

            return (hasError, "Loading");
        }

        public async UniTask HandleError()
        {
            await UniTask.Yield();
        }

        public T GetResult<T>()
        {
            throw new NotImplementedException();
        }
    }
}