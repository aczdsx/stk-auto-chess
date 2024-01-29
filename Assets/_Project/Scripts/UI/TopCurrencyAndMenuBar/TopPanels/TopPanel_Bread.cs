using System;
using Cysharp.Text;

namespace CookApps.SampleTeamBattle
{
    public class TopPanel_Bread : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Bread;

        private void OnEnable()
        {
            UserDataWallet.OnBreadChanged += OnBreadChanged;
        }

        private void OnDisable()
        {
            UserDataWallet.OnBreadChanged -= OnBreadChanged;
        }

        private void OnBreadChanged(int bread)
        {
            currencyText.SetText(bread);
        }
    }
}
