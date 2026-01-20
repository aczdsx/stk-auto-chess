using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class AccountLevelUpWindowPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton expandButton;

        

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
        }
    }
}
