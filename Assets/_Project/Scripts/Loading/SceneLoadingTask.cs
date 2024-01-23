using System;
using Cysharp.Threading.Tasks;

public static class SceneLoadingTask
{
    public static async UniTask HandleLoading(string prevSceneName, string sceneName, object defaultUIData)
    {
        if (prevSceneName == "Lobby")
        {
            await UnloadLobbyResources();
        }

        if (sceneName == "Lobby")
        {
            await LoadLobbyResources();
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
}
