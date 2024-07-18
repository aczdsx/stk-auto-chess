using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public static class SceneLoadingTask
    {
        public static async UniTask HandleLoading(string prevSceneName, string sceneName, object defaultUIData)
        {
            Debug.LogColor($"Scene Change : {prevSceneName} -> {sceneName}", "blue");

            if (prevSceneName == "Lobby")
            {
                await UnloadLobbyResources();
            }

            if (prevSceneName == "InGame")
            {
                await UnloadInGameResources();
            }

            if (sceneName == "Lobby")
            {
                await LoadLobbyResources(defaultUIData);
            }

            if (sceneName == "InGame")
            {
                await LoadInGameResources(defaultUIData);
            }
        }

        private static async UniTask LoadLobbyResources(object defaultUIData)
        {
            int chapter = (int) defaultUIData;
            await InGameResourceHolder.LoadLobbyResources(chapter);

            await TopPanelSingleUseHelper.Instance.Initialize();
        }

        private static async UniTask UnloadLobbyResources()
        {
            TopPanelSingleUseHelper.Instance.Clear();
            await UniTask.Yield();
        }

        private static async UniTask LoadInGameResources(object defaultUIData)
        {
            (InGameType inGameType, IGameStateUI state, int id) = ((InGameType, IGameStateUI, int)) defaultUIData;
            await InGameResourceHolder.LoadResources(inGameType, state, id);
        }

        private static async UniTask UnloadInGameResources()
        {
            InGameResourceHolder.UnloadResources();
            await UniTask.Yield();
        }
    }
}
