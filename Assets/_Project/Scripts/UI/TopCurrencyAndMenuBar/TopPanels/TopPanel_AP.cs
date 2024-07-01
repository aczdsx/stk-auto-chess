using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using Cysharp.Text;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public class TopPanel_AP : TopPanelBase
    {
        [SerializeField] private CAButton _topPanelButton;

        public override TopPanelType PanelType => TopPanelType.AP;

        private void OnEnable()
        {
            UserDataManager.OnAPChanged += APChanged;

            APChanged(UserDataManager.Instance.UserWallet.Ap);

            _topPanelButton.onClick.AddListener(OnClickTopPanelButton);
        }

        private void OnDisable()
        {
            UserDataManager.OnAPChanged -= APChanged;

            _topPanelButton.onClick.RemoveListener(OnClickTopPanelButton);
        }

        private void APChanged(int AP)
        {
            currencyText.SetText(AP.ToString("N0"));
        }

        private void OnClickTopPanelButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
            //ToastManager.Instance.ShowToastByTokenKey("MSG_GUIDE_IDLE_REWARD_AP");
        }
    }
}
