using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public static class SceneLoadingTask
    {
        public static async UniTask HandleLoading(string prevSceneName, string sceneName, object defaultUIData)
        {
            SpriteManager.Instance.UnloadAllSprites();
            Debug.LogColor($"Scene Change : {prevSceneName} -> {sceneName}", "blue");

            if (prevSceneName == "Lobby")
            {
                // await UnloadLobbyResources();
            }

            if (prevSceneName == "BattleReady")
            {
                await UnloadBattleReadyResources();
            }

            if (prevSceneName == "InGame")
            {
                await UnloadInGameResources();
            }

            if (sceneName == "Lobby")
            {
                await LoadLobbyResources(defaultUIData);
            }

            if (sceneName == "BattleReady")
            {
                await LoadBattleReadyResources(defaultUIData);
            }

            if (sceneName == "InGame")
            {
                await LoadInGameResources(defaultUIData);
            }
        }

        private static async UniTask LoadLobbyResources(object defaultUIData)
        {
            await TopPanelSingleUseHelper.Instance.Initialize();
        }

        private static async UniTask LoadBattleReadyResources(object defaultUIData)
        {
            int chapter = (int) defaultUIData;
            await InGameResourceHolder.LoadBattleReadyResources(chapter);

            await TopPanelSingleUseHelper.Instance.Initialize();
        }

        private static async UniTask UnloadBattleReadyResources()
        {
            TopPanelSingleUseHelper.Instance.Clear();
            await UniTask.Yield();
        }

        private static async UniTask LoadInGameResources(object defaultUIData)
        {
            switch (defaultUIData)
            {
                case (InGameType inGameType, IGameStateUICore state, int id):
                    await InGameResourceHolder.LoadResources(inGameType, state, id);
                    break;
                case (InGameType inGameType, IGameStateUICore state, UserPVPBattleDetailData data):
                    await InGameResourceHolder.LoadResources(inGameType, state, 0);
                    break;
            }
        }

        private static async UniTask UnloadInGameResources()
        {
            InGameResourceHolder.UnloadResources();
            await UniTask.Yield();
        }
    }
}
