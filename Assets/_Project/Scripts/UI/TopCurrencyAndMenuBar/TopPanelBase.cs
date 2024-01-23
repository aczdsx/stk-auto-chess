using CookApps.TeamBattle;
using TMPro;
using UnityEngine;

public enum TopPanelType
{
    Coin = 0,
    Bread,
    Jewel,
    ETicket,
    KTicket,
    Menu,
    CloseButton,
}

public abstract class TopPanelBase : CachedMonoBehaviour
{
    [SerializeField] protected TMP_Text currencyText;
    public abstract TopPanelType PanelType { get; }
}
