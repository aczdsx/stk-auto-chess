using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class AccountLevelUpWindowPopup : UILayer
    {
        [SerializeField] private CAButton expandButton;

        

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
        }
    }
}
