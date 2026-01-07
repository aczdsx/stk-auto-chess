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
            public IReadOnlyList<string> NaninovelScriptNames; // 나니노벨 스크립트 이름 목록 (있으면 나니노벨을 거쳐서 이동)
        }

        private SceneLoadingData sceneLoadingData;

        public delegate UniTask SceneLoadedAsyncTask(string prevScene, string nextScene, object defaultUIData);

        private static List<SceneLoadedAsyncTask> startChangeSceneAsyncTasks = new();

        public static event SceneLoadedAsyncTask OnStartChangeScene
        {
            add => startChangeSceneAsyncTasks.Add(value);
            remove => startChangeSceneAsyncTasks.Remove(value);
        }

        /// <summary>
        /// 무거운 씬간 전환시 사용, 전환 중간에 가벼운 씬을 둠으로써 무거운 씬2개가 동시에 떠서 메모리가 부족해지는 것을 방지
        /// </summary>
        /// <param name="nextScene">목적지 씬 이름</param>
        /// <param name="nextSceneData">목적지 씬 데이터</param>
        /// <param name="sceneLoadingEventReceiver">씬 로딩 이벤트 리시버</param>
        /// <param name="naninovelScriptNames">나니노벨 스크립트 이름 목록 (있으면 나니노벨을 거쳐서 이동)</param>
        public static void GoToNextScene(string nextScene, object nextSceneData = null, SceneLoadingEventReceiver sceneLoadingEventReceiver = null, IReadOnlyList<string> naninovelScriptNames = null)
        {
            // 나니노벨이 있으면 나니노벨 씬으로 먼저 이동
            if (naninovelScriptNames != null && naninovelScriptNames.Count > 0)
            {
                GoToNextSceneViaNaninovel(nextScene, nextSceneData, sceneLoadingEventReceiver, naninovelScriptNames);
                return;
            }

            // transition 연출 진행중 다른 씬으로 넘어가는 것을 방지하기 위해
            SceneUILayerManager.Instance.isSceneChanging = true;
            var data = new SceneLoadingData
            {
                CurrentSceneName = SceneUILayerManager.Instance.CurrentSceneName,
                NextSceneName = nextScene,
                NextSceneData = nextSceneData,
                SceneLoadingEventReceiver = sceneLoadingEventReceiver,
                NaninovelScriptNames = null
            };
            SceneUILayerManager.Instance.ChangeScene("SceneLoading", data);
        }

        /// <summary>
        /// 나니노벨을 거쳐서 목적지 씬으로 이동
        /// </summary>
        private static void GoToNextSceneViaNaninovel(string nextScene, object nextSceneData, SceneLoadingEventReceiver sceneLoadingEventReceiver, IReadOnlyList<string> naninovelScriptNames)
        {
            // 나니노벨 종료 시 원래 목적지로 이동하는 액션 생성
            System.Action onNaninovelEnd = () =>
            {
                // 나니노벨이 끝나면 원래 목적지로 이동
                SceneUILayerManager.Instance.isSceneChanging = true;
                var data = new SceneLoadingData
                {
                    CurrentSceneName = "Naninovel",
                    NextSceneName = nextScene,
                    NextSceneData = nextSceneData,
                    SceneLoadingEventReceiver = sceneLoadingEventReceiver,
                    NaninovelScriptNames = null
                };
                SceneUILayerManager.Instance.ChangeScene("SceneLoading", data);
            };

            // 나니노벨 씬으로 이동
            SceneUILayerManager.Instance.isSceneChanging = true;
            var naninovelData = new SceneLoadingData
            {
                CurrentSceneName = SceneUILayerManager.Instance.CurrentSceneName,
                NextSceneName = "Naninovel",
                NextSceneData = (naninovelScriptNames, onNaninovelEnd), // 튜플로 스크립트 이름 목록과 종료 액션 전달
                SceneLoadingEventReceiver = null,
                NaninovelScriptNames = naninovelScriptNames
            };
            SceneUILayerManager.Instance.ChangeScene("SceneLoading", naninovelData);
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
