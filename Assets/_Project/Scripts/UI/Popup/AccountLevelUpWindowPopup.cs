using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/AccountLevelUpWindowPopup.prefab")]
    public class AccountLevelUpWindowPopup : UILayer
    {
        [SerializeField] private CAButton _getRewardButton;

        [Header("Base Layer")]
        [SerializeField] private TextMeshProUGUI _currentLevelText;
        [SerializeField] private TextMeshProUGUI _currentAPCountText;
        [SerializeField] private TextMeshProUGUI _currentBattleDeckCountText;

        [Space(10)]
        [SerializeField] private TextMeshProUGUI _prevLevelText;
        [SerializeField] private TextMeshProUGUI _prevAPCountText;
        [SerializeField] private TextMeshProUGUI _prevBattleDeckCountText;

        [Header("Reward Layer")]
        [SerializeField] private GameObject _rewardGridLayerObject;
        [SerializeField] private GameObject _rewardSlotObject;

        private SpecAccountLevelExp _currentSpecAccountLevelExpData;
        private SpecAccountLevelExp _prevSpecAccountLevelExpData;

        protected override void Awake()
        {
            base.Awake();

            _getRewardButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _getRewardButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_account_levelup);

            _currentSpecAccountLevelExpData = param as SpecAccountLevelExp;
            _prevSpecAccountLevelExpData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(_currentSpecAccountLevelExpData.lv - 1);

            SetAccountLevelUpPopup();
        }

        private void SetAccountLevelUpPopup()
        {
            if (_currentSpecAccountLevelExpData == null) return;
            if (_prevSpecAccountLevelExpData == null) return;

            ClearPopup();

            _prevLevelText.text = _prevSpecAccountLevelExpData.lv.ToString();
            _prevAPCountText.text = _prevSpecAccountLevelExpData.ap_max.ToString();
            _prevBattleDeckCountText.text = _prevSpecAccountLevelExpData.squad_count.ToString();

            _currentLevelText.text = _currentSpecAccountLevelExpData.lv.ToString();
            _currentAPCountText.text = _currentSpecAccountLevelExpData.ap_max.ToString();
            _currentBattleDeckCountText.text = _currentSpecAccountLevelExpData.squad_count.ToString();

            // 보상 데이터 세팅
            var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(_currentSpecAccountLevelExpData.reward_id);
            var rewardItemList = SpecDataManager.Instance.GetRewardItemListByRewadInfoList(rewardInfoList);
            foreach (var rewardItem in rewardItemList)
            {
                GameObject newRewardSlot = Instantiate(_rewardSlotObject, _rewardGridLayerObject.transform);
                RewardItemSlot rewardItemSlot = newRewardSlot.GetComponent<RewardItemSlot>();

                rewardItemSlot.SetRewardItem(rewardItem);
            }

            // 보상 데이터 저장
            UserDataManager.Instance.IncreaseRewardItemList(rewardItemList, true);
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_confirm);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearPopup()
        {
            BMUtil.RemoveChildObjects(_rewardGridLayerObject.transform);
        }
    }
}
