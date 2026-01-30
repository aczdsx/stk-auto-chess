using Cysharp.Text;
using R3;

namespace CookApps.AutoBattler
{
    public class TopPanel_BuildItem : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Elpis_BuildItem;

        private static readonly ItemId CurrencyId = IdMap.Item.BuildItem;
        private InventoryDataBridge _inventoryBridge;

        private void Awake()
        {
            _inventoryBridge = new InventoryDataBridge();

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
    }
}