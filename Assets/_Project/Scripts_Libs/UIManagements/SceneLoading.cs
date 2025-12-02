using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneLoadingEventReceiver
    {
        public Func<UniTask> OnLoading { get; }
        public Action OnNextSceneLoaded { get; }
        
        public SceneLoadingEventReceiver(Func<UniTask> onLoading, Action onNextSceneLoaded)
        {
            OnLoading = onLoading;
            OnNextSceneLoaded = onNextSceneLoaded;
        }
    }
    
    public abstract class SceneLoading : UILayer
    {
        private class SceneLoadingData
        {
            public string CurrentSceneName;
            public string NextSceneName;
            public object NextSceneData;
            public SceneLoadingEventReceiver SceneLoadingEventReceiver;
        }

        private SceneLoadingData sceneLoadingData;
        
        public delegate UniTask SceneLoadedAsyncTask(string prevScene, string nextScene, object defaultUIData);

        private static List<SceneLoadedAsyncTask> startChangeSceneAsyncTasks = new ();

        public static event SceneLoadedAsyncTask OnStartChangeScene
        {
            add => startChangeSceneAsyncTasks.Add(value);
            remove => startChangeSceneAsyncTasks.Remove(value);
        }
            
        /// <summary>
        /// 무거운 씬간 전환시 사용, 전환 중간에 가벼운 씬을 둠으로써 무거운 씬2개가 동시에 떠서 메모리가 부족해지는 것을 방지
        /// </summary>
        /// <param name="nextScene"></param>
        /// <param name="nextSceneData"></param>
        /// <param name="transition"></param>
        public static void GoToNextScene(string nextScene, object nextSceneData = null, SceneLoadingEventReceiver sceneLoadingEventReceiver = null)
        {
            // transition 연출 진행중 다른 씬으로 넘어가는 것을 방지하기 위해
            SceneUILayerManager.Instance.isSceneChanging = true;
            var data = new SceneLoadingData
            {
                CurrentSceneName = SceneUILayerManager.Instance.CurrentSceneName,
                NextSceneName = nextScene,
                NextSceneData = nextSceneData,
                SceneLoadingEventReceiver = sceneLoadingEventReceiver
            };
            SceneUILayerManager.Instance.ChangeScene("SceneLoading", data);
        }

        protected internal override void OnPreEnter(object param)
        {
            sceneLoadingData = param as SceneLoadingData;
            EnterAsync().Forget();
        }

        protected virtual async UniTask EnterAsync()
        {
            await UniTask.Yield();

            await UniTask.WhenAll(startChangeSceneAsyncTasks.Select(x => x.Invoke(sceneLoadingData.CurrentSceneName, sceneLoadingData.NextSceneName, sceneLoadingData.NextSceneData)));

            if (sceneLoadingData.SceneLoadingEventReceiver != null)
            {
                if (sceneLoadingData.SceneLoadingEventReceiver.OnLoading != null)
                {
                    await sceneLoadingData.SceneLoadingEventReceiver.OnLoading();
                }
            }

            Resources.UnloadUnusedAssets();

            var wrapper = SceneUILayerManager.Instance.ChangeScene(sceneLoadingData.NextSceneName, sceneLoadingData.NextSceneData);
            await UniTask.WaitUntil(() => wrapper.IsDone);
            
            if (sceneLoadingData.SceneLoadingEventReceiver != null)
            {
                sceneLoadingData.SceneLoadingEventReceiver.OnNextSceneLoaded?.Invoke();
            }
            ClearData();
        }

        protected virtual void ClearData()
        {
            sceneLoadingData = null;
        }
    }
}
