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

            if (sceneName == "Lobby")
            {
                await LoadLobbyResources();
            }

            if (prevSceneName == "InGame")
            {
                await UnloadInGameResources();
            }

            if (sceneName == "InGame")
            {
                await LoadInGameResources(defaultUIData);
            }
        }

        private static async UniTask LoadLobbyResources()
        {
            await TopPanelSingleUseHelper.Instance.Initialize();
        }

        private static async UniTask UnloadLobbyResources()
        {
            TopPanelSingleUseHelper.Instance.Clear();
            await UniTask.Yield();
        }

        private static async UniTask LoadInGameResources(object defaultUIData)
        {
            (int chapter, int stageIndex, DifficultyType difficultyType) = ((int, int, DifficultyType)) defaultUIData;
            await InGameResourceHolder.LoadResources(chapter, stageIndex, difficultyType);
        }

        private static async UniTask UnloadInGameResources()
        {
            InGameResourceHolder.UnloadResources();
            InGameHpBarViewPool.Instance.ReleasePool();
            await UniTask.Yield();
        }
    }
}
