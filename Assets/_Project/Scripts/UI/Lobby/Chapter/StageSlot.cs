using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class StageSlot : CachedMonoBehaviour
    {
        [SerializeField] private TMP_Text stageNameText;
        [SerializeField] private GameObject[] starObjs;
        [SerializeField] private CAButton enterBtn;

        private int chapter;
        private int stageIndex;

        protected void Awake()
        {
            enterBtn.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickEnter()).AddTo(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        internal void SetStageData(int chapter, int stageIndex)
        {
            // this.chapter = chapter;
            // this.stageIndex = stageIndex;
            // //StageInfo specStage = SpecDataManager.Instance.GetStageInfo(chapter, stageIndex);
            // UserStage userStage = UserDataManager.Instance.GetUserStage(specStage.stage_id);
            // stageNameText.SetText("{0}-{1}", chapter, stageIndex + 1);
            // for (var i = 0; i < starObjs.Length; i++)
            // {
            //     starObjs[i].SetActive(i < userStage.StarCount);
            // }
        }

        private void OnClickEnter()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<StageDetailPopup>((chapter, stageIndex)).Forget();
        }
    }
}
