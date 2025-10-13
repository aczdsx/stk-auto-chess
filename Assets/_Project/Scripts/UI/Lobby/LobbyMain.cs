using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

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
        public Transform GetIdleRewardTransform => _idleRewardButton.transform;

        [SerializeField] private CAButton _playButton;
        [SerializeField] private CAButton _stageSelectButton;
        [SerializeField] private CAButton _shopButton;
        [SerializeField] private CAButton _gachaButton;
        [SerializeField] private CAButton _idleRewardButton;
        [SerializeField] private CAButton _attendanceButton;
        [SerializeField] private CAButton _questButton;
        [SerializeField] private CAButton _trialDungeonButton;
        [SerializeField] private CAButton _sessionEventButton;
        [SerializeField] private CAButton _consumeAPEventButton;
        [SerializeField] private CAButton _EnterArenaButton;
        [SerializeField] private CAButton _userAccountLayerButton;
        [SerializeField] private CAButton _settingButton;

        [Header("Vignette Layer")]
        [SerializeField] private VignetteSO _vignetteData;
        [SerializeField] private RawImage _vignetteImage;

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
        [SerializeField] private ParticleSystem _dropFx;

        [Header("Red dot")]
        [SerializeField] private GameObject _characterReddotObject;
        [SerializeField] private GameObject _gachaReddotObject;
        [SerializeField] private GameObject _idleRewardReddotObject;
        [SerializeField] private GameObject _chapterSelectReddotObject;
        [SerializeField] private GameObject _questReddotObject;
        [SerializeField] private GameObject _attendanceReddotObject;
        [SerializeField] private GameObject _sessionTimeEventReddotObject;
        [SerializeField] private GameObject _useAPEventReddotObject;
        [SerializeField] private GameObject _trialDungeonReddotObject;
        [SerializeField] private GameObject _pvpArenaReddotObject;

        private List<LobbyBottomStageSlot> _stageSlotList = new();

        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private bool _isIdleRewardFullState = false;

        public static LobbyMain GetLobbyMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        }

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
            _sessionEventButton.onClick.AddListener(OnClickSessionEventButton);
            _consumeAPEventButton.onClick.AddListener(OnClickConsumeAPEventButton);
            _EnterArenaButton.onClick.AddListener(OnClickEnterArenaButton);
            _userAccountLayerButton.onClick.AddListener(OnClickUserAccountLayerButton);
            _settingButton.onClick.AddListener(OnClickSettingButton);
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
            _sessionEventButton.onClick.RemoveListener(OnClickSessionEventButton);
            _consumeAPEventButton.onClick.RemoveListener(OnClickConsumeAPEventButton);
            _EnterArenaButton.onClick.RemoveListener(OnClickEnterArenaButton);
            _userAccountLayerButton.onClick.RemoveListener(OnClickUserAccountLayerButton);
            _settingButton.onClick.RemoveListener(OnClickSettingButton);
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.AP);

            // DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.FIRST_IN, "0", () =>
            // {
            // });

            // 전투 진행
            int currentStageId = UserDataManager.Instance.GetLastPlayStageID();
            var stageSpecData = SpecDataManager.Instance.GetStageData(currentStageId);
            InGameManager.Instance.StartInGame<FlowStateLobbyCombat>(stageSpecData);

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
                    UpdateAttendanceData();

                    UpdateReddotState();
                    UpdateOpenCondition();
                    CheckShowSurveyPopup();
                    CheckShopBannerPopup();
                    break;
                case LobbyMainRefreshType.STAGE:
                    SetBottomStageUI();
                    CheckNewChapterClear();
                    CheckUserAccountLevelUp();
                    break;
                case LobbyMainRefreshType.GUIDE_MISSION:
                    _guideMissionSlot?.RefreshGuideMissionSlot();
                    UpdateOpenCondition();
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
            _userNameText.text = (userBasicData.Nickname.Length > 25) ? "닉네임을 설정해주세요." : userBasicData.Nickname;

            int userLevel = SpecDataManager.Instance.GetAccountLevelByExp(userBasicData.Exp);
            _userLevelText.text = $"Lv.{userLevel}";

            var specLevelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel(userLevel);
            if (specLevelData != null)
            {
                long leftExp = userBasicData.Exp - specLevelData.exp_start;
                float resultValue = leftExp / (float)specLevelData.exp_need;

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

            RectTransform currentStageRect = null;
            for (int i = 0; i < stageList.Count; i++)
            {
                GameObject newSlotObject = Instantiate(_stageSelectSlotObject, _stageSelectScrollRect.content);
                LobbyBottomStageSlot slot = newSlotObject.GetComponent<LobbyBottomStageSlot>();

                bool isCurrentStage = stageList[i].stage_id == currentStagdId;

                slot.SetStageItemSlot(stageList[i], isCurrentStage);

                _stageSlotList.Add(slot);

                if (isCurrentStage)
                {
                    currentStageRect = newSlotObject.GetComponent<RectTransform>();
                }
            }

            // 보스 스테이지 관련
            SpecStage bossStageData = SpecDataManager.Instance.GetStageData(specStageData.chapter_id, specStageData.difficulty_type, StageType.BATTLE_BOSS);
            if (bossStageData != null)
            {
                // var stageMonsterData = SpecDataManager.Instance.GetStageMonsterData(bossStageData.chapter_id, bossStageData.stage_number, bossStageData.difficulty_type);
                // var specMonsterData = SpecDataManager.Instance.GetCharacterData(stageMonsterData.monster_id);

                // _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(specMonsterData.prefab_id);
                // _bossStageText.text = $"{bossStageData.chapter_id}-{bossStageData.stage_number}";

                // 보스 이미지 처리
                _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(specStageData.chapter_id);

                _bossStageText.text = $"{bossStageData.chapter_id}-{bossStageData.stage_number}";
            }

            // Vignette 세팅
            SetVignetteColor(specChapterData.chapter_id);

            // 현재 스테이지 슬롯을 뷰포트 중앙으로 정렬
            if (currentStageRect != null)
            {
                CenterOnStageSlot(currentStageRect);
            }
        }

        private void CenterOnStageSlot(RectTransform target)
        {
            if (_stageSelectScrollRect == null || target == null) return;

            var scrollRect = _stageSelectScrollRect;
            var content = scrollRect.content;
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;

            // 레이아웃 강제 갱신 후 계산
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            // 타겟과 콘텐츠의 월드 코너를 콘텐츠 로컬 공간으로 가져옴
            Vector3[] itemWorldCorners = new Vector3[4];
            Vector3[] contentWorldCorners = new Vector3[4];
            target.GetWorldCorners(itemWorldCorners);
            content.GetWorldCorners(contentWorldCorners);

            // 콘텐츠 로컬 좌표계로 변환
            for (int i = 0; i < 4; i++)
            {
                itemWorldCorners[i] = content.InverseTransformPoint(itemWorldCorners[i]);
                contentWorldCorners[i] = content.InverseTransformPoint(contentWorldCorners[i]);
            }

            float contentWidth = content.rect.width;
            float contentHeight = content.rect.height;
            float viewportWidth = viewport.rect.width;
            float viewportHeight = viewport.rect.height;

            // 콘텐츠의 좌측/상단 기준치 (pivot 보정)
            float contentLeft = -content.rect.width * content.pivot.x;
            float contentTop = content.rect.height * (1f - content.pivot.y);

            // 아이템 중심 좌표 (콘텐츠 로컬)
            float itemLeft = itemWorldCorners[0].x;
            float itemRight = itemWorldCorners[3].x;
            float itemBottom = itemWorldCorners[0].y;
            float itemTop = itemWorldCorners[1].y;
            float itemCenterX = (itemLeft + itemRight) * 0.5f;
            float itemCenterY = (itemTop + itemBottom) * 0.5f;

            // 좌측으로부터의 거리 (pivot 0 보정값 사용)
            float itemCenterFromLeft = itemCenterX - contentLeft;
            // 상단으로부터의 거리 (Unity는 위가 +Y 이므로 top 기준 계산)
            float itemCenterFromTop = contentTop - itemCenterY;

            if (scrollRect.horizontal && contentWidth > viewportWidth)
            {
                float targetLeftForCenter = itemCenterFromLeft - viewportWidth * 0.5f;
                float maxLeft = contentWidth - viewportWidth; // 스크롤 가능한 최대치
                float normalized = Mathf.Clamp01(targetLeftForCenter / Mathf.Max(1f, maxLeft));
                scrollRect.horizontalNormalizedPosition = normalized;
            }

            if (scrollRect.vertical && contentHeight > viewportHeight)
            {
                // ScrollRect의 verticalNormalizedPosition은 1=top, 0=bottom 기준
                float targetTopForCenter = itemCenterFromTop - viewportHeight * 0.5f;
                float maxTop = contentHeight - viewportHeight;
                float normalized = 1f - Mathf.Clamp01(targetTopForCenter / Mathf.Max(1f, maxTop));
                scrollRect.verticalNormalizedPosition = normalized;
            }

            Canvas.ForceUpdateCanvases();
        }

        private void SetVignetteColor(int targetChapter)
        {
            var vignette = _vignetteData.stageColors.FirstOrDefault(x => x.InGameType == InGameType.STAGE && x.ID == targetChapter);
            _vignetteImage.material = vignette.Material;
            _vignetteImage.material.SetColor("_DotColor", vignette.Color);
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

                    float resultValue = (float)currentRewardTimeSpan.TotalMinutes / maxTimeLimitMinute;
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

        // 상점 배너 팝업 노출 여부 체크
        private void CheckShopBannerPopup()
        {
            ShopPurchaseManager.Instance.ShowShopBannerPopup(ShopBannerShowType.LOBBY);
        }

        // 설문 팝업 노출 여부 체크
        private void CheckShowSurveyPopup()
        {
            bool isShowSurvey = Preference.LoadPreference(Pref.SHOW_SURVEY_POPUP, 0) == 1;
            if (isShowSurvey) return;

            int surveyVisitCount = SpecDataManager.Instance.GetGameConfig<int>("USER_SURVEY_NOTI_ACC_DATE");
            if (UserDataManager.Instance.UserBasicData.DailyVisitCount >= surveyVisitCount)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<EndTestgamePopup>().Forget();

                Preference.SavePreference(Pref.SHOW_SURVEY_POPUP, 1);
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

        // 출석부 상태 갱신
        private void UpdateAttendanceData()
        {
            var specEventData = SpecDataManager.Instance.GetSpecEventData(EventType.ATTENDANCE);
            var currentUserEventData = UserDataManager.Instance.GetUserEventData(specEventData.event_id);
            if (currentUserEventData != null)
            {
                if (currentUserEventData.EventExtraRefreshTimestamp < TimeManager.Instance.UtcNowTimeStampLocal())
                {
                    UserDataManager.Instance.SetUserEventActionCount(currentUserEventData.EventId, 1, true, true);
                    UserDataManager.Instance.UpdateEventTimeData(currentUserEventData.EventId);
                }
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

            // 가챠 버튼 레드닷 (티켓이 1장 이상일 경우) -> 10개 이상으로 변경
            bool isHaveGachaTicket = UserDataManager.Instance.UserWallet.CTicket >= 10;
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

            // 퀘스트 레드닷
            bool isAvailDailyQuestReward = false;
            bool isAvailWeeklyQuestReward = false;
            var dailyQuestList = SpecDataManager.Instance.GetSpecQuestList(TermType.DAILY, true);
            var weeklyQuestList = SpecDataManager.Instance.GetSpecQuestList(TermType.WEEKLY, true);

            foreach (var questData in dailyQuestList)
            {
                var userQuestData = UserDataManager.Instance.GetUserQuestData(questData.quest_id);
                if (userQuestData == null) continue;

                if (userQuestData.QuestStateType == (int)QuestStateType.REWARD)
                {
                    isAvailDailyQuestReward = true;
                    break;
                }
            }

            foreach (var questData in weeklyQuestList)
            {
                var userQuestData = UserDataManager.Instance.GetUserQuestData(questData.quest_id);
                if (userQuestData == null) continue;

                if (userQuestData.QuestStateType == (int)QuestStateType.REWARD)
                {
                    isAvailWeeklyQuestReward = true;
                    break;
                }
            }

            _questReddotObject.SetActive(isAvailDailyQuestReward || isAvailWeeklyQuestReward);

            // 출석 레드닷
            bool isAvailAttendanceReward = false;
            var specEventData = SpecDataManager.Instance.GetSpecEventData(EventType.ATTENDANCE);
            if (specEventData != null)
            {
                var userEventConditionList = UserDataManager.Instance.GetUserEventConditionDataList(specEventData.event_id);
                if (userEventConditionList != null && userEventConditionList.Count > 0)
                {
                    isAvailAttendanceReward = userEventConditionList.Exists(data => data.EventStateType == (int)EventStateType.REWARD);
                }

                _attendanceReddotObject.SetActive(isAvailAttendanceReward);
            }

            // 세션타임 이벤트 레드닷
            bool isAvailSessionTimeEventReward = false;
            var specSessionEventData = SpecDataManager.Instance.GetSpecEventData(EventType.ACC_PLAY_TIME);
            var userSessionEventConditionList = UserDataManager.Instance.GetUserEventConditionDataList(specSessionEventData.event_id);
            if (userSessionEventConditionList != null && userSessionEventConditionList.Count > 0)
            {
                isAvailSessionTimeEventReward = userSessionEventConditionList.Exists(data => data.EventStateType == (int)EventStateType.REWARD);
            }

            _sessionTimeEventReddotObject.SetActive(isAvailSessionTimeEventReward);


            // 행동력 이벤트 레드닷
            bool isAvailUseAPReward = false;
            var specUseAPEventData = SpecDataManager.Instance.GetSpecEventData(EventType.USE_AP);
            var userUseAPEventConditionList = UserDataManager.Instance.GetUserEventConditionDataList(specUseAPEventData.event_id);
            if (userUseAPEventConditionList != null && userUseAPEventConditionList.Count > 0)
            {
                isAvailUseAPReward = userUseAPEventConditionList.Exists(data => data.EventStateType == (int)EventStateType.REWARD);
            }

            _useAPEventReddotObject.SetActive(isAvailUseAPReward);

            // 시련 던전 레드닷
            bool isAvailPlayTrialDungeon = false;

            int totalStageStar = UserDataManager.Instance.GetAllTotalChapterStarCount();
            var trialDungeonList = SpecDataManager.Instance.GetSpecDungeonTrialDataListByStageStar(totalStageStar);
            if (trialDungeonList != null && trialDungeonList.Count > 0)
            {
                foreach (var dungeonData in trialDungeonList)
                {
                    var userDungeonData = UserDataManager.Instance.GetTrialDungeonData(dungeonData.dungeon_id);
                    if (userDungeonData == null) continue;

                    if (userDungeonData.DungeonStateType == (int)DungeonStateType.WAIT)
                    {
                        isAvailPlayTrialDungeon = true;
                        break;
                    }
                }
            }

            _trialDungeonReddotObject.SetActive(isAvailPlayTrialDungeon);

            // 아레나 PVP 레드닷
            bool isAvailPlayPVP = false;

            isAvailPlayPVP = UserDataManager.Instance.UserWallet.PvpTicket > 0;

            _pvpArenaReddotObject.SetActive(isAvailPlayPVP);
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
                            if (specGuideMissionData.guide_mission_type == GuideMissionType.CLEAR_TRIAL)
                            {
                                if (specGuideMissionData.sub_key == 10001 || specGuideMissionData.sub_key == 10004)
                                {
                                    ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_2");
                                    GuideMissionManager.Instance.UpdateGuideMissionAlert();
                                    return;
                                }
                            }
                            else
                            {
                                ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_2");
                                GuideMissionManager.Instance.UpdateGuideMissionAlert();
                                return;
                            }
                        }
                    }
                }

                // 스테이지 진입
                InGameManager.Instance.EndInGame();
                SceneTransition_Animator transition = SceneTransition_Animator.Create();
                SceneLoading.GoToNextScene("InGame",
                    (InGameType.STAGE, (IGameStateUI)new InGameMainStateUIStageUI(), (int)currentStageData.stage_id),
                    transition).Forget();


                // [TODO] 방어 덱 설정 진입 테스트 코드 (나중에 삭제)
                // SceneTransition_Animator transition = SceneTransition_Animator.Create();
                // UserPVPBattleDetailData data = null;
                // SceneLoading.GoToNextScene("InGame",
                //     (InGameType.PVP_DEFENSE, (IGameStateUI) new InGameMainStatePvpDefenseUI(), 0),
                //     transition).Forget();

                // [TODO] PVP 진입 테스트 코드 (나중에 삭제)
                // InGameManager.Instance.EndInGame();
                // SceneTransition_Animator transition = SceneTransition_Animator.Create();
                // UserPVPBattleDetailData data = new();
                // SceneLoading.GoToNextScene("InGame",
                //     (InGameType.PVP, (IGameStateUI) new InGameMainStatePvpUI(), data),
                //     transition).Forget();

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

        private void OnClickSessionEventButton()
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

        private void OnClickConsumeAPEventButton()
        {
            // 이벤트 기간 유효성 검증
            var currentSpecEventData = SpecDataManager.Instance.GetCurrentSpecEvent(EventType.USE_AP);
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

            SceneUILayerManager.Instance.PushUILayerAsync<ItemConsumeEventPopup>(currentUserEventData).Forget();
        }

        private void OnClickEnterArenaButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<ArenaMainPopup>().Forget();
        }

        private void OnClickUserAccountLayerButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<NicknamePopup>(false).Forget();
        }

        private void OnClickSettingButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PushUILayerAsync<SettingPopup>().Forget();
        }

        private void ClearBottomSlotLayer()
        {
            _stageSlotList.Clear();

            BMUtil.RemoveChildObjects(_stageSelectScrollRect.content);
        }

        private void UpdateOpenCondition()
        {
            _attendanceButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.ATTENDANCE));
            _gachaButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.SUMMON));
            _questButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.QUEST));
            _trialDungeonButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.TRIAL_DUNGEON));
            _EnterArenaButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.PVP));
            _sessionEventButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.SESSION_TIME));
            _consumeAPEventButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.AP_USE));
        }

        public void PlayDropFx()
        {
            _dropFx.gameObject.SetActive(true);
            _dropFx.Stop();
            _dropFx.Play();
        }
    }
}
