using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneLoading : CachedMonoBehaviour
    {
        private static string currentSceneName;
        private static string nextSceneName;
        private static object nextSceneData;
        private static ISceneTransition transition;

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
        public static async UniTask GoToNextScene(string nextScene, object nextSceneData = null, ISceneTransition transition = null)
        {
            // transition 연출 진행중 다른 씬으로 넘어가는 것을 방지하기 위해
            SceneUILayerManager.Instance.isSceneChanging = true;
            if (transition == null)
            {
                transition = new SceneTransition_Instant();
            }

            currentSceneName = SceneUILayerManager.Instance.CurrentSceneName;

            SceneLoading.transition = transition;
            await transition.FadeInAsync();
            nextSceneName = nextScene;
            SceneLoading.nextSceneData = nextSceneData;
            SceneUILayerManager.Instance.ChangeScene("SceneLoading");
        }

        public void Start()
        {
            StartAsync().Forget();
        }

        private async UniTask StartAsync()
        {
            await UniTask.Yield();
            await UniTask.WhenAll(startChangeSceneAsyncTasks.Select(x => x.Invoke(currentSceneName, nextSceneName, nextSceneData)));
            SceneUILayerManager.SceneLoadAsyncOperationWrapper wrapper = SceneUILayerManager.Instance.ChangeScene(nextSceneName, nextSceneData);
            wrapper.Completed += OneTimeCheckSceneLoaded;
        }

        private void OneTimeCheckSceneLoaded()
        {
            transition.FadeOutAsync(true);
            ClearData();
        }

        private void ClearData()
        {
            currentSceneName = null;
            nextSceneName = null;
            nextSceneData = null;
            transition = null;
        }
    }
}
