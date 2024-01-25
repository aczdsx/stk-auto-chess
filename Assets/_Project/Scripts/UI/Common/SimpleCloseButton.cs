using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class SimpleCloseButton : MonoBehaviour
    {
        public void OnClickClose(UILayer ui)
        {
            SceneUIManager.Instance.PopUILayer(ui);
        }
    }
}
