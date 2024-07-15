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
        [SerializeField] private Material _chapter1VignetteMaterial; // [TODO] 임시 작업
        [SerializeField] private Material _defaultVignetteMaterial; // [TODO] 임시 작업
        [SerializeField] private Image _commanderBgImage;

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
            InGameObjectManager.Instance.ClearSynergyFx();
            InGameObjectManager.Instance.RemoveCharacterFromField(characterController);
            _inGameBottomCharacterUI.ReturnCharacter(characterController);
            SetInGameTopUI();
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            (int chapter, int stageIndex, DifficultyType difficultyType) = ((int, int, DifficultyType)) param;
            var specStage = SpecDataManager.Instance.GetStageData(chapter, stageIndex, difficultyType);
            InGameManager.Instance.StartInGame<FlowStateStageReady>(specStage, specStage);
            InGameMainFlowManager.Instance.AddUpdateListener(0, ManagedUpdate);
            _vignetteImage.material = (chapter == 1) ? _chapter1VignetteMaterial : _defaultVignetteMaterial;
            _vignetteImage.material.SetColor("_DotColor", _stageVignetteColorList[chapter - 1]);

            // 최근 플레이 스테이지 저장
            UserDataManager.Instance.SetLastPlayStageID(specStage.stage_id, true);

            // 유저 레벨업 체크용 이전 레벨 데이터 저장
            UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;

            _InGameTopUI.SetStageName($"스테이지 {chapter}-{stageIndex}");

            // 사운드 재생
            PlayStageBGM(chapter);
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

        private void PlayStageBGM(int targetChapter)
        {
            switch (targetChapter)
            {
                case 1:
                    SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter0);
                    break;
                case 2:
                    SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter1);
                    break;
                case 3:
                    SoundManager.Instance.PlayBGM(SoundBGM.snd_bgm_chapter2);
                    break;
            }
        }

        public void UpdateCommanderSkillCoolTime()
        {
            _inGameBottomCharacterUI.UpdateCommanderSkillCoolTime();
        }

        public void SetIconColor(float fadeAlpha)
        {
            _commanderBgImage.color = new Color(0, 0, 0, fadeAlpha);
            // _inGameBottomCharacterUI.SetIconColor(fadeAlpha);
        }

        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            _inGameBottomCharacterUI.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }

        public void SetCommanderFx(bool active)
        {
            _inGameBottomCharacterUI.SetCommanderFx(active);
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
