using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        // 다이얼로그 이벤트 체크 후 실행
        public void UpdateDialogueEvent(DialogueEventType eventType, string subKeyValue, Action onComplete = null)
        {
            int dialogueGroupID = SpecDataManager.Instance.GetDialgueGroupIDByEventType(eventType, subKeyValue);
            if (dialogueGroupID <= 0) return;

            // 다이얼로그 재생 여부 확인
            if (IsWatchedDialogueEvent(eventType, subKeyValue)) return;

            // 현재 다이얼로그 팝업이 켜져 있는 상태인지 확인
            var dialoguePopup = SceneUILayerManager.Instance.GetUILayer<DialoguePopup>();
            if (dialoguePopup != null)
            {
                return;
            }

            // 다이얼로그 팝업 생성
            switch (eventType)
            {
                case DialogueEventType.FIRST_IN:
                    SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupID, onComplete)).Forget();
                    break;
                case DialogueEventType.POPUP_OPEN:
                    if (SceneUILayerManager.Instance.GetUILayer(subKeyValue) != null)
                    {
                        SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupID, onComplete)).Forget();
                    }
                    break;
                case DialogueEventType.GUIDE_START:
                    var guideMissionModel = ServerDataManager.Instance.GuideMission;
                    var specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get((int)guideMissionModel.GuideMissionId);
                    if (specGuideMissionData != null && specGuideMissionData.id.ToString().Equals(subKeyValue))
                    {
                        SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupID, onComplete)).Forget();
                    }
                    break;
                case DialogueEventType.STAGE_CLEAR:
                    SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupID, onComplete)).Forget();
                    break;
                case DialogueEventType.STAGE_START:
                    SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupID, onComplete)).Forget();
                    break;
                case DialogueEventType.FAIL:
                    SceneUILayerManager.Instance.PushUILayerAsync<DialoguePopup>((dialogueGroupID, onComplete)).Forget();
                    break;
            }
        }

        // 다이얼로그 재생 여부 확인
        public bool IsWatchedDialogueEvent(DialogueEventType eventType, string subKeyValue)
        {
            int dialogueGroupID = SpecDataManager.Instance.GetDialgueGroupIDByEventType(eventType, subKeyValue);
            if (dialogueGroupID > 0)
            {
                return ClientProgressData.Get().HasDialogueId(dialogueGroupID);
            }

            return false;
        }

        private void ShowDialogue(int dialogueGroupID)
        {

        }
    }
}
