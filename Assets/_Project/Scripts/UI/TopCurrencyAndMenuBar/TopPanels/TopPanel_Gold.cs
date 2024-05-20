using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Gold : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Gold;

        private void OnEnable()
        {
            UserDataManager.OnGoldChanged += GoldChanged;
        }

        private void OnDisable()
        {
            UserDataManager.OnGoldChanged -= GoldChanged;
        }

        private void GoldChanged(int gold)
        {
            currencyText.SetText(gold);
        }
    }
}
