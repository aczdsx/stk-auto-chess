using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class TopPanel_PVP_Ticket : TopPanelBase
    {
        [SerializeField] private CAButton _topPanelButton;
        
        public override TopPanelType PanelType => TopPanelType.PVP_Ticket;

        private void OnEnable()
        {
            UserDataManager.OnPVPTicketChanged += PVPTicketChanged;

            PVPTicketChanged(UserDataManager.Instance.UserWallet.PvpTicket);
            
            _topPanelButton.onClick.AddListener(OnClickTopPanelButton);
        }

        private void OnDisable()
        {
            UserDataManager.OnPVPTicketChanged -= PVPTicketChanged;
            
            _topPanelButton.onClick.RemoveListener(OnClickTopPanelButton);
        }

        private void PVPTicketChanged(int PVPTicket)
        {
            currencyText.SetText(PVPTicket.ToString("N0"));
        }
        
        private void OnClickTopPanelButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<BuyArenaPopup>().Forget();
        }
    }
}