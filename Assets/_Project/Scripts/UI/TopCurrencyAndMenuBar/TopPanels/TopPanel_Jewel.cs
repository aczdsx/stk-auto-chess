using Cysharp.Text;
using R3;

namespace CookApps.AutoBattler
{
    public class TopPanel_Jewel : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Jewel;

        private static readonly ItemId CurrencyId = IdMap.Item.Jewel;
        private InventoryModel _inventoryModel;

        private void Awake()
        {
            _inventoryModel = ServerDataManager.Instance.Inventory;

            _inventoryModel.OnCurrencyChanged
                .Where(this, (x, self) => x.itemId == CurrencyId && self.CachedGo.activeInHierarchy)
                .Subscribe(this, (x, self) => self.UpdateCurrencyText(x.newAmount))
                .AddTo(this);
        }

        private void OnEnable()
        {
            UpdateCurrencyText(_inventoryModel.GetCurrency(CurrencyId));
        }

        private void UpdateCurrencyText(ulong amount)
        {
            currencyText.SetText(amount);
        }
    }
}
