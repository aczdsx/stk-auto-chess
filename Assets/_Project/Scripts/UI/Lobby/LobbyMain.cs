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
        [SerializeField] private CAButton _playButton;
        
        private List<LobbyBottomStageSlot> _stageSlotList = new();

        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();

        private bool _isIdleRewardFullState = false;
        private ElpisDataBridge elpisDataBridge;

        public static LobbyMain GetLobbyMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<LobbyMain>();
        }

        protected override void Awake()
        {
            base.Awake();

            _playButton
                .OnClickAsObservable()
                .SubscribeAwait(this,
                    (_, self, _) =>
                        self.OnClickStartButtonAsync(),
                    AwaitOperation.Drop)
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

        private async UniTask OnClickStartButtonAsync()
        {
            SceneTransition.Create<SceneTransition_Animator>();
            await SceneTransition.FadeInAsync();
            var currentStageData = SpecDataManager.Instance.GetStageData(UserDataManager.Instance.GetLastPlayStageID());
            SceneLoading.GoToNextScene("BattleReady", currentStageData.chapter_id);
        }
    }
}
