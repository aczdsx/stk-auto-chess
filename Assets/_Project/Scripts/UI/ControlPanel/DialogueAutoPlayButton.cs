using CookApps.AutoBattler;
using Naninovel;
using UnityEngine;

public class DialogueAutoPlayButton : ScriptableButton
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
        player.OnAutoPlay += HandleAutoModeChange;
    }

    protected override void OnDisable ()
    {
        base.OnDisable();
        player.OnAutoPlay -= HandleAutoModeChange;
    }

    protected override void OnButtonClick ()
    {
        player.SetAutoPlayEnabled(!player.AutoPlayActive);
        NaninovelMain.isAuto = player.AutoPlayActive;
    }

    private void HandleAutoModeChange (bool enabled)
    {
        objOn.SetActive(enabled);
        objOff.SetActive(!enabled);// UIComponent.LabelColorMultiplier = enabled ? activeColorMultiplier : Color.white;
    }
} 
