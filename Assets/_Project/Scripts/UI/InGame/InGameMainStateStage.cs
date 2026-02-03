using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameMainStateStage : IGameStateUICore, IReturnCharacterUI, IGuideBottomUI, IFocusSlotUI, IKillLogUI, IAlertBottomCharacterUI, ICommanderSkillUI, IBottomScrollRectCheck
    {
        private InGameUI _inGameUI;
        private StageInfo _specStage;

        private float _updateTimer = 0f;
        private const float UpdateInterval = 0.3f;
        private const float InGameMaxTime = 60f;

        public async UniTask Initialize(Transform canvasTransform, int id)
        {
            var stageUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/StageUI.prefab").Task;
            _inGameUI = Object.Instantiate(stageUIObj, canvasTransform).GetComponent<InGameUI>();
            _inGameUI.transform.SetSiblingIndex(2);

            _specStage = SpecDataManager.Instance.GetStageData(id);
            if (_specStage.chapter_id == 1)
            {
                string defaultName = LanguageManager.Instance.GetDefaultText("STELLA_KNIGHT");
                _inGameUI.TopUI.SetMyName(defaultName);
            }
            else
            {
                _inGameUI.TopUI.SetMyName(UserDataManager.Instance.UserBasicData.Nickname);
            }
            
            string stageString = LanguageManager.Instance.GetDefaultText("UI_STAGE");
            _inGameUI.TopUI.SetStageName($"{stageString} {_specStage.chapter_id}-{_specStage.stage_number}");
            _inGameUI.TopUI.SetElementCounterUI(_specStage.stage_elemental);

            InGameManager.Instance.StartInGame<FlowStateStageReady>(_specStage);

            // 최근 플레이 스테이지 저장
            LocalDataManager.Instance.SetLastPlayStageId((uint)_specStage.stage_id);

            // 유저 레벨업 체크용 이전 레벨 데이터 저장
            UserDataManager.Instance.PrevAccountLevel = UserDataManager.Instance.UserBasicData.Level;
        }

        public void RefreshInGameTopUI(bool isCombat)
        {
            _inGameUI.TopUI.UpdateSynergyUI(AllianceType.Player, isCombat);
            _inGameUI.TopUI.UpdateSynergyUI(AllianceType.Enemy, isCombat);

            _inGameUI.TopUI.UpdateAttrUI(AllianceType.Player, isCombat);
            _inGameUI.TopUI.UpdateAttrUI(AllianceType.Enemy, isCombat);
        }

        public void ReturnCharacterUI(CharacterController characterController)
        {
            _inGameUI.BottomUI.ReturnCharacter(characterController);
            InGameManager.Instance.UpdateSynergyAndAttr();
        }

        public void SetInGameBottomUIInGuideUI()
        {
            _inGameUI.BottomUI.CheckNewCharacter();
        }

        public void ManagedUpdate(float dt)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase)
            {
                _updateTimer += dt;
                if (InGameManager.Instance.IsInGameCombat)
                {
                    InGameMain.GetInGameMain().SetInGameTime(InGameMain.GetInGameMain().InGameTime - dt);
                    _inGameUI.BottomUI.UpdateCommanderSkillCoolTime();
                }

                if (_updateTimer >= UpdateInterval)
                {
                    _inGameUI.TopUI.UpdateTopHpUI(AllianceType.Player);
                    _inGameUI.TopUI.UpdateTopHpUI(AllianceType.Enemy);
                    _inGameUI.TopUI.UpdateTimeUI(InGameMain.GetInGameMain().InGameTime);

                    _updateTimer -= UpdateInterval;
                }
            }
        }

        public void InitReadyStateUI(List<DeckCharacterPlacement> battleDeckList)
        {
            _inGameUI.PlayAnimation("SetEntry");
            _inGameUI.BottomUI.InitData();
            RefreshInGameTopUI(false);
            InGameMain.GetInGameMain().SetInGameTime(InGameMaxTime);
            _inGameUI.TopUI.InitTopUI(typeof(FlowStateStageFail));
            _inGameUI.BottomUI.InitReadyStateUI(typeof(FlowStateStageCombat), battleDeckList);

            // 다이얼로그 체크
            // DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.STAGE_START,
            //     InGameManager.Instance.SpecStage.stage_id.ToString());
        }

        public void InitCombatStateUI()
        {
            _inGameUI.PlayAnimation("SetBattleEntry");
            _inGameUI.BottomUI.InitCommanderSkill();
            _inGameUI.BottomUI.InitSpeedUpSetting();
            _inGameUI.TopUI.InitCombatTopUI();

            InGameMain.GetInGameMain().RefreshInGameTopUI(true);
            
            bool isOpenStatisticPop = Preference.LoadPreference(Pref.STATISTIC, false);
            if (isOpenStatisticPop && !_inGameUI.BottomUI.IsSpeedUpRedDot)
                SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(_inGameUI.BottomUI).Forget();
        }

        public void SetFocusSlotUI(CharacterInfo spec)
        {
            _inGameUI.BottomUI.SetFocusCharacterUI(spec);
        }

        public void UnSetFocusSlotUI(bool isDropFx)
        {
            _inGameUI.BottomUI.UnSetFocusCharacterUI(isDropFx);
        }

        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            _inGameUI.BottomUI.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }
        
        public bool IsCheckTouchTile(InGameTile tile)
        {
            return tile.IsOccupied() && (tile.OccupiedCharacter.AllianceType == AllianceType.Player);
        }
        
        public void SetAlertBottomCharacter(int characterID)
        {
            _inGameUI.BottomUI.SetAlertBottomCharacter(characterID);
        }

        public void AddKillLog(in CookApps.AutoBattler.KillSource source, CharacterController death, bool isPlayerKill)
        {
            _inGameUI.TopUI.AddKillLog(source, death, isPlayerKill);
        }

        public bool IsPointInBottomScrollRect(UnityEngine.Vector2 screenPosition)
        {
            return _inGameUI.BottomUI.IsPointInScrollRect(screenPosition);
        }

        public void SetDropHighlight(bool active)
        {
            _inGameUI.BottomUI.SetDropHighlight(active);
        }
    }
}