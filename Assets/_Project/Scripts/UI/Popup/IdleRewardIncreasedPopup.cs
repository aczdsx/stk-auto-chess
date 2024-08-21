using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/IdleRewardIncreasedPopup.prefab")]
    public class IdleRewardIncreasedPopup : UILayer
    {
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private TextMeshProUGUI _expText;
        [SerializeField] private TextMeshProUGUI _exp2Text;
        [SerializeField] private TextMeshProUGUI _apText;
        
        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
            
            var latestStageID = UserDataManager.Instance.GetLatestClearUserStageID();
            var lastStageData = SpecDataManager.Instance.GetStageData(latestStageID);
            int totalStageClearCount = UserDataManager.Instance.GetAllClearUserStageList().Count + 1;
            
            var specIdleRewardList = SpecDataManager.Instance.GetAllIdleRewardList(lastStageData.chapter_id);

            foreach (var item in specIdleRewardList)
            {
                double baseAmount = item.min_count;
                double addAmount = item.add_count * (double)totalStageClearCount;
                double totalAmount = (baseAmount + addAmount) * 60;
                
                switch (item.item_type)
                {
                    case ItemType.AP:
                        _apText.text = "+" + totalAmount.ToString("n0");
                        break;
                    case ItemType.GOLD:
                        _coinText.text = "+" + totalAmount.ToString("n0");
                        break;
                    case ItemType.CHAR_USER_EXP_ITEM:
                        _expText.text = "+" + totalAmount.ToString("n0");
                        break;
                    case ItemType.CHAR_USER_EXP_ITEM_2:
                        _exp2Text.text = "+" + totalAmount.ToString("n0");
                        break;
                }
            }
        }
    }
}
