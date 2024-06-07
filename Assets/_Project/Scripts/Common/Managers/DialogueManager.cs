using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class DialogueManager : Singleton<DialogueManager>
    {
        // 다이얼로그 이벤트 체크 후 실행
        public void UpdateDialogueEvent(DialogueEventType eventType, string subKeyValue)
        {
            int dialogueGroupID = SpecDataManager.Instance.GetDialgueGroupIDByEventType(eventType, subKeyValue);
            if (dialogueGroupID <= 0) return;

            // 다이얼로그 재생 여부 확인
            if (IsWatchedDialogueEvent(eventType, subKeyValue)) return;

            bool needSave = false;

            // 다이얼로그 팝업 생성
            switch (eventType)
            {
                case DialogueEventType.FIRST_IN:
                    SceneUILayerManager.Instance.PushUILayerAsync<DialogueShowPopup>(dialogueGroupID).Forget();
                    needSave = true;

                    break;
                case DialogueEventType.POPUP_OPEN:
                    if (SceneUILayerManager.Instance.GetUILayer(subKeyValue) != null)
                    {
                        SceneUILayerManager.Instance.PushUILayerAsync<DialogueShowPopup>(dialogueGroupID).Forget();
                        needSave = true;
                    }

                    break;
                case DialogueEventType.STAGE_CLEAR:
                    int stageID = int.Parse(subKeyValue);
                    if (UserDataManager.Instance.IsClearStage(stageID))
                    {
                        SceneUILayerManager.Instance.PushUILayerAsync<DialogueShowPopup>(dialogueGroupID).Forget();
                        needSave = true;
                    }

                    break;
            }

            // 다이얼로그 히스토리 데이터 추가 및 저장
            if (needSave)
            {
                UserDataManager.Instance.AddDialogHistory(dialogueGroupID);
            }
        }

        // 다이얼로그 재생 여부 확인
        public bool IsWatchedDialogueEvent(DialogueEventType eventType, string subKeyValue)
        {
            int dialogueGroupID = SpecDataManager.Instance.GetDialgueGroupIDByEventType(eventType, subKeyValue);
            if (dialogueGroupID > 0)
            {
                return UserDataManager.Instance.CheckDialogHistory(dialogueGroupID);
            }

            return false;
        }

        private void ShowDialogue(int dialogueGroupID)
        {

        }
    }
}
