using System;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
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
        [Header("Buttons")]
        [SerializeField] private CAButton battleButton;

        private UniTaskCompletionSource preEnterTaskSource = new();

        public Transform GetTopCurrencyAndMenuBarParent()
        {
            return CachedTr;
        }

        private List<LobbyBottomStageSlot> stageSlotList = new();

        private bool _isIdleRewardFullState = false;
        private ElpisModel elpisDataModel;

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
        }

        protected override void OnBackButton(ref bool offPrevUI) { }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            preEnterTaskSource = new UniTaskCompletionSource();
            PreEnterAsync().ContinueWith(() => preEnterTaskSource.TrySetResult()).Forget();
            SceneUILayerManager.OnUITransitionEvent += OnUITransition;
        }

        private async UniTask PreEnterAsync()
        {
            var animateCamera = MainCameraHolder.CameraGestureController;

            baseAnimator.Play("IdleExit");

            animateCamera.SetCameraZoomForce(30.0f);

            await LoadElpis();

            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_command01);
            SceneTransition.FadeOutAsync().Forget();
            animateCamera.ZoomAsync(16.0f, 1.0f).Forget();

            await PlayEnterAnimationAsync();

            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.AP, TopPanelType.Elpis_BuildItem);
            if (!TutorialManager.IsSkipTutorial)
            {
                await TutorialManager.Instance.TryStartOutgameTutorial();
                TutorialManager.Instance.SubscribeGuideMissionChanged();
                TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.ENTER_ELPIS, "0");
            }

            if (ServerDataManager.Instance.GuideMission.GuideMissionId == GuideMissionConstants.커맨드센터들어간가이드미션ID)
            {
                ServerDataManager.Instance.GuideMission.AddActionValue(GuideMissionType.USE_BUILDING);
            }

            CheckShowSurveyPopup();

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

        private async UniTask HubbleLobbyScequence()
        {
            SceneUILayerManager.Instance.SetEnableMainNodeCanvas(false);
            MainCameraHolder.CameraGestureController.SetCanInteractCamera(false);
            await UniTask.Delay(500);
            SceneUILayerManager.Instance.SetEnableMainNodeCanvas(true);
            MainCameraHolder.CameraGestureController.SetCanInteractCamera(true);
        }

        public async UniTask OnClickStartButton()
        {
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            var currentStageData = SpecDataManager.Instance.GetStageData(BattleModel.GetTargetStageId());
            SceneLoading.GoToNextScene("BattleReady", currentStageData.chapter_id);
        }

        public void PlayExitAnimation()
        {
            StartExitAnimation(null);
        }

        public void PlayEnterAnimation()
        {
            PlayEnterAnimationAsync().Forget();
        }

        public async UniTask PlayEnterAnimationAsync()
        {
            var tcs = new UniTaskCompletionSource();
            base.StartEnterAnimation(_ => tcs.TrySetResult());
            await tcs.Task;
            CheckRedDots().Forget();
        }

        protected override void StartEnterAnimation(Action<UILayer> endCallback)
        {
            // PreEnterAsync에서 애니메이션 처리, 여기서는 완료 대기 후 callback만 호출
            WaitAndInvokeCallback(endCallback).Forget();
        }

        private async UniTask WaitAndInvokeCallback(Action<UILayer> endCallback)
        {
            await preEnterTaskSource.Task;
            endCallback?.Invoke(this);
        }

        #region RedDot

        /// <summary>
        /// 서버에서 바로 못받는 애들 갱신해주기
        /// ex. 퀘스트
        /// </summary>
        private async UniTask CheckRedDots()
        {
            await NetManager.Instance.Quest.ListDailyQuestAsync();
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

        #endregion
    }
}
