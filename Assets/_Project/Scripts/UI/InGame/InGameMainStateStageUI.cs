using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameMainStateUIStageUI : IGameStateUI
    {
        private InGameTopUI _InGameTopUI;
        private InGameBottomCharacterUI _inGameBottomCharacterUI;
        private SpecStage _specStage;

        private float _updateTimer = 0f;
        private const float UpdateInterval = 0.3f;
        private const float InGameMaxTime = 60f;

        public void RefreshInGameTopUI()
        {
            InGameObjectManager.Instance.ClearSynergyFx();
            _InGameTopUI.UpdateSynergyUI(AllianceType.Player, true);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy, true);

            _InGameTopUI.UpdateAttrUI(AllianceType.Player);
            _InGameTopUI.UpdateAttrUI(AllianceType.Enemy);
        }

        public async UniTask Initialize(Transform canvasTransform, int id)
        {
            var topUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/Top.prefab").Task;
            var bottomUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/Bottom.prefab").Task;

            _InGameTopUI = GameObject.Instantiate(topUIObj, canvasTransform).GetComponent<InGameTopUI>();
            _inGameBottomCharacterUI = GameObject.Instantiate(bottomUIObj, canvasTransform)
                .GetComponent<InGameBottomCharacterUI>();

            _specStage = SpecDataManager.Instance.GetStageData(id);
            _InGameTopUI.SetStageName($"스테이지 {_specStage.chapter_id}-{_specStage.stage_number}");

            InGameManager.Instance.StartInGame<FlowStateStageReady>(_specStage, _specStage);

            // 최근 플레이 스테이지 저장
            UserDataManager.Instance.SetLastPlayStageID(_specStage.stage_id, true);

            // 유저 레벨업 체크용 이전 레벨 데이터 저장
            UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;

            PlayBGM();
            InGameMain.GetInGameMain().SetVignette(_specStage.chapter_id);
        }

        public void ReturnCharacter(CharacterController characterController)
        {
            _inGameBottomCharacterUI.ReturnCharacter(characterController);
            InGameManager.Instance.UpdateSynergyAndAttr();
        }

        public void AddCharacter(List<UserCharacterBattleDeck> battleDeckList)
        {
            _inGameBottomCharacterUI.AddCharacter(battleDeckList);
        }

        public void SetInGameBottomUIInGuide()
        {
            _inGameBottomCharacterUI.CheckNewCharacter();
        }

        public void ManagedUpdate(float dt)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)
            {
                _updateTimer += dt;
                InGameMain.GetInGameMain().SetInGameTime(InGameMain.GetInGameMain().InGameTime - dt);

                if (_updateTimer >= UpdateInterval)
                {
                    _InGameTopUI.UpdateTopHpUI(AllianceType.Player);
                    _InGameTopUI.UpdateTopHpUI(AllianceType.Enemy);
                    _InGameTopUI.UpdateTimeUI(InGameMain.GetInGameMain().InGameTime);

                    _updateTimer -= UpdateInterval;
                }
            }
        }

        public void SetReadyUI()
        {
            _inGameBottomCharacterUI.InitData();
            RefreshInGameTopUI();
            InGameMain.GetInGameMain().SetInGameTime(InGameMaxTime);

            // 다이얼로그 체크
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_START, InGameManager.Instance.SpecStage.stage_id.ToString());
        }

        public void UpdateCommanderSkillCoolTime()
        {
            _inGameBottomCharacterUI.UpdateCommanderSkillCoolTime();
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

        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            _inGameBottomCharacterUI.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }

        public int GetVignetteID()
        {
            return _specStage.chapter_id - 1;
        }

        private void PlayBGM()
        {
            switch (_specStage.chapter_id)
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
    }
}
