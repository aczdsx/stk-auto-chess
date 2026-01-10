namespace CookApps.AutoBattler
{
    /// <summary>
    /// 단순 설명 표시 액션.
    /// 화살표 없이 텍스트만 표시하고, 딤드 클릭으로 다음 진행.
    /// </summary>
    public class TutorialActionNone : ITutorialActionStrategy
    {
        public void OnShow(TutorialActionContext context)
        {
            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);
            context.NextObj.SetActive(true); // [TODO] 나중에 텍스트 애니메이션 완료 된 후로 변경 필요
        }

        public void OnNext(TutorialActionContext context)
        {
            // 다음 버튼 활성화
            context.NextObj.SetActive(true);
            context.ArrowRectTransform.gameObject.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 다음 진행 가능
            return true;
        }

        public void OnClear(TutorialActionContext context)
        {
            // 다음 버튼 비활성화
            context.NextObj.SetActive(false);
        }
    }
}
