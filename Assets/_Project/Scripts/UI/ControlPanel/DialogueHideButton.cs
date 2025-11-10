using Naninovel;

public class DialogueHideButton : ScriptableButton
{
    private IUIManager uiManager;

    protected override void Awake ()
    {
        base.Awake();

        uiManager = Engine.GetService<IUIManager>();
    }

    protected override void OnButtonClick () => uiManager.SetUIVisibleWithToggle(false);
} 
