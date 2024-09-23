using System;
using System.Threading;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class TopPanel_PVP_Ticket : TopPanelBase
    {
        [SerializeField] private CAButton _topPanelButton;

        [SerializeField] private TextMeshProUGUI _remainTimeText;
        
        private CancellationTokenSource _unitaskCancelToken = new CancellationTokenSource();
        
        public override TopPanelType PanelType => TopPanelType.PVP_Ticket;

        private void OnEnable()
        {
            UserDataManager.OnPVPTicketChanged += PVPTicketChanged;

            PVPTicketChanged(UserDataManager.Instance.UserWallet.PvpTicket);
            
            _topPanelButton.onClick.AddListener(OnClickTopPanelButton);

            SetRemainTime();
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
        
        private async void SetRemainTime()
        {
            try
            {
                _unitaskCancelToken.Cancel();
                _unitaskCancelToken = new CancellationTokenSource();
                
                await UpdateRemainTime(_unitaskCancelToken.Token).AttachExternalCancellation(this.GetCancellationTokenOnDestroy());
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }
        
        private async UniTask UpdateRemainTime(CancellationToken cancelToken)
        {
            UserDataManager.Instance.UpdatePVPTicketData(true);
            
            var targetTimeStamp = UserDataManager.Instance.UserPVP.PvpTicketNextTimestamp;
            
            TimeSpan remainTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(targetTimeStamp);
            
            _remainTimeText.gameObject.SetActive(true);
            
            try
            {
                while (UserDataManager.Instance.IsItemMaxCount(ItemType.PVP_TICKET) == false)
                {
                    _remainTimeText.text = LanguageManager.Instance.GetRemainTimeText(remainTimeSpan);

                    await UniTask.Delay(1000, cancellationToken: cancelToken);

                    remainTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(targetTimeStamp);
                    
                    // 시간이 경과하였을 경우 처리
                    if (remainTimeSpan.TotalSeconds <= 0)
                    {
                        UserDataManager.Instance.UpdatePVPTicketData(true);
                    }
                }
                
                // 아이템이 가득 찼을 경우 처리
                if (UserDataManager.Instance.IsItemMaxCount(ItemType.PVP_TICKET))
                {
                    _remainTimeText.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
            }
        }
    }
}