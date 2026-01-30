using CookApps.TeamBattle;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum TopPanelType
    {
        Gold = 0,
        AP,
        Jewel,
        C_Ticket,
        PVP_Ticket,
        Elpis_BuildItem,

        Char_User_Exp_Item,
        Char_User_Exp_Item_2,

        Menu,
        CloseButton,
    }

    public abstract class TopPanelBase : CachedMonoBehaviour
    {
        [SerializeField] protected TMP_Text currencyText;
        public abstract TopPanelType PanelType { get; }
        public TopCurrencyAndMenuBar attachedTopBar { get; set; }
    }
}
