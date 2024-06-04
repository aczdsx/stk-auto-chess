using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

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
        [SerializeField] private InGameTopUI _InGameTopUI;
        [SerializeField] private InGameBottomCharacterUI _inGameBottomCharacterUI;

        private float _updateTimer = 0f;
        private float _inGameTime = 0f;
        private const float UpdateInterval = 0.5f;
        private const float InGameMaxTime = 60f;
        private SpecStage _specStage;

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
        }

        public void ReturnObjectActive(bool active)
        {
            _inGameBottomCharacterUI.ReturnObjectActive(active);
        }

        public void ReturnCharacter(CharacterController characterController)
        {
            InGameObjectManager.Instance.RemoveCharacterFromField(characterController);
            _inGameBottomCharacterUI.ReturnCharacter(characterController);
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

        public void SetReadyUI()
        {
            _inGameBottomCharacterUI.InitData(() =>
            {
                _InGameTopUI.UpdateSynergyUI(AllianceType.Player);
            });

            _InGameTopUI.UpdateSynergyUI(AllianceType.Player);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy);

            _inGameTime = InGameMaxTime;
        }

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

        private async UniTask InitializeInGame()
        {
            _specStage = InGameResourceHolder.SpecStage;

            InGameManager.Instance.StartInGame<FlowStateStageReady>(_specStage);
        }
    }
}
