using CookApps.TeamBattle.UIManagements;

public class LobbyMain : UIBase
{
    public void OnClickStart()
    {
        var currentChapter = 0;
        SceneUIManager.Instance.RequestPushUI("StageSelectMain", currentChapter);
    }
}
