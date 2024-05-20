using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Jewel : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Jewel;

        private void Awake()
        {
            JewelChanged(UserDataManager.Instance.UserWallet.Jewel);
        }

        private void OnEnable()
        {
            UserDataManager.OnJewelChanged += JewelChanged;
        }

        private void OnDisable()
        {
            UserDataManager.OnJewelChanged -= JewelChanged;
        }

        private void JewelChanged(int jewel)
        {
            currencyText.SetText(jewel);
        }
    }
}
