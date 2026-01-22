using System;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 다이얼로그 팝업 표시 튜토리얼 액션.
    /// tutorial_action_key에 지정된 dialogue_group_id로 DialoguePopup을 표시합니다.
    /// 다이얼로그 완료 후 다음 튜토리얼 진행은 DialoguePopup에서 HandleTutorialAction(DIALOGUE_POP_END)로 처리됩니다.
    ///
    /// tutorial_action_key: dialogue_group_id (다이얼로그 그룹 ID)
    /// </summary>
    public class TutorialActionShowDialoguePop : ITutorialActionStrategy
    {
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

            // DialoguePopup 표시 (콜백 없이 - DIALOGUE_POP_END 트리거로 다음 튜토리얼 진행)
            SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupId, (Action)null)).Forget();
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
            if (context.TutorialCanvas != null)
            {
                context.TutorialCanvas.enabled = true;
            }
        }
    }
}
