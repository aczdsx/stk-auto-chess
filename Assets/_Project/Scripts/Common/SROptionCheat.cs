#if !RELEASE || UNITY_EDITOR
using System;
using System.ComponentModel;
using CookApps.AutoBattler;

[Serializable]
public partial class SROptions
{
    [Category("아이템 관련")]
    public void 아이템추가()
    {
        UserDataManager.Instance.IncreaseItem(원하는아이템타입, 원하는아이템갯수, true);
    }

    [Category("아이템 관련")]
    public void 아이템제거()
    {
        UserDataManager.Instance.DecreaseItem(원하는아이템타입, 원하는아이템갯수, true);
    }

    [Category("아이템 관련")]
    public ItemType 원하는아이템타입 { get; set; } = ItemType.GOLD;
    [Category("아이템 관련")]
    public int 원하는아이템갯수 { get; set; } = 0;
}
#endif
