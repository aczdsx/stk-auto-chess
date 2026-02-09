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
        private InventoryModel _inventoryModel;

        private void Awake()
        {
            _inventoryModel = ServerDataManager.Instance.Inventory;

            _topPanelButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickTopPanelButton()).AddTo(this);

            _inventoryModel.OnCurrencyChanged
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
            var ap = _inventoryModel.ActionPoint;
            currencyText.SetText(ap?.Current ?? 0);
        }

        private void OnClickTopPanelButton()
        {
            //SceneUILayerManager.Instance.PushUILayerAsync<IdleRewardPopup>().Forget();
        }
    }
}
