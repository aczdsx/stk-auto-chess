using Cysharp.Text;

namespace CookApps.AutoBattler
{
    public class TopPanel_PVP_Ticket : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.PVP_Ticket;

        private void OnEnable()
        {
            UserDataManager.OnCTicketChanged += PVPTicketChanged;

            PVPTicketChanged(UserDataManager.Instance.UserWallet.PvpTicket);
        }

        private void OnDisable()
        {
            UserDataManager.OnCTicketChanged -= PVPTicketChanged;
        }

        private void PVPTicketChanged(int PVPTicket)
        {
            currencyText.SetText(PVPTicket.ToString("N0"));
        }
    }
}