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

        public static async UniTask GoToNextScene(string nextScene, object nextSceneData = null, ISceneTransition transition = null)
        {
            // transition 연출 진행중 다른 씬으로 넘어가는 것을 방지하기 위해
            SceneUIManager.Instance.isSceneChanging = true;
            if (transition == null)
            {
                transition = new SceneTransition_Instant();
            }

            currentSceneName = SceneUIManager.Instance.CurrentSceneName;

            SceneLoading.transition = transition;
            await transition.FadeInAsync();
            nextSceneName = nextScene;
            SceneLoading.nextSceneData = nextSceneData;
            SceneUIManager.Instance.ChangeScene("SceneLoading");
        }

        public void Start()
        {
            StartAsync().Forget();
        }

        private async UniTask StartAsync()
        {
            await UniTask.Yield();
            await UniTask.WhenAll(startChangeSceneAsyncTasks.Select(x => x.Invoke(currentSceneName, nextSceneName, nextSceneData)));
            SceneUIManager.OnUITransitionEvent += OneTimeCheckSceneLoaded;
            SceneUIManager.Instance.ChangeScene(nextSceneName, nextSceneData);
        }

        private void OneTimeCheckSceneLoaded(SceneUIManager.UITransition transition, string uiKey, UILayer uiLayer)
        {
            string[] defaultUINames = SceneUIManager.Instance.GetDefaultUINames(nextSceneName);
            if (defaultUINames[^1] != uiKey) // default UI는 key와 uiName이 같다.
            {
                return;
            }

            if (transition != SceneUIManager.UITransition.EnterFinished)
            {
                return;
            }

            SceneUIManager.OnUITransitionEvent -= OneTimeCheckSceneLoaded;
            SceneLoading.transition.FadeOutAsync(true).ContinueWith(ClearData).Forget();
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
