using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum GachaPopupTabType
    {
        CommonCharacter,
        PickUpCharacter,
    }

    public class GachaPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _backButton;

        [Header("Gacha Tabs")]
        [SerializeField] private CAToggle _gachaCommonCharacterTabToggle;
        [SerializeField] private CAToggle _gachaPickUpCharacterTabToggle;

        [Header("Gacha Layers")]
        [SerializeField] private GachaCommonCharacterLayer _gachaCommonCharacterLayer;
        [SerializeField] private GachaPickUpCharacterLayer _gachaPickUpCharacterLayer;

        private GachaPopupTabType _currentTabType = GachaPopupTabType.CommonCharacter;

        protected override void Awake()
        {
            base.Awake();
            _backButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.C_Ticket, TopPanelType.Jewel);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            _currentTabType = GachaPopupTabType.CommonCharacter;
            if (param != null)
            {
                _currentTabType = (GachaPopupTabType)param;
            }

            InitTabLayer();
            ChangeTabLayer(_currentTabType);

            // 상점 배너 팝업 체크
            ShopPurchaseManager.Instance.UpdateShopBannerConditionValue(ShopBannerConditionType.ENTER_GACHA_POP, 0, 1, false);
            ShopPurchaseManager.Instance.ShowShopBannerPopup(ShopBannerShowType.IMMEDIATE);

            // test
            //DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.POPUP_OPEN, nameof(gameObject));
            
            // 보상 지급 여부 체크
            var progressData = ClientProgressData.Get();
            if (!progressData.HasRewardedFirstGachaTicket())
            {
                var rewardItemList = new List<RewardItem> { new (IdMap.Item.CharacterTicket, 10) };
                // 서버에 보상 수령 요청
                SceneUILayerManager.Instance.PushUILayerAsync<RewardResultPopup>(("REWARD_TITLE", rewardItemList)).Forget();
                progressData.SetRewardedFirstGachaTicket(true);
            }
        }

        public void SetCanvasTargetDisplay(int targetDisplay)
        {
            var canvas = SceneUILayerManager.Instance.MainCanvas;
            if (canvas != null)
                canvas.targetDisplay = targetDisplay;
        }

        private void InitTabLayer()
        {
            _gachaCommonCharacterLayer.SetGachaLayer(this);
            _gachaPickUpCharacterLayer.SetGachaLayer(this);
        }

        public void OnClickCommonCharacterLayerTabButton()
        {
            ChangeTabLayer(GachaPopupTabType.CommonCharacter);
        }

        public void OnClickPickUpCharacterLayerTabButton()
        {
            ChangeTabLayer(GachaPopupTabType.PickUpCharacter);
        }

        private void ChangeTabLayer(GachaPopupTabType targetTabType)
        {
            ClearTabLayer();

            switch (targetTabType)
            {
                case GachaPopupTabType.CommonCharacter:
                    _gachaCommonCharacterTabToggle.isOn = true;
                    _gachaCommonCharacterLayer.gameObject.SetActive(true);
                    break;
                case GachaPopupTabType.PickUpCharacter:
                    _gachaPickUpCharacterTabToggle.isOn = true;
                    _gachaPickUpCharacterLayer.gameObject.SetActive(true);
                    break;
            }
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        private void ClearTabLayer()
        {
            //_gachaCommonCharacterTab.SetActive(false);
            //_gachaPickUpCharacterTab.SetActive(false);

            _gachaCommonCharacterLayer.gameObject.SetActive(false);
            _gachaPickUpCharacterLayer.gameObject.SetActive(false);
        }

        protected override void OnBackButton(ref bool offPrevUI)
        {
        }
    }
}
