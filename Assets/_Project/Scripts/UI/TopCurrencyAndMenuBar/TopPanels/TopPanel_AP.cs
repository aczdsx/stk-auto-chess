using CookApps.TeamBattle.UIManagements;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

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
            UpdateCurrencyText();
        }

        private void UpdateCurrencyText(ulong _ = 0)
        {
            var ap = _inventoryBridge.ActionPoint;
            currencyText.SetText(ap?.Current ?? 0);
        }

        private void OnClickTopPanelButton()
        {
            //SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
        }
    }
}
