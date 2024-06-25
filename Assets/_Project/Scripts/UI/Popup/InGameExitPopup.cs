using System.Collections;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/WindowPopup/InGameExitPopup.prefab")]
    public class InGameExitPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _exitButton;
        [SerializeField] private CAButton _continueButton;

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
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            InGameMainFlowManager.Instance.SetPlaySpeed(0);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();

            InGameMainFlowManager.Instance.SetPlaySpeed(1.0f);
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void OnClickExitButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);

            InGameMainFlowManager.Instance.AddNextState<FlowStateStageFail>();
        }
    }
}
