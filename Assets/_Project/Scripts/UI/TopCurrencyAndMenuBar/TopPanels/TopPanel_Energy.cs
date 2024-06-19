using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Energy : TopPanelBase
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

        private void ApChanged(int energy)
        {
            currencyText.SetText(energy.ToString("N0"));
        }
    }
}
