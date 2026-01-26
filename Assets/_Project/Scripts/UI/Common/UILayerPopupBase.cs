using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// UILayerType.Popup용 Base 클래스
    /// OnPostEnter/OnPostExit 시점에 튜토리얼 트리거 발동
    /// TODO: TutorialManager에서 Event 받아서 처리하도록 리팩토링 필요
    /// </summary>
    public abstract class UILayerPopupBase : UILayer
    {
        protected override void OnPostEnter()
        {
            base.OnPostEnter();

            if (TutorialManager.Instance != null)
            {
                Debug.Log($"[UILayerPopupBase] OnPostEnter: {GetType().Name}");
                TutorialManager.Instance.HandleTutorialAction(
                    TutorialTriggerType.SHOW_POP_COMPLETE,
                    GetType().Name);
            }
        }

        protected override void OnPostExit()
        {
            base.OnPostExit();

            if (TutorialManager.Instance != null)
            {
                Debug.Log($"[UILayerPopupBase] OnPostExit: {GetType().Name}");
                TutorialManager.Instance.HandleTutorialAction(
                    TutorialTriggerType.CLOSE_POP_COMPLETE,
                    GetType().Name);
            }
        }
    }
}
