using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/IdleRewardPopup.prefab")]
    public class IdleRewardPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _getRewardButton;

        [Space]
        [SerializeField] private GameObject _rewardGridLayerObject;
        [SerializeField] private GameObject _rewardSlotObject;

        private List<RewardItem> _currentIdleRewardItemList = new List<RewardItem>();
        private List<RewardItemSlot> _currentIdleRewardItemSlotList = new List<RewardItemSlot>();

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _getRewardButton.onClick.AddListener(OnClickGetRewardButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            InitIdleRewardData();
        }

        private void InitIdleRewardData()
        {
            _currentIdleRewardItemList = UserDataManager.Instance.GetCurrentIdleRewardItemList();

            SetRewardItemSlot();
        }

        private void RefreshIdleRewardData()
        {
            _currentIdleRewardItemList = UserDataManager.Instance.GetCurrentIdleRewardItemList();

            SetRewardItemSlot();
        }

        private void SetRewardItemSlot()
        {
            if (_currentIdleRewardItemList == null || _currentIdleRewardItemList.Count <= 0) return;

            ClearSlot();

            foreach (var idleReward in _currentIdleRewardItemList)
            {
                GameObject newRewardSlot = Instantiate(_rewardSlotObject, _rewardGridLayerObject.transform);
                RewardItemSlot rewardItemSlot = newRewardSlot.GetComponent<RewardItemSlot>();

                rewardItemSlot.SetRewardItem(idleReward);

                _currentIdleRewardItemSlotList.Add(rewardItemSlot);
            }
        }

        private void OnClickGetRewardButton()
        {
            RefreshIdleRewardData();

            // 수령 가능한 보상이 있는지 확인
            if (_currentIdleRewardItemList == null || _currentIdleRewardItemList.Count <= 0)
            {
                return;
            }

            // 보상 수령
            UserDataManager.Instance.IncreaseRewardItemList(_currentIdleRewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(_currentIdleRewardItemList).Forget();

            // 방치 보상 데이터 갱신
            UserDataManager.Instance.RefreshLastRewardGetTime();

            // temp - 일단은 off
            OnClickCloseButton();
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearSlot()
        {
            BMUtil.RemoveChildObjects(_rewardGridLayerObject.transform);

            _currentIdleRewardItemSlotList.Clear();
        }
    }
}
