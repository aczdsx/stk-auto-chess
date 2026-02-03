using CookApps.AutoBattler;
using Naninovel;
using Naninovel.UI;

public class DialogueLogButton : ScriptableButton
{
    private IUIManager uiManager;

    protected override void Awake ()
    {
        base.Awake();

        uiManager = Engine.GetService<IUIManager>();
    }

    protected override void OnButtonClick ()
    {
        uiManager.GetUI<IPauseUI>()?.Hide();

        var backlogUI = uiManager.GetUI<IBacklogUI>();
        if (backlogUI is CustomBacklogPanel customPanel)
            customPanel.ShowFromButton();
        else
            backlogUI?.Show();
    }
}
