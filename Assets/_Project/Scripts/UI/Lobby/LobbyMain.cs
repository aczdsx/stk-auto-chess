using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Lobby/LobbyMain.prefab")]
    public class LobbyMain : UILayer
    {
        [SerializeField] private CAButton btnStart;

        protected override void Awake()
        {
            base.Awake();
            btnStart.onClick.AddListener(OnClickStart);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            btnStart.onClick.RemoveListener(OnClickStart);
        }

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.Bread, TopPanelType.Coin, TopPanelType.Jewel, TopPanelType.Menu);
        }

        private void OnClickStart()
        {
            int currentStageId = UserDataManager.Instance.GetCurrentStageId();
            SceneUILayerManager.Instance.PushUILayerAsync<ChapterMain>(currentStageId).Forget();
        }
    }
}
