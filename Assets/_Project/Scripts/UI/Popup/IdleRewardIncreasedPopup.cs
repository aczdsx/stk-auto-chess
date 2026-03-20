using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class IdleRewardIncreasedPopup : UILayerPopupBase
    {
        [SerializeField] private TextMeshProUGUI _coinText;
        [SerializeField] private TextMeshProUGUI _expText;
        [SerializeField] private TextMeshProUGUI _exp2Text;
        [SerializeField] private TextMeshProUGUI _apText;
        
        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
            
            var latestStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
            var lastStageData = latestStageID > 0 ? SpecDataManager.Instance.GetStageData(latestStageID) : null;
            if (lastStageData == null) return;
            int totalStageClearCount = ServerDataManager.Instance.Battle.ClearedStageCount + 1;

            var specIdleRewardList = SpecDataManager.Instance.GetAllIdleRewardList(lastStageData.chapter_id);

            foreach (var item in specIdleRewardList)
            {
                double baseAmount = item.min_count;
                double addAmount = item.add_count * (double)totalStageClearCount;
                double totalAmount = (baseAmount + addAmount) * 60;
                
                if (item.item_id == IdMap.Item.ActionPoint)
                {
                    _apText.text = "+" + totalAmount.ToString("n0");
                }
                else if (item.item_id == IdMap.Item.Gold)
                {
                    _coinText.text = "+" + totalAmount.ToString("n0");
                }
                else if (item.item_id == IdMap.Item.CharExp)
                {
                     _expText.text = "+" + totalAmount.ToString("n0");
                    // _exp2Text.text = "+" + totalAmount.ToString("n0");
                }
            }
        }
    }
}
