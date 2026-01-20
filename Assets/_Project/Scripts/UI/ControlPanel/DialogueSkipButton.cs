using CookApps.AutoBattler;
using Naninovel;
using UnityEngine;

public class DialogueSkipButton : ScriptableButton
{
    [SerializeField] private GameObject objOn, objOff;

    private IScriptPlayer player;
    private IMoviePlayer moviePlayer;

    protected override void Awake ()
    {
        base.Awake();

        player = Engine.GetService<IScriptPlayer>();
        moviePlayer = Engine.GetService<IMoviePlayer>();
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
        player.SetSkipEnabled(!player.SkipActive);
        NaninovelMain.isSkip = player.SkipActive;

        // Skip 활성화 시 영상 재생 중이면 스킵
        if (player.SkipActive && moviePlayer != null && moviePlayer.Playing)
        {
            moviePlayer.Stop();
        }
    }

    private void HandleSkipModeChange (bool enabled)
    {
        objOn.SetActive(enabled);
        objOff.SetActive(!enabled);

        // Skip 모드 변경 시에도 영상 스킵 체크
        if (enabled && moviePlayer != null && moviePlayer.Playing)
        {
            moviePlayer.Stop();
        }
    }
} 
