using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/01_Pops/LoadingPopup.prefab")]
    public class LoadingPopup : UILayer
    {
        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.PVP_Ticket);
            
            //SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
        }
    }   
}