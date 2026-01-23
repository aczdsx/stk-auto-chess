using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler.Prologue
{
    public class InGameMainStatePrologue : IGameStateUICore, IKillLogUI, ISkipUI, IBottomScrollRectCheck
    {
        private InGameUI _inGameUI;

        private float _updateTimer = 0f;
        private const float UpdateInterval = 0.2f;
        private const float InGameMaxTime = 60f;

        public async UniTask Initialize(Transform canvasTransform, int id)
        {
            var stageUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/PrologueUI.prefab").Task;
            _inGameUI = Object.Instantiate(stageUIObj, canvasTransform).GetComponent<InGameUI>();
            _inGameUI.transform.SetSiblingIndex(2);
            PrologueUtility.PrologueStageUI = stageUIObj;
            _inGameUI.TopUI.SetMyName(UserDataManager.Instance.UserBasicData.Nickname);
            _inGameUI.TopUI.SetStageName("프롤로그");

            InGameManager.Instance.StartInGame<FlowStatePrologueReady>(null as StageInfo);
        }


        public void InitCombatStateUI()
        {
            _inGameUI.PlayAnimation("SetBattleEntry");
            // _inGameUI.BottomUI.InitCommanderSkill();
            _inGameUI.BottomUI.InitSpeedUpSetting();
            InGameMain.GetInGameMain().RefreshInGameTopUI(true);

            bool isOpenStatisticPop = Preference.LoadPreference(Pref.STATISTIC, false);
            if (isOpenStatisticPop)
                SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(_inGameUI.BottomUI).Forget();
        }

        public void RefreshInGameTopUI(bool isCombat)
        {
            // _inGameUI.TopUI.UpdateSynergyUI(AllianceType.Player, isCombat);
            // _inGameUI.TopUI.UpdateSynergyUI(AllianceType.Enemy, isCombat);

            // _inGameUI.TopUI.UpdateAttrUI(AllianceType.Player, isCombat);
            // _inGameUI.TopUI.UpdateAttrUI(AllianceType.Enemy, isCombat);
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
            if (InGameMainFlowManager.Instance.CurrentFlowState is StateCombatBase && InGameManager.Instance.IsInGameCombat)
            {
                _updateTimer += dt;
                InGameMain.GetInGameMain().SetInGameTime(InGameMain.GetInGameMain().InGameTime - dt);
                _inGameUI.BottomUI.UpdateCommanderSkillCoolTime();

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
            _inGameUI.BottomUI.InitData();
            RefreshInGameTopUI(false);
            InGameMain.GetInGameMain().SetInGameTime(InGameMaxTime);
            _inGameUI.TopUI.InitTopUI(typeof(FlowStateStageFail));
            _inGameUI.BottomUI.InitReadyStateUI(typeof(FlowStatePrologueCombat), battleDeckList);
        }

        public void SetFocusSlotUI(CharacterInfo spec)
        {
            _inGameUI.BottomUI.SetFocusCharacterUI(spec);
        }

        public void UnSetFocusSlotUI(bool isDropFx)
        {
            _inGameUI.BottomUI.UnSetFocusCharacterUI(isDropFx);
        }

        public bool IsCheckTouchTile(InGameTile tile)
        {
            return tile.IsOccupied() && tile.View.AllianceType == AllianceType.Player;
        }

        public void AddKillLog(in CookApps.AutoBattler.KillSource source, CharacterController death, bool isPlayerKill)
        {
            _inGameUI.TopUI.AddKillLog(source, death, isPlayerKill);
        }

        public void AddKillLog(CharacterController kill, CharacterController death, bool isPlayerKill)
        {
            var ks = CookApps.AutoBattler.KillSource.From(kill, isPlayerKill);
            _inGameUI.TopUI.AddKillLog(ks, death, isPlayerKill);
        }

        public void AddKillLog(long source, CharacterController death, bool isPlayerKill)
        {
            var ks = CookApps.AutoBattler.KillSource.From(source, isPlayerKill);
            _inGameUI.TopUI.AddKillLog(ks, death, isPlayerKill);
        }

        public void SetAlertBottomCharacter(int characterID)
        {
            _inGameUI.BottomUI.SetAlertBottomCharacter(characterID);
        }

        public void OnSkipRequested()
        {
            InGameMainFlowManager.Instance.AddNextState<FlowStatePrologueClear>(null);
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
