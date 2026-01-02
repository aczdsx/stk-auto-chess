using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CookApps.BattleSystem;
using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
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

    public partial class LobbyMain : UILayer
    {
        [SerializeField] private CAButton battleButton;
        [SerializeField] private CAButton dungeonButton;
        [SerializeField] private CAButton characterButton;
        [SerializeField] private CAButton hubbleButton;
        [SerializeField] private CAButton shopButton;
        [SerializeField] private CAButton summonButton;
        [SerializeField] private CAButton eventButton;
        [SerializeField] private CAButton inventoryButton;
        
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
                .Subscribe(this, (_, self) => self.OnClickStartButton().Forget())
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

            eventButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickEventButton())
                .AddTo(this);

            inventoryButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClickInventoryButton())
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
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Gold, TopPanelType.AP);

            elpisDataBridge = new ElpisDataBridge();
            await LoadElpis();
            
            SceneTransition.FadeOutAsync().Forget();

            // bgm on
            SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_lobby);
        }

        private async UniTask OnClickStartButton()
        {
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();
            var currentStageData = SpecDataManager.Instance.GetStageData(UserDataManager.Instance.GetLastPlayStageID());
            SceneLoading.GoToNextScene("BattleReady", currentStageData.chapter_id);
        }

        private void OnClickDungeonButton()
        {
            // TODO: 던전 버튼 클릭 처리
        }

        private void OnClickCharacterButton()
        {
            // TODO: 캐릭터 버튼 클릭 처리
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
            // TODO: 소환 버튼 클릭 처리
        }

        private void OnClickEventButton()
        {
            // TODO: 이벤트 버튼 클릭 처리
        }

        private void OnClickInventoryButton()
        {
            // TODO: 인벤토리 버튼 클릭 처리
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
