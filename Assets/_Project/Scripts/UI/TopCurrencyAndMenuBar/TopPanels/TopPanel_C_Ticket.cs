using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_C_Ticket : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.C_Ticket;

        private void Awake()
        {
            CTicketChanged(UserDataManager.Instance.UserWallet.CTicket);
        }

        private void OnEnable()
        {
            UserDataManager.OnCTicketChanged += CTicketChanged;
        }

        private void OnDisable()
        {
            UserDataManager.OnCTicketChanged -= CTicketChanged;
        }

        private void CTicketChanged(int cTicket)
        {
            currencyText.SetText(cTicket.ToString("N0"));
        }
    }
}
