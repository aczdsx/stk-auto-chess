using System;
using System.Collections.Generic;
using System.Linq;
using Cookapps.Autobattleproject.V1;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public interface IGameStateUI
    {
        void SetInGameBottomUI();
        void SetInGameTopUI();
        void Initialize(int id);
        void PlayBGM();
        string GetStageName();
    }

    [RegisterUILayer(UILayerType.Cover, "Prefabs/UI/InGame/InGameMain.prefab")]
    public class InGameMain : UILayer
    {
        public float InGameTime => _inGameTime;
        [SerializeField] private InGameTopUI _InGameTopUI;
        [SerializeField] private InGameBottomCharacterUI _inGameBottomCharacterUI;
        [SerializeField] private List<Color> _stageVignetteColorList;
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private Animator _sceneAnimator;
        [SerializeField] private Material _chapter1VignetteMaterial; // [TODO] 임시 작업
        [SerializeField] private Material _defaultVignetteMaterial; // [TODO] 임시 작업

        private float _updateTimer = 0f;
        private float _inGameTime = 0f;
        private const float UpdateInterval = 0.3f;
        private const float InGameMaxTime = 60f;
        private IGameStateUI _currentGameStateUI;

        public static InGameMain GetInGameMain()
        {
            return SceneUILayerManager.Instance.GetUILayer<InGameMain>();
        }

        public void ReturnCharacter(CharacterController characterController)
        {
            _inGameBottomCharacterUI.ReturnCharacter(characterController);
            InGameManager.Instance.UpdateSynergyAndAttr();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            (IGameStateUI gameState, int id) = ((IGameStateUI, int)) param;
            _currentGameStateUI = gameState;
            _currentGameStateUI.Initialize(id);
            _InGameTopUI.SetStageName(_currentGameStateUI.GetStageName());
            _currentGameStateUI.PlayBGM();

            // [TODO] stage에서만 되도록 작업 필요
            // _vignetteImage.material = (chapter == 1) ? _chapter1VignetteMaterial : _defaultVignetteMaterial;
            // _vignetteImage.material.SetColor("_DotColor", _stageVignetteColorList[chapter - 1]);

            InGameMainFlowManager.Instance.AddUpdateListener(0, ManagedUpdate);
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        }

        private void SetInGameBottomUI()
        {
            _inGameBottomCharacterUI.InitData();
        }

        public void SetInGameBottomUIInGuide()
        {
            _inGameBottomCharacterUI.CheckNewCharacter();
        }

        public void PlaySceneAnimation(string name)
        {
            _sceneAnimator.SetTrigger(name);
        }

        public void SetReadyUI()
        {
            SetInGameBottomUI();
            SetInGameTopUI();
            _inGameTime = InGameMaxTime;

            // 다이얼로그 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_START, InGameManager.Instance.SpecStage.stage_id.ToString());
        }

        public void AddCharacter(List<UserCharacterBattleDeck> battleDeckList)
        {
            _inGameBottomCharacterUI.AddCharacter(battleDeckList);
        }
        public void SetInGameTopUI()
        {
            InGameObjectManager.Instance.ClearSynergyFx();
            _InGameTopUI.UpdateSynergyUI(AllianceType.Player, true);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy, true);

            _InGameTopUI.UpdateAttrUI(AllianceType.Player);
            _InGameTopUI.UpdateAttrUI(AllianceType.Enemy);
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

        public void UpdateCommanderSkillCoolTime()
        {
            _inGameBottomCharacterUI.UpdateCommanderSkillCoolTime();
        }

        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            _inGameBottomCharacterUI.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }

        public void SetFocusSlot(SpecCharacter spec)
        {
            _inGameBottomCharacterUI.SetFocusCharacter(spec);
        }

        public void UnSetFocusSlot(bool isDropFx)
        {
            _inGameBottomCharacterUI.UnSetFocusCharacter(isDropFx);
        }

        public void SetCombatUI()
        {
            _InGameTopUI.SetCombatUI();
        }

        public void OpenStatisticPop()
        {
            _inGameBottomCharacterUI.OpenStatisticPop();
        }

        public void ShowSKillTooltip(CharacterStatData getCharacterStat)
        {
            _inGameBottomCharacterUI.ShowSKillTooltip(getCharacterStat);
        }

        public void CloseSkillTooltip()
        {
            _inGameBottomCharacterUI.CloseSkillTooltip();
        }
    }
}
