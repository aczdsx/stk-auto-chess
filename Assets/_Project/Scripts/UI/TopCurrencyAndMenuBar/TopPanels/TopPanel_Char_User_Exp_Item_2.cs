using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_Char_User_Exp_Item_2 : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Char_User_Exp_Item_2;

        private void OnEnable()
        {
            UserDataManager.OnCharUserExpItem2Changed += CharUserExpItem2Changed;

            CharUserExpItem2Changed(UserDataManager.Instance.UserWallet.CharUserExpItem2);
        }

        private void OnDisable()
        {
            UserDataManager.OnCharUserExpItem2Changed -= CharUserExpItem2Changed;
        }

        private void CharUserExpItem2Changed(int expItem2)
        {
            currencyText.SetText(expItem2.ToString("N0"));
        }
    }
}
