using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Gold : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Gold;

        private void OnEnable()
        {
            UserDataManager.OnGoldChanged += GoldChanged;

            GoldChanged(UserDataManager.Instance.UserWallet.Gold);
        }

        private void OnDisable()
        {
            UserDataManager.OnGoldChanged -= GoldChanged;
        }

        private void GoldChanged(int gold)
        {
            currencyText.SetText(gold.ToString("N0"));
        }
    }
}
