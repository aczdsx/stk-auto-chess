using Cysharp.Text;
using R3;

namespace CookApps.AutoBattler
{
    public class TopPanel_BuildItem : TopPanelBase
    {
        public override TopPanelType PanelType => TopPanelType.Elpis_BuildItem;

        private static readonly ItemId CurrencyId = IdMap.Item.BuildItem;
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