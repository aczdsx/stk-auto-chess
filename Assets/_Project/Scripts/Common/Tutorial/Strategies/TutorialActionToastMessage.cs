using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 토스트 메시지 튜토리얼 액션.
    /// 설명 팝업(말풍선) 없이 튜토리얼 내부 토스트 UI를 표시하고,
    /// 애니메이션이 끝나면 자동으로 다음 튜토리얼로 진행합니다.
    ///
    /// tutorial_action_key: 사용 안함 (desc_key의 텍스트를 토스트로 표시)
    /// </summary>
    public class TutorialActionToastMessage : ITutorialActionStrategy
    {
        private static readonly int LongAnim = Animator.StringToHash("LongAnim");

        private TutorialActionContext _cachedContext;

        /// <summary>
        /// 현재 토스트 메시지 튜토리얼 진행 중인지 여부
        /// </summary>
        public static bool IsActive { get; private set; }

        public void OnShow(TutorialActionContext context)
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
            _cachedContext = context;
            IsActive = true;

            // 전체 화면 마스크 설정 (HoleRadius=1, 가운데)
            context.SetFullScreenMask();

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 토스트 메시지 텍스트 설정 (desc_key 사용)
            string message = LanguageManager.Instance.GetDialogueText(context.CurrentTutorial.desc_key);
            context.TutorialToastText.text = message;

            // 토스트 오브젝트 활성화 및 애니메이션 시작
            context.TutorialToastObj.SetActive(true);
            context.TutorialToastAnimator.SetTrigger(LongAnim);

            // 애니메이션 완료 대기 후 다음 진행
            WaitForAnimationAndProceedAsync(context).Forget();
        }

        /// <summary>
        /// 애니메이션 완료까지 대기 후 다음 튜토리얼로 진행
        /// </summary>
        private async UniTaskVoid WaitForAnimationAndProceedAsync(TutorialActionContext context)
        {
            // 다음 프레임까지 대기 (Trigger가 적용되도록)
            await UniTask.Yield();

            // 현재 애니메이션 클립 길이 가져오기
            var animator = context.TutorialToastAnimator;
            var clipInfo = animator.GetCurrentAnimatorClipInfo(0);

            if (clipInfo.Length > 0)
            {
                float clipLength = clipInfo[0].clip.length;
                await UniTask.Delay(TimeSpan.FromSeconds(clipLength));
            }
            else
            {
                // 클립 정보를 못 가져온 경우 기본 대기
                await UniTask.Delay(TimeSpan.FromSeconds(2f));
            }

            // 아직 활성 상태인 경우에만 진행
            if (IsActive)
            {
                // 토스트 숨김
                context.TutorialToastObj.SetActive(false);

                IsActive = false;
                _cachedContext?.OnCompleted?.Invoke();
            }
        }

        public void OnNext(TutorialActionContext context)
        {
            // 토스트 메시지는 "다음" 버튼 사용 안함
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            // 딤드 클릭으로 진행 불가 - 애니메이션이 끝나야 진행
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            IsActive = false;
            _cachedContext = null;

            // 토스트 숨김
            if (context.TutorialToastObj != null)
            {
                context.TutorialToastObj.SetActive(false);
            }

            context.RestoreMask();
        }
    }
}
