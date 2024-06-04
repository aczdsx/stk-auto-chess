using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GuideMissionSlot : CachedMonoBehaviour
    {
        [SerializeField] private GameObject _activateLayerObject;
        [SerializeField] private CAButton _guideMissionButton;

        [SerializeField] private TextMeshProUGUI _missionTitleText;
        [SerializeField] private TextMeshProUGUI _missionDescText;

        [SerializeField] private Image _missionRewardItemImage;
        [SerializeField] private TextMeshProUGUI _missionRewardAmountText;

        private UserGuideMission _userGuideMissionData;
        private SpecGuideMission _specGuideMissionData;

        private void Awake()
        {
            _guideMissionButton.onClick.AddListener(OnClickMissionSlotButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _guideMissionButton.onClick.RemoveListener(OnClickMissionSlotButton);
        }

        public void InitGuideMissionSlot()
        {
            int currentOrder = UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder;
            _specGuideMissionData = SpecDataManager.Instance.SpecGuideMission.Get(currentOrder);

            _userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();

            SetGuideMissionSlot();
        }

        public void RefreshGuideMissionSlot()
        {
            int currentOrder = UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder;
            _specGuideMissionData = SpecDataManager.Instance.SpecGuideMission.Get(currentOrder);

            _userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();

            SetGuideMissionSlot();
        }

        private void SetGuideMissionSlot()
        {
            if (_specGuideMissionData == null || _userGuideMissionData == null) return;

            _missionTitleText.text = _specGuideMissionData.name_token;
            _missionDescText.text = _specGuideMissionData.desc_token;

            _missionRewardItemImage.sprite = ImageManager.Instance.GetItemSprite(_specGuideMissionData.reward_type);
            _missionRewardAmountText.text = $"x{_specGuideMissionData.reward_amount}";

            _activateLayerObject.SetActive(_userGuideMissionData.MissionStateType == (int)MissionStateType.REWARD);
        }

        private void OnClickMissionSlotButton()
        {

        }
    }
}
