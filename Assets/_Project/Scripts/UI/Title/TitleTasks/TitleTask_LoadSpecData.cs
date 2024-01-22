using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.SampleTeamBattle
{
    public class TitleTask_LoadSpecData : ITitleTask
    {
        private bool isComplete;
        private bool isErrorOccur;

        public ITitleTaskPriority Priority => ITitleTaskPriority.Step_3;

        private bool isStartUp;
        private ProgressCallback progressCallback;

        public void Initialize(TitleMain titleMainUI, ProgressCallback progressCallback)
        {
            this.progressCallback = progressCallback;
            progressCallback.Invoke(GetHashCode(), 0f);
        }

        public async UniTask RunTask()
        {
            progressCallback.Invoke(GetHashCode(), 0.5f);
            await SpecDataManager.Instance.Initialize();
            // GlobalEffectCodeInfoManager.Instance.Initialize(); // userdatamanager.initialize보다 먼저 호출되어야함
            EffectCodeManager.Instance.LoadEffectCodeClassDatas();
            isComplete = true;
            progressCallback.Invoke(GetHashCode(), 1f);
            progressCallback = null;
        }

        public (bool, string) HasError()
        {
            if (!isComplete)
            {
                return (true, "아직 처리중");
            }

            if (isErrorOccur)
            {
                return (true, "스펙 로드 못함!");
            }

            return (false, null);
        }

        public async UniTask HandleError()
        {
            while (true)
            {
                await UniTask.Yield();
            }
        }

        public T GetResult<T>()
        {
            return default;
        }
    }
}
