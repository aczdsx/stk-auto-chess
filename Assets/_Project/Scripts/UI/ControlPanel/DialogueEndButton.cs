using CookApps.AutoBattler;
using Naninovel;

/// <summary>
/// 대화 종료 버튼.
/// 확인 팝업 후 즉시 종료하여 @end 로직을 실행합니다.
/// </summary>
public class DialogueEndButton : ScriptableButton
{
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
        NaninovelMain.GetNaninovelMain()?.SkipToEndAsync();
    }
}
