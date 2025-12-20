using System;
using System.Collections;
using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CookApps.BattleSystem;

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
        private GuideMissionInfo _specGuideMissionData;

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
            _specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(currentOrder);

            _userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();

            SetGuideMissionSlot();

            // 다이얼로그 팝업 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, _specGuideMissionData.id.ToString());
        }

        public void RefreshGuideMissionSlot()
        {
            int currentOrder = UserDataManager.Instance.UserMissionData.GuideMissionCurrentOrder;

            // 가이드 미션 최대 오더일 경우 off 처리
            if (currentOrder > SpecDataManager.Instance.GetGuideMissionMaxOrder())
            {
                gameObject.SetActive(false);
                return;
            }

            // 가이드 미션 상태 갱신
            UserDataManager.Instance.RefreshCurrentGuideMissionData();

            // 가이드 미션 슬롯 데이터 세팅
            _specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(currentOrder);

            _userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();

            SetGuideMissionSlot();

            // 다이얼로그 팝업 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.GUIDE_START, _specGuideMissionData.id.ToString(),
                () =>
                {
                    if (_specGuideMissionData.id == 1)
                        SceneUILayerManager.Instance.PushUILayerAsync<NicknamePopup>(true).Forget();
                });
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
                SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList), callback =>
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
                if (_specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_STAGE)
                {
                    GuideMissionInfo specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get(_specGuideMissionData.id);

                    StageInfo guideStageData = SpecDataManager.Instance.GetStageData(_specGuideMissionData.sub_key);
                    StageInfo currentStageData = SpecDataManager.Instance.GetStageData(UserDataManager.Instance.GetLastPlayStageID());

                    bool isMatchChapter = guideStageData.chapter_id == currentStageData.chapter_id;

                    if (specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_STAGE && !isMatchChapter)
                    {
                        // 스테이지 데이터 세팅
                        var lastestStageID = UserDataManager.Instance.GetLatestClearUserStageID();
                        var lastestSpecStageData = SpecDataManager.Instance.GetStageData(lastestStageID);
                        var nextStageData = SpecDataManager.Instance.GetNextStageData(lastestStageID);

                        // 가장 최신 챕터를 확인하고 플레이 가능한 최대 스테이지 넘버로 이동
                        int targetStageNumber = 1;
                        if (lastestSpecStageData != null && lastestSpecStageData.chapter_id == guideStageData.chapter_id)
                        {
                            if (nextStageData != null)
                            {
                                targetStageNumber = nextStageData.stage_number;
                            }
                        }
                        // 스테이지 데이터 세팅
                        var targetSpecStage = SpecDataManager.Instance.GetStageData(nextStageData.chapter_id, targetStageNumber, nextStageData.difficulty_type);
                        UserDataManager.Instance.SetLastPlayStageID(targetSpecStage.stage_id, true);


                        // 로비 배경 전환
                        InGameManager.Instance.EndInGame();
                        // 로비 배경 전환 및 챕터 이동
                        SceneTransition.Create<SceneTransition_FadeInOut>();
                        SceneTransition.FadeInAsync().Forget();
                        SceneLoading.GoToNextScene("Lobby", guideStageData.chapter_id);

                        // 로비 메인 하단 스테이지 UI 갱신
                        var battleReadyMain = SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
                        if (battleReadyMain != null)
                        {
                            battleReadyMain.RefreshUI(LobbyMainRefreshType.STAGE);
                        }

                        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
                        return;
                    }
                }
                GuideMissionManager.Instance.UpdateGuideMissionAlert();
            }
        }
    }
}
