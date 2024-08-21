#define USE_LOCAL_SERVER

using System;
using System.Linq;
using System.Text.RegularExpressions;
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
        private bool isComplete;
        private bool hasError;
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
            
            progressCallback.Invoke(GetHashCode(), 1f);
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
