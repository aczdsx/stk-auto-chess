using Naninovel;

/// <summary>
/// 대화 종료 버튼.
/// 확인 팝업 후 스크립트 마지막 명령어(@end)로 점프하여 종료합니다.
/// </summary>
public class DialogueEndButton : ScriptableButton
{
    private IScriptPlayer player;

    protected override void Awake()
    {
        base.Awake();
        player = Engine.GetService<IScriptPlayer>();
    }

    protected override void OnButtonClick()
    {
        ShowConfirmPopup();
    }

    private void ShowConfirmPopup()
    {
        OnConfirmEnd();
        // var popupData = new SystemConfirmPopupData();
        // popupData.SetPopupData(
        //     "시스템 알림",
        //     "대화를 종료하시겠습니까?",
        //     "확인",
        //     "취소",
        //     OnConfirmEnd
        // );

        // SceneUILayerManager.Instance.PushUILayerAsync<SystemConfirmPopup>(popupData).Forget();
    }

    private void OnConfirmEnd()
    {
        JumpToEndLabel();
    }

    private void JumpToEndLabel()
    {
        if (player == null || player.PlayedScript == null)
        {
            Debug.LogWarning("[DialogueEndButton] 재생 중인 스크립트가 없습니다.");
            return;
        }

        // 마지막 명령어(@end)로 점프
        var lastIndex = player.Playlist.Count - 1;
        player.Resume(lastIndex);

        // 입력 대기 해제하여 바로 실행
        if (player.WaitingForInput)
        {
            player.SetWaitingForInputEnabled(false);
        }
    }
}
