using System.Collections;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public class SceneLoading : CachedMonoBehaviour
    {
        private static string nextScene;
        private static string nextSceneBaseUI;
        private static object nextSceneData;
        private static ISceneTransition transition;

        public static async UniTask GoToNextScene(string nextScene, object nextSceneData = null, ISceneTransition transition = null)
        {
            SceneUIManager.Instance.isSceneChanging = true;
            if (transition == null)
            {
                transition = new SceneTransition_Instant();
            }

            SceneLoading.transition = transition;
            await transition.FadeInAsync();
            SceneLoading.nextScene = nextScene;
            SceneLoading.nextSceneData = nextSceneData;
            SceneUIManager.Instance.ChangeScene("SceneLoading");
        }

        public IEnumerator Start()
        {
            yield return null;
            SceneUIManager.OnUITransitionEvent += OneTimeCheckSceneLoaded;
            SceneUIManager.Instance.ChangeScene(nextScene, nextSceneData);
        }

        private void OneTimeCheckSceneLoaded(SceneUIManager.UITransition transition, string uiKey, UILayer uiLayer)
        {
            string[] defaultUINames = SceneUIManager.Instance.GetDefaultUINames(nextScene);
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
            nextScene = null;
            nextSceneBaseUI = null;
            nextSceneData = null;
            transition = null;
        }
    }
}
