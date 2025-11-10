using Naninovel;
using UnityEngine;

public class DialogueSkipButton : ScriptableButton
{
    [SerializeField] private GameObject objOn, objOff;
    
    private IScriptPlayer player;

    protected override void Awake ()
    {
        base.Awake();

        player = Engine.GetService<IScriptPlayer>();
    }

    protected override void OnEnable ()
    {
        base.OnEnable();
        player.OnSkip += HandleSkipModeChange;
    }

    protected override void OnDisable ()
    {
        base.OnDisable();
        player.OnSkip -= HandleSkipModeChange;
    }

    protected override void OnButtonClick ()
    {
        // player.SetSkipEnabled(!player.SkipActive);
        // SceneDialog.isSkip = player.SkipActive;
    }

    private void HandleSkipModeChange (bool enabled)
    {
        objOn.SetActive(enabled);
        objOff.SetActive(!enabled);
    }
} 
