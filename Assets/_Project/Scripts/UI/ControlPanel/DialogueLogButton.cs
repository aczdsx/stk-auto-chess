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
        Debug.Log("DialogueLogButton / OnButtonClick ()");
        uiManager.GetUI<IPauseUI>()?.Hide();
        uiManager.GetUI<IBacklogUI>()?.Show();
    }
}
