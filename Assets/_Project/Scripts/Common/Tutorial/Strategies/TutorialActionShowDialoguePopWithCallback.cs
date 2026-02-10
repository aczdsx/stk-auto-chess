using System;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 다이얼로그 팝업 표시 튜토리얼 액션 (콜백 버전).
    /// tutorial_action_key에 지정된 dialogue_group_id로 DialoguePopup을 표시하고,
    /// 다이얼로그가 완료되면 콜백을 통해 다음 튜토리얼로 진행합니다.
    ///
    /// 주의: DIALOGUE_POP_END 트리거와 중복 사용하지 마세요.
    ///
    /// tutorial_action_key: dialogue_group_id (다이얼로그 그룹 ID)
    /// </summary>
    public class TutorialActionShowDialoguePopWithCallback : ITutorialActionStrategy
    {
        /// <summary>
        /// 현재 튜토리얼 컨텍스트 참조 (콜백용)
        /// </summary>
        private static TutorialActionContext _cachedContext;

        public void OnShow(TutorialActionContext context)
        {
            _cachedContext = context;

            // 화살표 비활성화
            context.ArrowRectTransform.gameObject.SetActive(false);

            // 튜토리얼 캔버스 비활성화 (DialoguePopup 클릭을 위해)
            if (context.TutorialCanvas != null)
            {
                context.TutorialCanvas.enabled = false;
            }

            // tutorial_action_key를 dialogue_group_id로 파싱
            string actionKey = context.CurrentTutorial.tutorial_action_key;
            if (!int.TryParse(actionKey, out int dialogueGroupId))
            {
                UnityEngine.Debug.LogError($"[TutorialActionShowDialoguePopWithCallback] Invalid dialogue_group_id: {actionKey}");
                return;
            }

            // DialoguePopup 표시 (완료 콜백 전달)
            SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupId, (Action)HandleDialogueCompleted)).Forget();
        }

        private static void HandleDialogueCompleted()
        {
            _cachedContext?.OnCompleted?.Invoke();
        }

        public void OnNext(TutorialActionContext context)
        {
            context.NextObj.SetActive(false);
        }

        public bool CanProceedOnDimmedClick(TutorialActionContext context)
        {
            return false;
        }

        public void OnClear(TutorialActionContext context)
        {
            _cachedContext = null;

            if (context.TutorialCanvas != null)
            {
                context.TutorialCanvas.enabled = true;
            }
        }
    }
}
