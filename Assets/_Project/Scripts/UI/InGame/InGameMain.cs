using System;
using System.Collections.Generic;
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
        [SerializeField] private Transform _characterSelecteTransform;
        [SerializeField] private List<InGameCharacterItem> _characterItemList;
        [SerializeField] private InGameTopUI _InGameTopUI;
        //[SerializeField] private InGameBottomCharacterUI _inGameBottomCharacterUI;

        private float _updateTimer = 0f;
        private float _inGameTime = 0f;
        private const float UpdateInterval = 0.5f;
        private const float InGameMaxTime = 60f;
        private SpecStage _specStage;

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            InitializeInGame().Forget();
            InGameMainFlowManager.Instance.AddUpdateListener(0, ManagedUpdate);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        }

        public void HideCharacterSelectUI(Action continuation)
        {
            Vector3 startPos = _characterSelecteTransform.transform.position;

            Vector3 endPos = new Vector3(startPos.x, startPos.y - 300, startPos.z);

            PrimeTweenExtensions.MoveTo(_characterSelecteTransform, endPos, 0.5f, PrimeTween.Ease.Linear)
                .OnComplete(() =>
                {
                    continuation?.Invoke();
                });
        }

        public void SetReadyUI()
        {
            //[TODO] 내가 보유한 캐릭터들 가져오도록 수정 필요
            List<CharacterStatData> characterStats = new List<CharacterStatData>();
            characterStats.Add(new CharacterStatData(40101, 10));
            characterStats.Add(new CharacterStatData(30601, 10));
            characterStats.Add(new CharacterStatData(40402, 10));

            Debug.LogColor("SetReadyUI");
            for (int i = 0; i < _characterItemList.Count; i++)
            {
                if (i < characterStats.Count)
                {
                    _characterItemList[i].SetData(characterStats[i], AddCharacterToTile);
                }
                else
                {
                    _characterItemList[i].SetData(null, null);
                }
            }

            _InGameTopUI.UpdateSynergyUI(AllianceType.Player);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy);

            _inGameTime = InGameMaxTime;
        }

        // private CharacterController ct;
        private void ManagedUpdate(float dt)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)
            {
                _updateTimer += dt;
                _inGameTime -= dt;

                if (_updateTimer >= UpdateInterval)
                {
                    _InGameTopUI.UpdateTopHpUI(AllianceType.Player);
                    _InGameTopUI.UpdateTopHpUI(AllianceType.Enemy);
                    _InGameTopUI.UpdateTimeUI(_inGameTime);

                    _updateTimer -= UpdateInterval;
                }
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _startButton?.onClick.AddListener(OnStartButtonClicked);
        }

        private async UniTask InitializeInGame()
        {
            _specStage = InGameResourceHolder.SpecStage;

            InGameManager.Instance.StartInGame<FlowStateStageReady>(_specStage);
        }

        private void OnStartButtonClicked()
        {
            HideCharacterSelectUI(() =>
            {
                InGameMainFlowManager.Instance.AddNextState<FlowStateStageStart>();
            });
        }

        private async void AddCharacterToTile(CharacterStatData statData)
        {
            Debug.Log($"AddBoardCharacter: {statData.CharacterId}");
            var ingameTile = InGameObjectManager.Instance.InGameGrid.GetEmptyTile();
            int2 pos = new int2(ingameTile.X, ingameTile.Y);

            await UniTask.WhenAll(new[]
            {
                InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Player,
                    typeof(CharacterStateIdle)),
            });

            _InGameTopUI.UpdateSynergyUI(AllianceType.Player);
        }
    }
}
