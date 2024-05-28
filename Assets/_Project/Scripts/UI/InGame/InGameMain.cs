using System;
using System.Collections.Generic;
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
        [SerializeField] private Transform _characterSelecteTransform;
        [SerializeField] private List<InGameCharacterItem> _characterItemList;

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
        }

        public override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            InitializeInGame().Forget();
            InGameMainFlowManager.Instance.AddUpdateListener(0, ManagedUpdate);
        }

        public override void OnPreExit()
        {
            base.OnPreExit();
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        }

        public void HideCharacterSelectUI(Action continuation)
        {
            Vector3 startPos = _characterSelecteTransform.transform.position;

            Vector3 endPos = new Vector3(startPos.x, startPos.y - 50, startPos.z);

            PrimeTweenExtensions.MoveTo(_characterSelecteTransform, endPos, 0.5f, PrimeTween.Ease.Linear)
                .OnComplete(() => continuation?.Invoke());

            _characterSelecteTransform.gameObject.SetActive(false);
        }

        public void SetReadyUI(List<CharacterStatData> statDatas)
        {
            for (int i = 0; i < _characterItemList.Count; i++)
            {
                if (i < statDatas.Count)
                {
                    _characterItemList[i].SetData(statDatas[i], AddBoardCharacter);
                }
                else
                {
                    _characterItemList[i].SetData(null, null);
                }
            }
        }

        // private CharacterController ct;
        private void ManagedUpdate(float dt)
        {
            // [TODO] 체력 바 관리 Buff 관리
            // ct.GetEffectCodeContainer().GetEffectCodesByType(EffectCodeType.Buff);
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
            HideCharacterSelectUI(() =>
            {
                InGameMainFlowManager.Instance.AddNextState<FlowStateStageStart>();
            });
        }

        private async void AddBoardCharacter(CharacterStatData statData)
        {
            Debug.Log($"AddBoardCharacter: {statData.CharacterId}");
            var ingameTile = InGameObjectManager.Instance.InGameGrid.GetEmptyTile();
            int2 pos = new int2(ingameTile.X, ingameTile.Y);

            await UniTask.WhenAll(new[]
            {
                InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Player,
                    typeof(CharacterStateIdle)),
            });
        }
    }
}
