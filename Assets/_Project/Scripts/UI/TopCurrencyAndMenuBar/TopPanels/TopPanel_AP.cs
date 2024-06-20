using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_AP : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.AP;

        private void OnEnable()
        {
            UserDataManager.OnAPChanged += APChanged;

            APChanged(UserDataManager.Instance.UserWallet.Ap);
        }

        private void OnDisable()
        {
            UserDataManager.OnAPChanged -= APChanged;
        }

        private void APChanged(int AP)
        {
            currencyText.SetText(AP.ToString("N0"));
        }
    }
}
