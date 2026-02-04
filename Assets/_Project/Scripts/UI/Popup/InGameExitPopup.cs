using System;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class InGameExitPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _continueButton;
        
        private Type _failType;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _exitButton.OnClickAsObservable().SubscribeAwait(this, (_, self, _) => self.OnClickExitButtonAsync(), AwaitOperation.Drop).AddTo(this);
            _continueButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            _failType = (Type) param;
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            InGameMainFlowManager.Instance.SetPlaySpeed(0);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();

            bool isSpeedUp = Preference.LoadPreference(Pref.IS_SPEED_UP, false);
            InGameMainFlowManager.Instance.SetInGameSpeed(isSpeedUp);
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private async UniTask OnClickExitButtonAsync()
        {
            SceneUILayerManager.Instance.PopUILayer(this);

            // 전투 준비 중일 경우 분기 처리
            if (InGameMainFlowManager.Instance.CurrentFlowState is StateReadyBase)
            {
                InGameManager.Instance.EndInGame();

                int lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
                var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);

                SceneTransition.Create<SceneTransition_FadeInOut>();
                await SceneTransition.FadeInAsync();
                SceneLoading.GoToNextScene("BattleReady", specLastStageData.chapter_id);
            }
            else
            {
                if (InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase && InGameManager.Instance.IsInGameCombat)
                {
                    InGameManager.Instance.AppEventResult = "fail";
                    InGameManager.Instance.AppEventReason = "exit";

                    InGameMainFlowManager.Instance.AddNextState(_failType);
                }
            }
        }
    }
}
