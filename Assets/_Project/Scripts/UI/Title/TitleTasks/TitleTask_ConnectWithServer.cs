#define USE_LOCAL_SERVER

using System;
using CookApps.gRPC;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using Cysharp.Threading.Tasks;

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
            var param = new GrpcInitializeParam(true);
            HatcheryGrpcManager.Instance.Initialize(param);
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
