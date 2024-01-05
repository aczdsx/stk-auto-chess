using System.Collections;
using CookApps.TeamBattle.Core;
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
                transition = new SceneTransaction_Instant();
            SceneLoading.transition = transition;
            await transition.FadeInAsync();
            SceneLoading.nextScene = nextScene;
            SceneLoading.nextSceneData = nextSceneData;
            SceneUIManager.Instance.ChangeScene("Loading");
        }

        public IEnumerator Start()
        {
            yield return null;
            SceneUIManager.OnUITransactionEvent += OneTimeCheckSceneLoaded;
            SceneUIManager.Instance.ChangeScene(nextScene, nextSceneData);
        }

        private void OneTimeCheckSceneLoaded(SceneUIManager.UITransaction transaction, string uiKey, UIBase uiBase)
        {
            var defaultUINames = SceneUIManager.Instance.GetDefaultUINames(nextScene);
            if (defaultUINames[^1] != uiKey) // default UI는 key와 uiName이 같다.
                return;

            if (transaction != SceneUIManager.UITransaction.EnterFinished)
                return;

            SceneUIManager.OnUITransactionEvent -= OneTimeCheckSceneLoaded;
            transition.FadeOutAsync(true).ContinueWith(() =>
            {
                transition = null;
            }).Forget();
        }
    }
}
