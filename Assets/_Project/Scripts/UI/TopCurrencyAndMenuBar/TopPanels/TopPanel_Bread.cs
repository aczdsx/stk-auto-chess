using System;
using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Bread : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Energy;

        private void OnEnable()
        {
            UserDataManager.OnEnergyChanged += EnergyChanged;
        }

        private void OnDisable()
        {
            UserDataManager.OnEnergyChanged -= EnergyChanged;
        }

        private void EnergyChanged(int bread)
        {
            currencyText.SetText(bread);
        }
    }
}
