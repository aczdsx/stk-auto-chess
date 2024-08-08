using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using System;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/InGameExitPopup.prefab")]
    public class InGameExitPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _continueButton;
        
        private Type _failType;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _exitButton.onClick.AddListener(OnClickExitButton);
            _continueButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _exitButton.onClick.RemoveListener(OnClickExitButton);
            _continueButton.onClick.RemoveListener(OnClickCloseButton);
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
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickExitButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);
            SceneUILayerManager.Instance.PopUILayer(this);
            InGameMainFlowManager.Instance.AddNextState(_failType);
        }
    }
}
