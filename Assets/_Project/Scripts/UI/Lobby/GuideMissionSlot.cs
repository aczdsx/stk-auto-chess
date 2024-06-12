using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
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

            // 다이얼로그 팝업 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, _specGuideMissionData.id.ToString());
        }

        public void RefreshGuideMissionSlot()
        {
            int currentOrder = UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder;
            _specGuideMissionData = SpecDataManager.Instance.SpecGuideMission.Get(currentOrder);

            _userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();

            SetGuideMissionSlot();

            // 다이얼로그 팝업 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, _specGuideMissionData.id.ToString());
        }

        private void SetGuideMissionSlot()
        {
            if (_specGuideMissionData == null || _userGuideMissionData == null) return;

            _missionTitleText.text = LanguageManager.Instance.GetLanguageText(_specGuideMissionData.name_token);
            _missionDescText.text = LanguageManager.Instance.GetLanguageText(_specGuideMissionData.desc_token);

            _missionRewardItemImage.sprite = ImageManager.Instance.GetItemSprite(_specGuideMissionData.item_type);
            _missionRewardAmountText.text = $"x{_specGuideMissionData.item_count}";

            _activateLayerObject.SetActive(_userGuideMissionData.MissionStateType == (int)MissionStateType.REWARD);
        }

        private void OnClickMissionSlotButton()
        {
            if (_userGuideMissionData == null) return;
            if (_specGuideMissionData == null) return;

            // 보상을 받을 수 있는 경우
            if (_userGuideMissionData.MissionStateType == (int)MissionStateType.REWARD)
            {
                // 보상 수령 처리
                List<RewardItem> rewardItemList = new List<RewardItem>();
                rewardItemList.Add(new RewardItem(_specGuideMissionData.item_type, _specGuideMissionData.item_key, _specGuideMissionData.item_count));
                SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(rewardItemList).Forget();

                // 보상 데이터 저장
                UserDataManager.Instance.IncreaseRewardItemList(rewardItemList, true);

                // 다음 가이드 미션 요청
                GuideMissionManager.Instance.ChangeGuideMissionState(_specGuideMissionData.guide_mission_type, MissionStateType.CLEAR);
            }
            else
            {
                // todo.. 별도 가이드미션 안내 처리
            }
        }
    }
}
