using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using Tech.Hive.V1;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{

    /// <summary>
    /// 전투 준비 화면(로비)의 메인 UI 레이어를 관리하는 클래스
    ///
    /// - 스테이지 선택 UI 표시 및 관리 (하단 스크롤 영역)
    /// - 방치 보상 상태 표시 및 갱신
    /// - 전투 시작 버튼 클릭 처리
    /// - 스테이지 선택 버튼 클릭 처리
    /// - 방치 보상 버튼 클릭 처리
    /// - 로비로 돌아가기 버튼 클릭 처리
    /// - 가이드 미션 갱신
    /// - 챕터 클리어, 계정 레벨업 등 팝업 표시 조건 체크
    /// - Vignette 효과 적용
    /// </summary>
    public class BattleReadyMain : UILayer
    {
        public Transform GetIdleRewardTransform => _idleRewardButton.transform;

        [SerializeField] private CAButton _backToLobby;
        [SerializeField] private CAButton _playButton;
        [SerializeField] private CAButton _stageSelectButton;

        [SerializeField] private CAButton _idleRewardButton;

        [Header("Stage Milestone")]
        [SerializeField] private StageMilestonePanel stageMilestonePanel;

        [Header("Vignette Layer")]
        [SerializeField] private RawImage _vignetteImage;

        [Header("Bottom Stage Select Layer")]
        [SerializeField] private ScrollRect _stageSelectScrollRect;
        [SerializeField] private GameObject _stageSelectSlotObject;
        [SerializeField] private Image _bossStageImage;
        [SerializeField] private SpriteLoader _bossStageImageLoader;
        [SerializeField] private TextMeshProUGUI _bossStageText;
        [SerializeField] private TextMeshProUGUI _chapterNameText;
        [SerializeField] private TextMeshProUGUI _stageProgressText;
        [SerializeField] private TextMeshProUGUI _backToLobbyText;

        [Header("Guide Mission")]
        [SerializeField] private GuideMissionSlot _guideMissionSlot;

        [Header("Idle Reward Layer")]
        [SerializeField] private GameObject _normalRewardStateObject;
        [SerializeField] private Image _normalRewardFillImage;
        [SerializeField] private GameObject _fullRewardStateObject;
        [SerializeField] private TextMeshProUGUI _idleRewardStateText;
        [SerializeField] private ParticleSystem _dropFx;
        

        private List<LobbyBottomStageSlot> _stageSlotList = new();

        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private bool _isIdleRewardFullState = false;
        private ElpisModel elpisDataBridge;

        public StageMilestonePanel StageMilestonePanel => stageMilestonePanel;

        public static BattleReadyMain GetBattleReadyMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<BattleReadyMain>();
        }

        protected override void Awake()
        {
            base.Awake();

            _backToLobby.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickGoToLobby()).AddTo(this);
            // _backToLobby.
            _playButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickStartButtonAsync(), AwaitOperation.Drop).AddTo(this);
            _stageSelectButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickChapterStageButton()).AddTo(this);
            _idleRewardButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickIdleRewardButton()).AddTo(this);
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            PreEnterAsync().Forget();
            SceneUILayerManager.OnUITransitionEvent += OnUITransition;
        }

        private async UniTask PreEnterAsync()
        {
            elpisDataBridge = ServerDataManager.Instance.Elpis;
            var simulationCenter = elpisDataBridge.GetFacilityByType(ElpisFacilityType.FacilityTypeSimulationCenter);
            _idleRewardButton.gameObject.SetActive(simulationCenter != null && simulationCenter.Level > 0);

            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.AP);

            // 챕터 데이터 갱신
            await NetManager.Instance.Battle.GetCurrentChapterAsync();

            // 목표 스테이지로 설정 (로비에서 진입 시 다음 목표 스테이지 반영)
            int currentStageId = BattleModel.GetTargetStageId();
            LocalDataManager.Instance.SetLastPlayStageId((uint)currentStageId);

            var stageSpecData = SpecDataManager.Instance.GetStageData(currentStageId);
            InGameManager.Instance.StartInGame<FlowStateLobbyCombat>(stageSpecData);

            // 방치 보상 갱신
            SetIdleRewardLayer();

            // 레이어 갱신
            RefreshUI(LobbyMainRefreshType.ALL);

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_command01);

            var guideMissionModel = ServerDataManager.Instance.GuideMission;

            if (guideMissionModel.GuideMissionId == 501 && currentStageId >= GuideMissionConstants.챕터2기준ID)
            {
                await guideMissionModel.AddActionValueAsync(GuideMissionType.ENTER_CHAPTER);
            }

            // UI 세팅 완료 후 페이드 아웃
            await SceneTransition.FadeOutAsync();

            TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.BATTLE_READY, "0");
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            SceneUILayerManager.OnUITransitionEvent -= OnUITransition;
        }

        private void OnUITransition(UILayerTransition transition, string key, UILayer layer, object data)
        {
            // 다른 UI가 닫힐 때 현재 자신이 최상단인지 확인
            if (transition == UILayerTransition.ExitFinished && layer != this)
            {
                var routes = SceneUILayerManager.Instance.GetUIRoutes();
                if (routes.Length > 0 && routes[^1] == this)
                {
                    OnBecameTop();
                }
            }
        }

        private void OnBecameTop()
        {
            NetManager.Instance.GuideMission.GetAsync().Forget();
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
            }
        }

        public void RefreshBottomStageUI()
        {
            if (_stageSlotList == null || _stageSlotList.Count <= 0) return;

            // 기본 데이터 갱신
            int currentStageId = (int)LocalDataManager.Instance.GetLastPlayStageId();

            var stageSpecData = SpecDataManager.Instance.GetStageData(currentStageId);
            var chapterSpecData = SpecDataManager.Instance.GetChapterData(stageSpecData.chapter_id, stageSpecData.difficulty_type);

            //_chapterImage.sprite = specStage.chapter_image;
            _backToLobbyText.text = _chapterNameText.text = LanguageManager.Instance.GetDefaultText(chapterSpecData.name_token);
            

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
        }

        private void SetBottomStageUI()
        {
            ClearBottomSlotLayer();

            int currentStagdId = (int)LocalDataManager.Instance.GetLastPlayStageId();

            StageInfo specStageData = SpecDataManager.Instance.GetStageData(currentStagdId);
            var specChapterData = SpecDataManager.Instance.GetChapterDataByStageID(currentStagdId);

            var stageList = SpecDataManager.Instance.GetStageList(specChapterData.chapter_id, specChapterData.difficulty_type);

            //_chapterImage.sprite = specStage.chapter_image;
            _backToLobbyText.text =  _chapterNameText.text = LanguageManager.Instance.GetDefaultText(specChapterData.name_token);

            int totalStageCount = stageList.Count;
            _stageProgressText.SetText("{0}/{1}", specStageData.stage_number, totalStageCount);

            // _apCostText.text = $"x{specStageData.need_ap}";

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
            StageInfo bossStageData = SpecDataManager.Instance.GetStageData(specStageData.chapter_id, specStageData.difficulty_type, StageType.BATTLE_BOSS);
            if (bossStageData != null)
            {
                // var stageMonsterData = SpecDataManager.Instance.GetStageMonsterData(bossStageData.chapter_id, bossStageData.stage_number, bossStageData.difficulty_type);
                // var specMonsterData = SpecDataManager.Instance.GetCharacterData(stageMonsterData.monster_id);

                // _bossStageImage.sprite = ImageManager.Instance.GetBossBannerSprite(specMonsterData.prefab_id);
                // _bossStageText.text = $"{bossStageData.chapter_id}-{bossStageData.stage_number}";
                //Assets/_Project/Addressables/Remote/Texture_StandAlone/Dynamic/BossBanner/BossBanner_1.png
                // 보스 이미지 처리
                _bossStageImageLoader.SetSprite(SpriteNameParser.GetBossBannerSprite(specStageData.chapter_id)).Forget();

                _bossStageText.text = $"{bossStageData.chapter_id}-{bossStageData.stage_number}";
            }

            // Vignette 세팅
            SetVignetteColor(specChapterData.chapter_id);

            // 마일스톤 패널 갱신
            stageMilestonePanel.SetChapterData(specChapterData);

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
            var data = SoDataProvider.Instance.Get<VignetteSO>();
            var vignette = data.stageColors.FirstOrDefault(x => x.InGameType == InGameType.STAGE && x.ID == targetChapter);
            _vignetteImage.material = vignette.Material.Asset as Material;
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

            try
            {
                while (!IdleRewardHelper.IsFull())
                {
                    _normalRewardStateObject.gameObject.SetActive(true);
                    _fullRewardStateObject.gameObject.SetActive(false);

                    float progressRatio = IdleRewardHelper.GetProgressRatio();
                    _idleRewardStateText.text = $"{IdleRewardHelper.GetProgressPercent()}%";
                    _normalRewardFillImage.fillAmount = progressRatio;

                    await UniTask.Delay(1000, cancellationToken: cancelToken);
                }

                // 꽉 찼을경우 처리
                _normalRewardStateObject.gameObject.SetActive(false);
                _fullRewardStateObject.gameObject.SetActive(true);
                _isIdleRewardFullState = true;
                _idleRewardStateText.text = "100%";
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
            if (ServerDataManager.Instance.GuideMission.GuideMissionId >= 600)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<EndTestgamePopup>().Forget();
                return;
            }
        }


        private void CheckNewChapterClear()
        {
            return;
            int lastClearStageID = (int)ServerDataManager.Instance.Battle.GetLatestClearedStageId();
            var lastClearStageData = SpecDataManager.Instance.GetStageData(lastClearStageID);
            var rewardInfo = SpecDataManager.Instance.GetSpecRewardInfo(ContentType.CHAPTER, lastClearStageData.chapter_id, lastClearStageData.difficulty_type);
            var isRewarded = ClientProgressData.Get().IsRewardReceived(rewardInfo.reward_id);
            if (isRewarded)
                return;

            SceneUILayerManager.Instance.PushUILayerAsync<ChapterClearWindowPopup>().Forget();
        }

        private void CheckUserAccountLevelUp()
        {
            var playerData = ServerDataManager.Instance.PlayerData;
            Debug.Log($"[CheckUserAccountLevelUp] Level: {playerData.Level}, PrevAccountLevel: {playerData.PrevAccountLevel}");
            if (playerData.Level <= playerData.PrevAccountLevel) return;

            var specAccountLevelData = SpecDataManager.Instance.GetAccountLevelExpDataByLevel((int)playerData.Level);
            if (specAccountLevelData != null)
            {
                SceneUILayerManager.Instance.PushUILayerAsync<AccountLevelUpWindowPopup>(specAccountLevelData).Forget();
                playerData.PrevAccountLevel = playerData.Level;
            }
        }

        private void OnClickCommanderSkillButton()
        {
            SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(false);
            SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>(null, callbackObject =>
            {
                SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(true);
            }).Forget();
        }

        public async UniTask OnClickGoToLobby()
        {
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            InGameManager.Instance.EndInGame();

            // 현재 진행중인 가이드 미션으로 ENTER_ELPIS_NANI 트리거 확인
            var guideMissionId = (int)ServerDataManager.Instance.GuideMission.GuideMissionId;
            SceneLoading.GoToNextSceneWithElpisEnterTrigger("Lobby", guideMissionId);
        }

        private async UniTask OnClickStartButtonAsync()
        {
            if (SceneTransition.IsFadeProcessing)
                return;
            var currentStageData = SpecDataManager.Instance.GetStageData((int)LocalDataManager.Instance.GetLastPlayStageId());
            if (currentStageData != null)
            {
                // TODO: 행동력 검사
                // if (!UserDataManager.Instance.CheckEnoughItem(IdMap.Item.ActionPoint, currentStageData.need_ap, false))
                // {
                //     SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
                //     ToastManager.Instance.ShowToastByTokenKey("MSG_GUIDE_IDLE_REWARD_AP");
                //     return;
                // }

                // 게임 플로우 강제성을 위한 예외처리 적용
                var guideMission = ServerDataManager.Instance.GuideMission;
                if (guideMission.Data != null)
                {
                    if (!TutorialManager.IsSkipTutorial)
                    {
                        var specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get((int)guideMission.GuideMissionId);
                        if (specGuideMissionData != null)
                        {
                            // 보상 수령 가능 상태면 보상 받으라고 안내
                            if (guideMission.IsGoalReached)
                            {
                                ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_1");
                                return;
                            }

                            // 진행 중인데 CLEAR_STAGE가 아니면 진입 차단
                            if (specGuideMissionData.guide_mission_type != GuideMissionType.CLEAR_STAGE)
                            {
                                ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_2");
                                return;
                            }
                        }
                    }
                }

                // 스테이지 진입
                var inGameParams = await NetManager.Instance.Battle.StartAsync(currentStageData.chapter_id, currentStageData.stage_id, 0, Array.Empty<string>());
                if (inGameParams == null)
                    return;

                SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
                await SceneTransition.FadeInAsync();

                InGameManager.Instance.EndInGame();

                SceneLoading.GoToNextSceneWithStageEnterTrigger("InGame", currentStageData.stage_id, inGameParams);
            }
        }

        private void OnClickChapterStageButton()
        {
            int currentStageId = (int)LocalDataManager.Instance.GetLastPlayStageId();
            SceneUILayerManager.Instance.PushUILayerAsync<ChapterListPopup>(currentStageId).Forget();
        }

        private void OnClickGachaButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget();
        }

        private void OnClickCharacterCollectionButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();
        }

        private void OnClickIdleRewardButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
        }

        private void OnClickQuestButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<QuestPopup>().Forget();
        }

        private void OnClickTrialDungeonButton()
        {
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

            // 이벤트 데이터 조회
            var currentEventData = ServerDataManager.Instance.Event.GetEvent(currentSpecEventData.event_id);
            if (currentEventData == null)
            {
                return;
            }

            SceneUILayerManager.Instance.PushUILayerAsync<SessionTimeEventPopup>(currentEventData.EventId, null).Forget();
        }

        private void OnClickConsumeAPEventButton()
        {
            // 이벤트 기간 유효성 검증
            var currentSpecEventData = SpecDataManager.Instance.GetCurrentSpecEvent(EventType.USE_AP);
            if (currentSpecEventData == null)
            {
                return;
            }

            // 이벤트 데이터 조회
            var currentEventData = ServerDataManager.Instance.Event.GetEvent(currentSpecEventData.event_id);
            if (currentEventData == null)
            {
                return;
            }

            SceneUILayerManager.Instance.PushUILayerAsync<ItemConsumeEventPopup>(currentEventData.EventId, null).Forget();
        }

        private void OnClickUserAccountLayerButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<NicknamePopup>(false).Forget();
        }

        private void OnClickSettingButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<SettingPopup>().Forget();
        }

        private void ClearBottomSlotLayer()
        {
            _stageSlotList.Clear();

            BMUtil.RemoveChildObjects(_stageSelectScrollRect.content);
        }

        private void UpdateOpenCondition()
        {
            // _attendanceButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.ATTENDANCE));
            // _gachaButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.SUMMON));
            // _questButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.QUEST));
            // _trialDungeonButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.TRIAL_DUNGEON));
            // _sessionEventButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.SESSION_TIME));
            // _consumeAPEventButton.gameObject.SetActive(SpecDataManager.Instance.GetIsOpenCondition(OpenConditionType.AP_USE));
        }

        public void PlayDropFx()
        {
            _dropFx.gameObject.SetActive(true);
            _dropFx.Stop();
            _dropFx.Play();
        }
    }
}
