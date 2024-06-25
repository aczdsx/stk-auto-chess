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
        [SerializeField] private List<Color> _stageVignetteColorList;
        [SerializeField] private RawImage _vignetteImage;
        [SerializeField] private Animator _sceneAnimator;

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
            _vignetteImage.material.SetColor("_DotColor", _stageVignetteColorList[chapter]);

            // 최근 플레이 스테이지 저장
            UserDataManager.Instance.SetLastPlayStageID(specStage.stage_id, true);

            // 유저 레벨업 체크용 이전 레벨 데이터 저장
            UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;
        }

        protected override void OnPreExit()
        {
            base.OnPreExit();
            InGameMainFlowManager.Instance.RemoveUpdateListener(ManagedUpdate);
        }

        public void SetInGameBottomUI()
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
            _InGameTopUI.UpdateSynergyUI(AllianceType.Player);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy);

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

        public void SetCommanderSkillCoolTime(float elapsedTime, float durationTime)
        {
            _inGameBottomCharacterUI.SetCommanderSkillCoolTime(elapsedTime, durationTime);
        }

        public void SetIconColor(float fadeAlpha)
        {
            _inGameBottomCharacterUI.SetIconColor(fadeAlpha);
        }

        public void SetCommanderSkillUI(int equippedCommanderSkillId)
        {
            _inGameBottomCharacterUI.SetCommanderSkillUI(equippedCommanderSkillId);
        }
    }
}
