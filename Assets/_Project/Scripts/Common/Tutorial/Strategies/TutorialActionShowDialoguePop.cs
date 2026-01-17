using System;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 다이얼로그 팝업 표시 튜토리얼 액션.
    /// tutorial_action_key에 지정된 dialogue_group_id로 DialoguePopup을 표시하고,
    /// 다이얼로그가 완료되면 다음 튜토리얼로 진행합니다.
    ///
    /// tutorial_action_key: dialogue_group_id (다이얼로그 그룹 ID)
    /// </summary>
    public class TutorialActionShowDialoguePop : ITutorialActionStrategy
    {
        /// <summary>
        /// 다이얼로그 완료 시 호출될 콜백
        /// </summary>
        public static Action OnDialogueCompleted;

        public void OnShow(TutorialActionContext context)
        {
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
                UnityEngine.Debug.LogError($"[TutorialActionShowDialoguePop] Invalid dialogue_group_id: {actionKey}");
                return;
            }

            // DialoguePopup 표시 (완료 콜백 전달)
            SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupId, (Action)HandleDialogueCompleted)).Forget();
        }

        private static void HandleDialogueCompleted()
        {
            OnDialogueCompleted?.Invoke();
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
        }
    }
}
