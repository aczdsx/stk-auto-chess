using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Char_User_Exp_Item : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Char_User_Exp_Item;

        private void OnEnable()
        {
            UserDataManager.OnCharUserExpItemChanged += CharUserExpItemChanged;

            CharUserExpItemChanged(UserDataManager.Instance.UserWallet.CharUserExpItem);
        }

        private void OnDisable()
        {
            UserDataManager.OnCharUserExpItemChanged -= CharUserExpItemChanged;
        }

        private void CharUserExpItemChanged(int expItem)
        {
            currencyText.SetText(expItem.ToString("N0"));
        }
    }
}
