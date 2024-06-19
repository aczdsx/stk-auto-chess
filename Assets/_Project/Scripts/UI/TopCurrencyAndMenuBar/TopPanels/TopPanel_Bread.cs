using System;
using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Bread : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Energy;

        private void OnEnable()
        {
            UserDataManager.OnApChanged += ApChanged;
        }

        private void OnDisable()
        {
            UserDataManager.OnApChanged -= ApChanged;
        }

        private void ApChanged(int bread)
        {
            currencyText.SetText(bread);
        }
    }
}
