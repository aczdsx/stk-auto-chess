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
        public float InGameTime => _inGameTime;
        [SerializeField] private InGameTopUI _InGameTopUI;
        [SerializeField] private InGameBottomCharacterUI _inGameBottomCharacterUI;

        private float _updateTimer = 0f;
        private float _inGameTime = 0f;
        private const float UpdateInterval = 0.3f;
        private const float InGameMaxTime = 60f;

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
        }

        public void ReturnObjectActive(bool active)
        {
            // _inGameBottomCharacterUI.ReturnObjectActive(active);
        }

        public void ReturnObjectColorChange(bool active)
        {
            // _inGameBottomCharacterUI.ReturnObjectColorChange(active);
        }

        public void ReturnCharacter(CharacterController characterController)
        {
            InGameObjectManager.Instance.RemoveCharacterFromField(characterController);
            _inGameBottomCharacterUI.ReturnCharacter(characterController);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            (int chapter, int stageIndex, DifficultyType difficultyType) = ((int, int, DifficultyType)) param;
            var specStage = SpecDataManager.Instance.GetStageData(chapter, stageIndex, difficultyType);
            InGameManager.Instance.StartInGame<FlowStateStageReady>(specStage, specStage);
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
                _InGameTopUI.UpdateAttrUI(AllianceType.Player);
            });

            _InGameTopUI.UpdateSynergyUI(AllianceType.Player);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy);

            _InGameTopUI.UpdateAttrUI(AllianceType.Player);
            _InGameTopUI.UpdateAttrUI(AllianceType.Enemy);

            _inGameTime = InGameMaxTime;

            // 다이얼로그 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_START, InGameManager.Instance.SpecStage.stage_id.ToString());
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
    }
}
