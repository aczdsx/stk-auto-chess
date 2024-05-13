using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.AutoBattler
{
    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/Lobby/LobbyMain.prefab")]
    public class LobbyMain : UILayer
    {
        [SerializeField] private CAButton commanderSkillButton;
        [SerializeField] private CAButton _playButton;
        [SerializeField] private CAButton _stageSelectButton;

        protected override void Awake()
        {
            base.Awake();
            commanderSkillButton.onClick.AddListener(OnClickCommanderSkillButton);
            _playButton.onClick.AddListener(OnClickStartButton);
            _stageSelectButton.onClick.AddListener(OnClickChapterStageButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            commanderSkillButton.onClick.RemoveListener(OnClickCommanderSkillButton);
            _playButton.onClick.RemoveListener(OnClickStartButton);
            _stageSelectButton.onClick.RemoveListener(OnClickChapterStageButton);
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
        private void OnClickStartButton()
        {
            SceneLoading.GoToNextScene("InGame", (1, 1)).Forget();
        }

        private void OnClickChapterStageButton()
        {
            int currentStageId = UserDataManager.Instance.GetCurrentStageId();
            SceneUILayerManager.Instance.PushUILayerAsync<ChapterMain>(currentStageId).Forget();
        }
    }
}
