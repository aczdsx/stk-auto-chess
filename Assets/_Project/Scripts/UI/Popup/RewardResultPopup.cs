using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class RewardResultPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _okButton;
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header("Reward Slot Layer")]
        [SerializeField] private GameObject _rewardSlotListLayerObject;
        [SerializeField] private GameObject _rewardItemSlotObject;

        private List<RewardItem> _rewardItemList;
        private string _titleToken;

        protected override void Awake()
        {
            base.Awake();

            _okButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickOkButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_item_reward);

            (_titleToken, _rewardItemList) = ((string, List<RewardItem>))param;
            
            SetPopupTitleText(_titleToken);
            
            SetRewardSlotList();
        }

        public void SetPopupTitleText(string text)
        {
            if (_titleText == null) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            
            _titleText.text = LanguageManager.Instance.GetDefaultText(text);
        }

        private void SetRewardSlotList()
        {
            if (_rewardItemList == null || _rewardItemList.Count == 0) return;

            ClearRewardSlotList();

            foreach (var rewardItem in _rewardItemList)
            {
                var rewardItemSlot = Instantiate(_rewardItemSlotObject, _rewardSlotListLayerObject.transform);
                rewardItemSlot.GetComponent<RewardItemSlot>().SetRewardSlot(rewardItem);
            }
        }

        private void OnClickOkButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }


        private void ClearRewardSlotList()
        {
            BMUtil.RemoveChildObjects(_rewardSlotListLayerObject.transform);
        }
    }
}
