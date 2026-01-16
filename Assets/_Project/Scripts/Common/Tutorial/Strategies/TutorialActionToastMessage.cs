namespace CookApps.AutoBattler
{
    /// <summary>
    /// 토스트 메시지 튜토리얼 액션.
    /// 설명 팝업(말풍선) 없이 토스트 메시지만 표시하고,
    /// 토스트가 사라지면 자동으로 다음 튜토리얼로 진행합니다.
    ///
    /// tutorial_action_key: 사용 안함 (desc_key의 텍스트를 토스트로 표시)
    /// </summary>
    public class TutorialActionToastMessage : ITutorialActionStrategy
    {
        /// <summary>
        /// 토스트 완료 시 호출되는 콜백 (TutorialController에서 설정)
        /// </summary>
        public static System.Action OnToastCompleted;

        /// <summary>
        /// 현재 토스트 메시지 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        public void OnShow(TutorialActionContext context)
        {
            IsActive = true;

            // 전체 화면 마스크 설정 (HoleRadius=1, 가운데)
            context.SetFullScreenMask();

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 토스트 메시지 표시 (desc_key 사용)
            string message = LanguageManager.Instance.GetDefaultText(context.CurrentTutorial.desc_key);
            ToastManager.Instance.ShowToastWithCallback(message, OnToastClosed);
        }

        public void OnNext(TutorialActionContext context)
        {
            // 토스트 메시지는 "다음" 버튼 사용 안함
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 토스트가 사라져야 진행
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            IsActive = false;
            OnToastCompleted = null;
            context.RestoreMask();
        }

        /// <summary>
        /// 토스트가 닫힐 때 호출
        /// </summary>
        private static void OnToastClosed()
        {
            if (IsActive)
            {
                IsActive = false;
                OnToastCompleted?.Invoke();
            }
        }
    }
}
