using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

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

    public partial class LobbyMain : UILayer, TopCurrencyAndMenuBarContainer
    {
        [Header("Guide")]
        [SerializeField] private GuideMissionSlot guideMissionSlot;

        [Header("Buttons")]
        [SerializeField] private CAButton battleButton;
        [SerializeField] private CAButton dungeonButton;
        [SerializeField] private CAButton characterButton;
        [SerializeField] private CAButton hubbleButton;
        [SerializeField] private CAButton shopButton;
        [SerializeField] private CAButton summonButton;
        [SerializeField] private CAButton consumeApEventButton;
        [SerializeField] private CAButton sessionTimeEventButton;
        [SerializeField] private CAButton inventoryButton;
        [SerializeField] private CAButton questButton;
        [SerializeField] private TMPro.TextMeshProUGUI _stageNameText;

        public Transform GetTopCurrencyAndMenuBarParent()
        {
            return CachedTr;
        }

        private List<LobbyBottomStageSlot> stageSlotList = new();

        private bool _isIdleRewardFullState = false;
        private ElpisDataBridge elpisDataBridge;

        public static LobbyMain GetLobbyMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        }

        protected override void Awake()
        {
            base.Awake();

            battleButton
                .OnClickAsObservable()
                .SubscribeAwait(this, (_, self, _) => self.OnClickStartButton(), AwaitOperation.Drop)
                .AddTo(this);

            dungeonButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickDungeonButton())
                .AddTo(this);

            characterButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickCharacterButton())
                .AddTo(this);

            hubbleButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickHubbleButton())
                .AddTo(this);

            shopButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickShopButton())
                .AddTo(this);

            summonButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickSummonButton())
                .AddTo(this);

            consumeApEventButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickConsumeApEventButton())
                .AddTo(this);

            sessionTimeEventButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickSessionTimeEventButton())
                .AddTo(this);

            inventoryButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickInventoryButton())
                .AddTo(this);

            questButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickQuestButton())
                .AddTo(this);
        }

        protected override void OnBackButton(ref bool offPrevUI) { }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);


            PreEnterAsync().Forget();
        }

        private async UniTask PreEnterAsync()
        {
            // await GuideMissionTestUtility.HandleIteratively();
            guideMissionSlot.InitGuideMissionSlot();

            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.AP);

            await LoadElpis();

            SceneTransition.FadeOutAsync().Forget();

            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_lobby);
            // TODO Model 대신 Bridge로 가져오기
            var model = ServerDataManager.Instance.GuideMission;
            var specGuideMissionData = SpecDataManager.Instance.GuideMissionInfo.Get((int)model.GuideMissionId);

#if _SJHONG_TEST_

            if(specGuideMissionData.id <= 101) {
                await TutorialManager.Instance.TryStartOutgameTutorial();

                TutorialManager.Instance.SubscribeGuideMissionChanged();
            }
#else
            // 아웃게임 튜토리얼 시작 (가이드 미션 기반)
            await TutorialManager.Instance.TryStartOutgameTutorial();

            TutorialManager.Instance.SubscribeGuideMissionChanged();

#endif
            // 이거는 엘피스 연출 후에 실행 되어야 함.
            TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.ENTER_ELPIS, "0");

            var currentStageData = SpecDataManager.Instance.GetStageData(BattleDataBridge.GetTargetStageId());
            _stageNameText.text = ZString.Format("SECTOR {0}-{1}", currentStageData.chapter_id, currentStageData.stage_number);
        }

        private async UniTask HubbleLobbyScequence()
        {
            SceneUILayerManager.Instance.SetEnableMainNodeCanvas(false);
            MainCameraHolder.CameraGestureController.SetCanInteractCamera(false);
            await UniTask.Delay(500);
            SceneUILayerManager.Instance.SetEnableMainNodeCanvas(true);
            MainCameraHolder.CameraGestureController.SetCanInteractCamera(true);
        }

        private async UniTask OnClickStartButton()
        {
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            var currentStageData = SpecDataManager.Instance.GetStageData(BattleDataBridge.GetTargetStageId());
            SceneLoading.GoToNextScene("BattleReady", currentStageData.chapter_id);
        }

        private void OnClickDungeonButton()
        {
            // CLEAR_BABEL 타입의 가이드 미션 중 가장 낮은 order를 가진 미션 찾기
            var guideMissionInfos = SpecDataManager.Instance.GuideMissionInfo.All;
            int minOrder = int.MaxValue;
            int requiredMissionId = 0;

            for (int i = 0; i < guideMissionInfos.Count; i++)
            {
                var missionInfo = guideMissionInfos[i];
                if (missionInfo.guide_mission_type == GuideMissionType.CLEAR_BABEL && missionInfo.order < minOrder)
                {
                    minOrder = missionInfo.order;
                    requiredMissionId = missionInfo.id;
                }
            }

            // 유저의 현재 가이드미션 ID와 비교
            var userGuideMissionId = ServerDataManager.Instance.GuideMission.GuideMissionId;

            if (userGuideMissionId < requiredMissionId)
            {
                // 토스트 메시지 표시
                ToastManager.Instance.ShowToastByTokenKey("GUIDE_MISSION_ALERT_MSG_2");
                return;
            }

            SceneUILayerManager.Instance.PushUILayerAsync<DungeonTrialPopup>().Forget();
        }

        private void OnClickCharacterButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();
        }

        private void OnClickHubbleButton()
        {
            // TODO: 허블 버튼 클릭 처리
        }

        private void OnClickShopButton()
        {
            // TODO: 상점 버튼 클릭 처리
        }

        private void OnClickSummonButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<GachaPopup>().Forget();
        }

        private void OnClickSessionTimeEventButton()
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

            SceneUILayerManager.Instance.PushUILayerAsync<SessionTimeEventPopup>(currentEventData.EventId).Forget();
        }

        private void OnClickConsumeApEventButton()
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

            SceneUILayerManager.Instance.PushUILayerAsync<ItemConsumeEventPopup>(currentEventData.EventId).Forget();
        }

        private void OnClickInventoryButton()
        {
            // TODO: 인벤토리 버튼 클릭 처리
        }

        private void OnClickQuestButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<QuestPopup>().Forget();
        }

        public void PlayExitAnimation()
        {
            StartExitAnimation(null);
        }

        public void PlayEnterAnimation()
        {
            StartEnterAnimation(null);
        }
    }
}
