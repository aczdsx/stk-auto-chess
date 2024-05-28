using System;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class TitleTask_InitializeUserData : ITitleTask
    {
        private bool isComplete;
        private bool hasError;
        public ITitleTaskPriority Priority => ITitleTaskPriority.Step_4;

        private ProgressCallback progressCallback;

        public void Initialize(TitleMain titleMainUI, ProgressCallback progressCallback)
        {
            this.progressCallback = progressCallback;
            progressCallback.Invoke(GetHashCode(), 0f);
        }

        public async UniTask RunTask()
        {
            bool res = await UserDataManager.Instance.Initialize();
            hasError = !res;
            if (hasError)
            {
                isComplete = true;
                return;
            }

            isComplete = true;
            progressCallback.Invoke(GetHashCode(), 1f);
        }

        public (bool, string) HasError()
        {
            if (!isComplete)
            {
                return (true, "아직 통신중");
            }

            return (hasError, "UserData 초기화 실패");
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
            throw new NotImplementedException();
        }
    }
}
