#define USE_LOCAL_SERVER

using System;
using System.Linq;
using System.Text.RegularExpressions;
using CookApps.Auth;
using CookApps.Build;
using CookApps.gRPC;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using Tech.Universal.V2;
using GrpcGame;

namespace CookApps.AutoBattler
{
    public class TitleTask_ConnectWithServer : ITitleTask
    {
        private bool isLogin = false;
        
        private bool isComplete;
        private bool hasError;
        private bool serverConnectFail;
        private CheckVersionResult checkVersionResult;

        public ITitleTaskPriority Priority => ITitleTaskPriority.Step_2;

        private ProgressCallback progressCallback;

        public void Initialize(TitleMain titleMainUI, ProgressCallback progressCallback)
        {
            this.progressCallback = progressCallback;
            progressCallback.Invoke(GetHashCode(), 0f);
        }

        public async UniTask RunTask()
        {
            MatchCollection matches = Regex.Matches(BuildInfo.GetVersionCode(), @"\d+");
            var res = ZString.Join("", matches.Select(x => x.Value));
            if (!int.TryParse(res, out var versionCode))
            {
                versionCode = 1000;
            }
            
#if SERVER_REAL
            var param = new GrpcInitializeParam(
                url : "stkauto.prod.cookappsgames.com",
                port: 443,
                channelCredentials: EnumChannelCredentials.SECURE_SSL,
                // channelCredentials: EnumChannelCredentials.INSECURE,
                bundleVersion: versionCode,
#else
            var param = new GrpcInitializeParam(
                url : "stkauto-adfjk.dev.cookappsgames.com",
                port: 443,
                channelCredentials: EnumChannelCredentials.SECURE_SSL,
                // channelCredentials: EnumChannelCredentials.INSECURE,
                bundleVersion: versionCode,
#endif
                
#if UNITY_IOS
                store:Store.AppStore,
#else
                store:Store.GooglePlay,
#endif
                onHandleGrpcSuccess: GrpcExceptionHandler.HandleServerSuccess,
                onHandleServerException: GrpcExceptionHandler.HandleServerException,
                onHandleGrpcException: GrpcExceptionHandler.HandleGrpcException,
                enabledLog:true,
                queueThreshold:3);
            
            UniversalGrpcManager.Instance.Initialize(param);
            GameGrpcManager.Instance.Initialize(param);
            
            var hacheryParam = new GrpcInitializeParam(true);
            HatcheryGrpcManager.Instance.Initialize(hacheryParam);
            
            // 버전 체크
            var tryCount = 0;
            do
            {
                checkVersionResult = await UniversalGrpcManager.Instance.CheckVersionAsync();
                if (checkVersionResult.IsError)
                    Debug.LogError("CheckVersionAsync Error!!");
                tryCount++;
            } while (checkVersionResult.IsError && tryCount < 3);

            progressCallback.Invoke(GetHashCode(), 1f);
            
            // 로그인 체크
            if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Apple))
            {
                isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            else if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Google))
            {
                isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            else if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Facebook))
            {
                isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
            else if (UniversalGrpcManager.Instance.IsLoggedIn(AuthPlatform.Guest))
            {
                isLogin = await UniversalGrpcManager.Instance.AutoLoginAsync();
            }
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
            CAppAuth.SetUID(UniversalGrpcManager.Instance.Uid);
            
#if SERVER_REAL
            CAppAuth.SetServer(EnumServer.PRODUCTION);
#else
            CAppAuth.SetServer(EnumServer.DEV);
#endif
        }

        public (bool, string) HasError()
        {
            if (!isComplete)
            {
                return (true, "아직 통신중");
            }

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
