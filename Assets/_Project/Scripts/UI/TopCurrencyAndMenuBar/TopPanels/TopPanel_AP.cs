using CookApps.TeamBattle.UIManagements;
using UnityEngine;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;

namespace CookApps.AutoBattler
{
    public class TopPanel_AP : TopPanelBase
    {
        [SerializeField] private CAButton _topPanelButton;

        public override TopPanelType PanelType => TopPanelType.AP;

        private static readonly ItemId CurrencyId = IdMap.Item.ActionPoint;
        private InventoryDataBridge _inventoryBridge;

        private void Awake()
        {
            _inventoryBridge = new InventoryDataBridge();

            _topPanelButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickTopPanelButton()).AddTo(this);

            _inventoryBridge.OnCurrencyChanged
                .Where(this, (x, self) => x.itemId == CurrencyId && self.CachedGo.activeInHierarchy)
                .Subscribe(this, (x, self) => self.UpdateCurrencyText(x.newAmount))
                .AddTo(this);
        }

        private void OnEnable()
        {
            UpdateCurrencyText(_inventoryBridge.GetCurrency(CurrencyId));
        }

        private void UpdateCurrencyText(ulong amount)
        {
            currencyText.SetText(amount);
        }

        private void OnClickTopPanelButton()
        {
            SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
        }
    }
}
