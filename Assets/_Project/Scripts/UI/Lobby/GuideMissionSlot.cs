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
        [SerializeField] private Image _rewardCharacterBGImage;
        [SerializeField] private Image _missionRewardCharacterImage;
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
            // 가이드 미션 상태 갱신
            UserDataManager.Instance.RefreshCurrentGuideMissionData();

            // 가이드 미션 슬롯 데이터 세팅
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

            SetGuideMissionRewardImage();
            _missionRewardAmountText.text = $"x{_specGuideMissionData.item_count}";

            _activateLayerObject.SetActive(_userGuideMissionData.MissionStateType == (int)MissionStateType.REWARD);
        }

        private void SetGuideMissionRewardImage()
        {
            switch (_specGuideMissionData.item_type)
            {
                case ItemType.CHARACTER:
                    var characterData = SpecDataManager.Instance.GetCharacterData(_specGuideMissionData.item_key);
                    _missionRewardCharacterImage.sprite = ImageManager.Instance.GetCharacterSmallItemSprite(characterData.prefab_id);
                    break;
                case ItemType.CHARACTER_PIECE:
                    var characterPieceData = SpecDataManager.Instance.GetCharacterData(_specGuideMissionData.item_key);
                    _missionRewardItemImage.sprite = ImageManager.Instance.GetCharacterPieceSprite(characterPieceData.prefab_id);
                    break;
                default:
                    _missionRewardItemImage.sprite = ImageManager.Instance.GetItemSprite(_specGuideMissionData.item_type);
                    break;
            }

            _missionRewardItemImage.gameObject.SetActive(_specGuideMissionData.item_type != ItemType.CHARACTER);
            _rewardCharacterBGImage.gameObject.SetActive(_specGuideMissionData.item_type == ItemType.CHARACTER);
            _missionRewardCharacterImage.gameObject.SetActive(_specGuideMissionData.item_type == ItemType.CHARACTER);
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
                SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(rewardItemList, callback =>
                {
                    // 다음 가이드 미션 요청
                    GuideMissionManager.Instance.ChangeGuideMissionState(_specGuideMissionData.guide_mission_type, _specGuideMissionData.sub_key, MissionStateType.CLEAR);
                }).Forget();

                // 보상 데이터 저장
                UserDataManager.Instance.IncreaseRewardItemList(rewardItemList, true);

                // 다음 가이드 미션 요청
                //GuideMissionManager.Instance.ChangeGuideMissionState(_specGuideMissionData.guide_mission_type, MissionStateType.CLEAR);
            }
            else
            {
                // todo.. 별도 가이드미션 안내 처리
            }
        }
    }
}
