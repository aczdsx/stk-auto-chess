using CookApps.TeamBattle.UIManagements;

public class LobbyMain : UILayer
{
    public override void OnPreEnter(object param)
    {
        base.OnPreEnter(param);
        TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Menu, TopPanelType.Jewel, TopPanelType.Coin, TopPanelType.Bread);
    }

    public void OnClickStart()
    {
        var currentChapter = 0;
        SceneUIManager.Instance.RequestPushUI("StageSelectMain", currentChapter);
    }
}
