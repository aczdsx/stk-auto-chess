namespace CookApps.AutoBattler
{
    /// <summary>
    /// 오브젝트 포커스 액션.
    /// 특정 오브젝트에 홀 마스크를 표시하여 강조합니다.
    /// </summary>
    public class TutorialActionFocusObject : ITutorialActionStrategy
    {
        public void OnShow(TutorialActionContext context)
        {
            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 포커스 타겟 찾기
            context.TargetUnmaskObj = TutorialTargetRegistry.FindGameObject(context.CurrentTutorial.tutorial_action_key);

            if (context.TargetUnmaskObj == null)
            {
                Debug.LogWarning($"[TutorialActionFocusObject] 포커스 타겟을 찾을 수 없음: {context.CurrentTutorial.tutorial_action_key}");
            }
            context.NextObj.SetActive(true); // [TODO] 나중에 텍스트 애니메이션 완료 된 후로 변경 필요
        }

        public void OnNext(TutorialActionContext context)
        {
            // 다음 버튼 활성화
            context.NextObj.SetActive(true);
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

            // 포커스 타겟 정리
            context.TargetUnmaskObj = null;
        }
    }
}
