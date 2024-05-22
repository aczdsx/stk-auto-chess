using System.Linq;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace CookApps.AutoBattler
{
    class InGameData
    {
        private int stageId;
        // char///
    }

    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/InGame/InGameMain.prefab")]
    public class InGameMain : UILayer
    {
        [SerializeField] private CAButton _startButton;
        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            InitializeInGame().Forget();
        }

        protected override void Awake()
        {
            base.Awake();
            _startButton?.onClick.AddListener(OnStartButtonClicked);
        }

        private async UniTask InitializeInGame()
        {
            GameObject stageObj = Instantiate(InGameResourceHolder.StagePrefab);
            if (!stageObj.TryGetComponent(out InGameStage stage))
            {
                Debug.LogError("InGameStage is not found");
                return;
            }

            InGameManager.Instance.StartInGame<FlowStateStageReady>(stage);
        }

        private void OnStartButtonClicked()
        {
            SceneLoading.GoToNextScene("InGame", (1, 1)).Forget();
            // InGameMainFlowManager.Instance.AddNextState<FlowStateStageStart>();
        }
    }
}
