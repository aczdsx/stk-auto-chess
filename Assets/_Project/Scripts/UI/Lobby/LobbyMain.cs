using System;
using System.Collections.Generic;
using System.Threading;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public enum LobbyMainRefreshType
    {
        ALL,
        STAGE,
        GUIDE_MISSION,
        CHARACTER_LAYER,
        IDLE_REWARD,
        REDDOT,
    }

    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Lobby/LobbyMain.prefab")]
    public class LobbyMain : UILayer
    {
        [SerializeField] private CAButton _playButton;
        [SerializeField] private CAButton _stageSelectButton;
        [SerializeField] private CAButton _shopButton;
        [SerializeField] private CAButton _gachaButton;
        [SerializeField] private CAButton _idleRewardButton;
        [SerializeField] private CAButton _attendanceButton;
        [SerializeField] private CAButton _questButton;
        [SerializeField] private CAButton _trialDungeonButton;
        [SerializeField] private CAButton _sessionEventButton;

        [Header("Vignette Layer")]
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private List<Color> _stageVignetteColorList;
        [SerializeField] private Material _chapter1VignetteMaterial; // [TODO] 임시 작업
        [SerializeField] private Material _defaultVignetteMaterial; // [TODO] 임시 작업

        [Header("User Info Layer")]
        //[SerializeField] private Image _userIconImage;
        [SerializeField] private TextMeshProUGUI _userNameText;
        [SerializeField] private TextMeshProUGUI _userLevelText;
        [SerializeField] private TextMeshProUGUI _userExpText;
        [SerializeField] private Slider _userExpSlider;

        [Header("Bottom Stage Select Layer")]
        [SerializeField] private ScrollRect _stageSelectScrollRect;
        [SerializeField] private GameObject _stageSelectSlotObject;
        [SerializeField] private Image _bossStageImage;
        [SerializeField] private TextMeshProUGUI _bossStageText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _stageProgressText;
        [SerializeField] private TextMeshProUGUI _apCostText;

        [Header("Guide Mission")]
        [SerializeField] private GuideMissionSlot _guideMissionSlot;

        [Header("Idle Reward Layer")]
        [SerializeField] private GameObject _normalRewardStateObject;
        [SerializeField] private Image _normalRewardFillImage;
        [SerializeField] private GameObject _fullRewardStateObject;
        [SerializeField] private TextMeshProUGUI _idleRewardStateText;

        [Header("Red dot")]
        [SerializeField] private GameObject _characterReddotObject;
        [SerializeField] private GameObject _gachaReddotObject;
        [SerializeField] private GameObject _idleRewardReddotObject;
        [SerializeField] private GameObject _chapterSelectReddotObject;

        private List<LobbyBottomStageSlot> _stageSlotList = new();

        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private bool _isIdleRewardFullState = false;

        protected override void Awake()
        {
            base.Awake();

            _playButton.onClick.AddListener(OnClickStartButton);
            _stageSelectButton.onClick.AddListener(OnClickChapterStageButton);
            _shopButton.onClick.AddListener(OnClickCharacterCollectionButton);
            _gachaButton.onClick.AddListener(OnClickGachaButton);
            _idleRewardButton.onClick.AddListener(OnClickIdleRewardButton);
            _attendanceButton.onClick.AddListener(OnClickAttendanceButton);
            _questButton.onClick.AddListener(OnClickQuestButton);
            _trialDungeonButton.onClick.AddListener(OnClickTrialDungeonButton);
            _sessionEventButton.onClick.AddListener(OnClickSeesionEventButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _playButton.onClick.RemoveListener(OnClickStartButton);
            _stageSelectButton.onClick.RemoveListener(OnClickChapterStageButton);
            _shopButton.onClick.RemoveListener(OnClickCharacterCollectionButton);
            _gachaButton.onClick.RemoveListener(OnClickGachaButton);
            _idleRewardButton.onClick.RemoveListener(OnClickIdleRewardButton);
            _attendanceButton.onClick.RemoveListener(OnClickAttendanceButton);
            _questButton.onClick.RemoveListener(OnClickQuestButton);
            _trialDungeonButton.onClick.RemoveListener(OnClickTrialDungeonButton);
            _sessionEventButton.onClick.RemoveListener(OnClickSeesionEventButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.AP);

            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.FIRST_IN, "0");

            // 전투 진행
            InGameManager.Instance.StartInGame<FlowStateStageLobbyCombat>(null);

            // 방치 보상 갱신
            SetIdleRewardLayer();

            // 레이어 갱신
            RefreshUI(LobbyMainRefreshType.ALL);

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_lobby);
        }

        public void RefreshUI(LobbyMainRefreshType refreshType)
        {
            switch (refreshType)
            {
                case LobbyMainRefreshType.ALL:
                    SetBottomStageUI();     // 하단 스테이지 UI 갱신
                    _guideMissionSlot?.RefreshGuideMissionSlot();   // 가이드 미션 갱신
                    SetUserInfoLayer();     // 유저 정보 갱신
                    CheckNewChapterClear();
                    CheckUserAccountLevelUp();
                    UpdateReddotState();
                    break;
                case LobbyMainRefreshType.STAGE:
                    SetBottomStageUI();
                    CheckNewChapterClear();
                    CheckUserAccountLevelUp();
                    break;
                case LobbyMainRefreshType.GUIDE_MISSION:
                    _guideMissionSlot?.RefreshGuideMissionSlot();
                    break;
                case LobbyMainRefreshType.CHARACTER_LAYER:
                    SetUserInfoLayer();
                    CheckUserAccountLevelUp();
                    break;
                case LobbyMainRefreshType.IDLE_REWARD:
                    _unitaskCancelToken.Cancel();
                    _unitaskCancelToken = new CancellationTokenSource();
                    SetIdleRewardLayer();
                    break;
                case LobbyMainRefreshType.REDDOT:
                    UpdateReddotState();
                    break;
            }
        }

        public void RefreshBottomStageUI()
        {
            if (_stageSlotList == null || _stageSlotList.Count <= 0) return;

            // 기본 데이터 갱신
            int currentStageId = UserDataManager.Instance.GetLastPlayStageID();

            var stageSpecData = SpecDataManager.Instance.GetStageData(currentStageId);
            var chapterSpecData = SpecDataManager.Instance.GetChapterData(stageSpecData.chapter_id, stageSpecData.difficulty_type);

            //_chapterImage.sprite = specStage.chapter_image;
            _chapterNameText.text = LanguageManager.Instance.GetLanguageText(chapterSpecData.name_token);

            int totalStageCount = SpecDataManager.Instance.GetStageCount(stageSpecData.chapter_id, DifficultyType.NORMAL);
            _stageProgressText.SetText("{0}/{1}", stageSpecData.stage_number, totalStageCount);

            // 슬롯 데이터 갱신
            _stageSlotList.ForEach(slot => slot.RefershSlot());
        }

        private void SetLobbyMainUI()
        {
            SetUserInfoLayer();
            SetBottomStageUI();
        }

        private void SetUserInfoLayer()
        {
            var userBasicData = UserDataManager.Instance.UserBasicData;

            //_userIconImage.sprite = ImageManager.Instance.GetCharacterSubIllustSprite(userBasicData.UserIconId);
            _userNameText.text = userBasicData.Nickname;

            int userLevel = SpecDataManager.Instance.GetAccountLevelByExp(userBasicData.Exp);
            _userLevelText.text = $"Lv.{userLevel}";

            var specLevelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(userLevel);
            if (specLevelData != null)
            {
                long leftExp = userBasicData.Exp - specLevelData.exp_start;
                float resultValue = leftExp / (float) specLevelData.exp_need;

                _userExpSlider.value = resultValue;
                _userExpText.text = string.Format("{0:N2}%", resultValue * 100);
            }
        }

        private void SetBottomStageUI()
        {
            ClearBottomSlotLayer();

            int currentStagdId = UserDataManager.Instance.GetLastPlayStageID();

            SpecStage specStageData = SpecDataManager.Instance.GetStageData(currentStagdId);
            var specChapterData = SpecDataManager.Instance.GetChapterDataByStageID(currentStagdId);

            var stageList = SpecDataManager.Instance.GetStageList(specChapterData.chapter_id, specChapterData.difficulty_type);

            //_chapterImage.sprite = specStage.chapter_image;
            _chapterNameText.text = LanguageManager.Instance.GetLanguageText(specChapterData.name_token);

            int totalStageCount = stageList.Count;
            _stageProgressText.SetText("{0}/{1}", specStageData.stage_number, totalStageCount);

            _apCostText.text = $"x{specStageData.need_ap}";

            for (int i = 0; i < stageList.Count; i++)
            {
                GameObject newSlotObject = Instantiate(_stageSelectSlotObject, _stageSelectScrollRect.content);
                LobbyBottomStageSlot slot = newSlotObject.GetComponent<LobbyBottomStageSlot>();
                slot.SetStageItemSlot(stageList[i]);

                _stageSlotList.Add(slot);
            }

            // 보스 스테이지 관련
            SpecStage bossStageData = SpecDataManager.Instance.GetStageData(specStageData.chapter_id, specStageData.difficulty_type, StageType.BATTLE_BOSS);
            if (bossStageData != null)
            {
                // var stageMonsterData = SpecDataManager.Instance.GetStageMonsterData(bossStageData.chapter_id, bossStageData.stage_number, bossStageData.difficulty_type);
                // var specMonsterData = SpecDataManager.Instance.GetCharacterData(stageMonsterData.monster_id);

                // _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(specMonsterData.prefab_id);
                // _bossStageText.text = $"{bossStageData.chapter_id}-{bossStageData.stage_number}";

                // 임시처리
                switch (specStageData.chapter_id)
                {
                    case 1:
                        _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(20201);
                        break;
                    case 2:
                        _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(20202);
                        break;
                    case 3:
                        _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(20203);
                        break;
                }

                _bossStageText.text = $"{bossStageData.chapter_id}-{bossStageData.stage_number}";
            }

            // Vignette 세팅
            SetVignetteColor(specChapterData.chapter_id);
        }

        private void SetVignetteColor(int targetChapter)
        {
            _vignetteImage.material = (targetChapter == 1) ? _chapter1VignetteMaterial : _defaultVignetteMaterial;
            _vignetteImage.material.SetColor("_DotColor", _stageVignetteColorList[targetChapter - 1]);
        }

        private async void SetIdleRewardLayer()
        {
            try
            {
                await CalculateIdleRewardState(_unitaskCancelToken.Token).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        private async UniTask CalculateIdleRewardState(CancellationToken cancelToken)
        {
            _isIdleRewardFullState = false;

            TimeSpan currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);
            int maxTimeLimitMinute = SpecDataManager.Instance.GetGameConfig<int>("idle_reward_acc_time_limit");
            try
            {
                while (maxTimeLimitMinute > currentRewardTimeSpan.TotalMinutes)
                {
                    _normalRewardStateObject.gameObject.SetActive(true);
                    _fullRewardStateObject.gameObject.SetActive(false);

                    float resultValue = (currentRewardTimeSpan.Minutes / (float) maxTimeLimitMinute);
                    float resultPercent = resultValue * 100;
                    _idleRewardStateText.text = $"{Mathf.Ceil(resultPercent)}%";

                    _normalRewardFillImage.fillAmount = resultValue;

                    await UniTask.Delay(1000, cancellationToken: cancelToken);

                    currentRewardTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(UserDataManager.Instance.UserIdleData.LastRewardGetTimestamp);
                }

                // 꽉 찼을경우 처리
                if (maxTimeLimitMinute <= currentRewardTimeSpan.TotalMinutes)
                {
                    _normalRewardStateObject.gameObject.SetActive(false);
                    _fullRewardStateObject.gameObject.SetActive(true);

                    _isIdleRewardFullState = true;

                    _idleRewardStateText.text = "100%";
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }

        private void CheckNewChapterClear()
        {
            if (UserDataManager.Instance.NewChapterOpenAlert == false) return;

            SceneUILayerManager.Instance.PushUILayerAsync<ChapterClearWindowPopup>().Forget();

            UserDataManager.Instance.NewChapterOpenAlert = false;
        }

        private void CheckUserAccountLevelUp()
        {
            if (UserDataManager.Instance.UserBasicData.Level <= UserDataManager.Instance.PrevAccountLevel) return;

            var specAccountLevelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(UserDataManager.Instance.UserBasicData.Level);
            if (specAccountLevelData != null)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<AccountLevelUpWindowPopup>(specAccountLevelData).Forget();

                UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;
            }
        }

        private void UpdateReddotState()
        {
            // 캐릭터 버튼 레드닷
            bool isReadyNewCharacter = false;
            var notHaveUserCharacterList = UserDataManager.Instance.GetAllNotHaveUserCharacterList();
            foreach (var userCharacter in notHaveUserCharacterList)
            {
                var specCharacterData = SpecDataManager.Instance.GetCharacterData(userCharacter.CharacterId);
                if (userCharacter.CharacterPiece >= specCharacterData.need_piece)
                {
                    isReadyNewCharacter = true;
                    break;
                }
            }

            _characterReddotObject.SetActive(isReadyNewCharacter);

            // 가챠 버튼 레드닷 (티켓이 1장 이상일 경우)
            bool isHaveGachaTicket = UserDataManager.Instance.UserWallet.CTicket > 0;
            _gachaReddotObject.SetActive(isHaveGachaTicket);

            // 방치 보상 레드닷 (가득 찼을 경우)
            _idleRewardReddotObject.SetActive(_isIdleRewardFullState);

            // 챕터 선택 레드닷
            bool isAvailGetChapterReward = false;
            var allChapterList = SpecDataManager.Instance.GetChapterList(DifficultyType.NORMAL);
            foreach (var chapterData in allChapterList)
            {
                if (isAvailGetChapterReward) break;

                int totalChapterStarCount = UserDataManager.Instance.GetTotalChapterStarCount(chapterData.chapter_id, chapterData.difficulty_type);
                if (totalChapterStarCount <= 0) continue;

                var rewardInfoList = SpecDataManager.Instance.GetSpecRewardInfoList(ContentType.STAGE_STAR, chapterData.chapter_id, chapterData.difficulty_type);
                foreach (var rewardInfoData in rewardInfoList)
                {
                    bool checkGetReward = totalChapterStarCount >= rewardInfoData.sub_value;

                    bool checkAlreadyGetReward = UserDataManager.Instance.IsGetStageAccReward(rewardInfoData.content_key_value,
                        rewardInfoData.difficulty_type, rewardInfoData.sub_value);

                    if (checkGetReward && !checkAlreadyGetReward)
                    {
                        isAvailGetChapterReward = true;
                        break;
                    }
                }
            }

            _chapterSelectReddotObject.SetActive(isAvailGetChapterReward);


            // int totalStarCount = UserDataManager.Instance.GetTotalChapterStarCount(_specRewardInfo.content_key_value, _specRewardInfo.difficulty_type);
            // _isAvailGetReward = totalStarCount >= _specRewardInfo.sub_value;
        }

        private void OnClickCommanderSkillButton()
        {
            SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(false);
            SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>(null, callbackObject =>
            {
                SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(true);
            }).Forget();
        }
        private void OnClickStartButton()
        {
            var currentStageData = SpecDataManager.Instance.GetStageData(UserDataManager.Instance.GetLastPlayStageID());
            if (currentStageData != null)
            {
                // 행동력 검사
                if (!UserDataManager.Instance.CheckEnoughItem(ItemType.AP, 0, currentStageData.need_ap, false))
                {
                    SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
                    ToastManager.Instance.ShowToastByTokenKey("MSG_GUIDE_IDLE_REWARD_AP");
                    return;
                }

                // 게임 플로우 강제성을 위한 예외처리 적용
                var userGuideMissionData = UserDataManager.Instance.GetCurrentGuideMissionData();
                if (userGuideMissionData != null)
                {
                    var specGuideMissionData = SpecDataManager.Instance.GetGuideMissionDataByOrder(userGuideMissionData.MissionId);
                    if (userGuideMissionData != null && specGuideMissionData != null)
                    {
                        if (specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_STAGE &&
                            userGuideMissionData.MissionStateType == (int)MissionStateType.REWARD)
                        {
                            ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_1");
                            return;
                        }

                        if (specGuideMissionData.guide_mission_type != GuideMissionType.CLEAR_STAGE)
                        {
                            ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_2");
                            return;
                        }
                    }
                }

                // 스테이지 진입
                InGameManager.Instance.EndInGame();
                SceneTransition_Animator transition = SceneTransition_Animator.Create();
                SceneLoading.GoToNextScene("InGame", ((int)currentStageData.chapter_id, (int)currentStageData.stage_number, currentStageData.difficulty_type), transition).Forget();

                SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            }
        }

        private void OnClickChapterStageButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            int currentStageId = UserDataManager.Instance.GetLastPlayStageID();
            SceneUILayerManager.Instance.PushUILayerAsync<ChapterListPopup>(currentStageId).Forget();
        }

        private void OnClickGachaButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget();
        }

        private void OnClickCharacterCollectionButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();
        }

        private void OnClickIdleRewardButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
        }

        private void OnClickAttendanceButton()
        {
            // 이벤트 기간 유효성 검증
            var currentSpecEventData = SpecDataManager.Instance.GetCurrentSpecEvent(EventType.ATTENDANCE);
            if (currentSpecEventData == null)
            {
                return;
            }

            // 이벤트 유저 데이터 유효성 검증
            var currentUserEventData = UserDataManager.Instance.GetUserEventData(currentSpecEventData.event_id);
            if (currentUserEventData == null)
            {
                return;
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<AttendancePopup>(currentUserEventData).Forget();
        }

        private void OnClickQuestButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<QuestPopup>().Forget();
        }

        private void OnClickTrialDungeonButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<DungeonTrialPopup>().Forget();
        }

        private void OnClickSeesionEventButton()
        {
            // 이벤트 기간 유효성 검증
            var currentSpecEventData = SpecDataManager.Instance.GetCurrentSpecEvent(EventType.ACC_PLAY_TIME);
            if (currentSpecEventData == null)
            {
                return;
            }

            // 이벤트 유저 데이터 유효성 검증
            var currentUserEventData = UserDataManager.Instance.GetUserEventData(currentSpecEventData.event_id);
            if (currentUserEventData == null)
            {
                return;
            }

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<SessionTimeEventPopup>(currentUserEventData).Forget();
        }

        private void ClearBottomSlotLayer()
        {
            _stageSlotList.Clear();

            BMUtil.RemoveChildObjects(_stageSelectScrollRect.content);
        }
    }
}
