using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Lobby/LobbyMain.prefab")]
    public class LobbyMain : UILayer
    {
        [SerializeField] private CAButton commanderSkillButton;

        protected override void Awake()
        {
            base.Awake();
            commanderSkillButton.onClick.AddListener(OnClickCommanderSkillButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            commanderSkillButton.onClick.RemoveListener(OnClickCommanderSkillButton);
        }

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Coin, TopPanelType.Jewel, TopPanelType.Menu);
        }

        private void OnClickCommanderSkillButton()
        {
            SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(false);
            SceneUILayerManager.Instance.PushUILayerAsync<CommanderSkillPopup>(null, callbackObject =>
            {
                SceneUILayerManager.Instance.SetEnableFloatingNodeCanvas(true);
            }).Forget();
        }
    }
}
