using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/RewardResultPopup.prefab")]
    public class RewardResultPopup : UILayer
    {
        [SerializeField] private CAButton _okButton;

        [Header("Reward Slot Layer")]
        [SerializeField] private GameObject _rewardSlotListLayerObject;
        [SerializeField] private GameObject _rewardItemSlotObject;

        private List<RewardItem> _rewardItemList;

        protected override void Awake()
        {
            base.Awake();

            _okButton.onClick.AddListener(OnClickOkButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _okButton.onClick.RemoveListener(OnClickOkButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            _rewardItemList = param as List<RewardItem>;

            SetRewardSlotList();
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
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearRewardSlotList()
        {
            BMUtil.RemoveChildObjects(_rewardSlotListLayerObject.transform);
        }
    }
}
