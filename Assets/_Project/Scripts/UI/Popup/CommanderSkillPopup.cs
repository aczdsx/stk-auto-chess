using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.TeamBattle.UI;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;


namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Popup, "Prefabs/UI/Popup/CommanderSkillPopup.prefab")]
    public class CommanderSkillPopup : UILayer
    {
        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);
        }
    }
}
