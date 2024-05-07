using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class SimpleCloseButton : MonoBehaviour
    {
        public void OnClickClose(UILayer ui)
        {
            SceneUILayerManager.Instance.PopUILayer(ui);
        }
    }
}
