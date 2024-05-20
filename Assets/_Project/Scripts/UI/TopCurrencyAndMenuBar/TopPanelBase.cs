using CookApps.TeamBattle;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public enum TopPanelType
    {
        Gold = 0,
        Energy,
        Jewel,
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
