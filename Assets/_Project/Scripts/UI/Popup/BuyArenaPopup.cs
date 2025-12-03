using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class BuyArenaPopup : UILayer
    {
        [Header("Common")]
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _purchaseButton;
        
        [Space(10)]
        [SerializeField] private TextMeshProUGUI _ticketPriceText;
        [SerializeField] private TextMeshProUGUI _ticketBuyCountText;
        [SerializeField] private TextMeshProUGUI _ticketPriceButtonText;
        [SerializeField] private TextMeshProUGUI _ticketDescText;
        [SerializeField] private TextMeshProUGUI _ticketCountText;

        private int _ticketPrice = 0;
        private int _ticketCount = 0;
        private int _ticketLimitCount = 0;
        
        private void Awake()
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
            _purchaseButton.onClick.AddListener(OnClickPurhcaseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _purchaseButton.onClick.RemoveListener(OnClickPurhcaseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            SetPopup();
        }

        private void SetPopup()
        {
            _ticketPrice = SpecDataManager.Instance.GetGameConfig<int>("PVP_TICKET_PURCHASE_PRICE");
            _ticketCount = SpecDataManager.Instance.GetGameConfig<int>("PVP_TICKET_PURCHASE_COUNT");
            _ticketLimitCount = SpecDataManager.Instance.GetGameConfig<int>("PVP_TICKET_PURCHASE_LIMIT_COUNT");

            string buyDescString = LanguageManager.Instance.GetLanguageText("BUY_ARENA_TICKET_DESC");
            _ticketDescText.text = string.Format(buyDescString, _ticketPrice, _ticketCount);

            string buyCountString = LanguageManager.Instance.GetLanguageText("BUY_COUNT_MSG");
            _ticketBuyCountText.text = string.Format(buyCountString, UserDataManager.Instance.UserPVP.BuyTicketCnt, _ticketLimitCount);
            
            _ticketPriceButtonText.text = $"x{_ticketPrice}";
        }

        private void OnClickPurhcaseButton()
        {
            // 구매 횟수 제한 체크
            if (UserDataManager.Instance.UserPVP.BuyTicketCnt >= _ticketLimitCount)
            {
                ToastManager.Instance.ShowToastByTokenKey("MSG_PURCHASE_COUNT_OVER");
                return;
            }
            
            // 재화 체크
            if (!UserDataManager.Instance.CheckEnoughItem(ItemType.GOLD, 0, _ticketPrice, true))
            {
                return;
            }
            
            // 재화 소모
            UserDataManager.Instance.DecreaseItem(ItemType.GOLD, 0, _ticketPrice, true, false);
            
            // 티켓 지급
            List<RewardItem> rewardItemList = new List<RewardItem>();
            RewardItem newReward = new RewardItem(ItemType.PVP_TICKET, 0, _ticketCount);
            rewardItemList.Add(newReward);
            
            UserDataManager.Instance.IncreaseRewardItemList(rewardItemList, true);

            SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList)).Forget();
            
            // 구매 횟수 증가
            UserDataManager.Instance.UserPVP.BuyTicketCnt++;
            UserDataManager.Instance.SaveUserPVPData();
            
            SceneUILayerManager.Instance.PopUILayer(this);
        }
        
        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
