using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameMainStateTrialDungeonUI : IGameStateUI
    {
        private InGameUI _inGameUI;
        private SpecDungeonTrial _specTrialDungeon;

        private float _updateTimer = 0f;
        private IGameStateUI _gameStateUIImplementation;
        private const float UpdateInterval = 0.2f;
        private const float InGameMaxTime = 60f;

        public async UniTask Initialize(Transform canvasTransform, int id)
        {
            _specTrialDungeon = SpecDataManager.Instance.GetSpecDungeonTrialData(id);

            bool isFirstTrial = _specTrialDungeon.dungeon_map_id == 1 &&
                                Preference.LoadPreference(Pref.FIRST_TRIAL, true);
            if (isFirstTrial) 
            {
                Preference.SavePreference(Pref.FIRST_TRIAL, false);
                var fxResource = await Addressables.LoadAssetAsync<GameObject>($"VFX/Prefab/Prefab_Dungeon_Boss_01.prefab").Task;
                var animator = Object.Instantiate(fxResource).GetComponent<Animator>();
                await WaitUntilAnimationFinished(animator, "Prefab_Dungeon_Boss_01");
            }

            GameObject stageUIObj = null;
            if (_specTrialDungeon.dungeon_map_id == 1)
                stageUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/TrialBossUI.prefab").Task;
            else
                stageUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/StageUI.prefab").Task;
            
            _inGameUI = Object.Instantiate(stageUIObj, canvasTransform).GetComponent<InGameUI>();
            _inGameUI.transform.SetSiblingIndex(2);

            _inGameUI.TopUI.SetMyName(UserDataManager.Instance.UserBasicData.Nickname);
            _inGameUI.TopUI.SetStageName(StringUtil.GetTrialDungeonString(_specTrialDungeon));
            InGameManager.Instance.StartInGame<FlowStateTrialDungeonReady>(_specTrialDungeon);
        }

        public UniTask Initialize(Transform canvasTransform, UserPVPBattleDetailData data)
        {
            throw new System.NotImplementedException();
        }
        
        public void InitReadyStateUI(List<UserCharacterBattleDeck> battleDeckList)
        {
            _inGameUI.BottomUI.InitData();
            RefreshInGameTopUI(false);
            InGameMain.GetInGameMain().SetInGameTime(InGameMaxTime);
            _inGameUI.TopUI.InitTopUI(typeof(FlowStateTrialDungeonFail));
            _inGameUI.BottomUI.InitReadyStateUI(typeof(FlowStateTrialDungeonCombat), battleDeckList);
        }
        
        public void InitCombatStateUI()
        {
            _inGameUI.PlayAnimation("SetBattleEntry");
            _inGameUI.BottomUI.InitCommanderSkill();
            _inGameUI.BottomUI.InitSpeedUpSetting();
            InGameMain.GetInGameMain().RefreshInGameTopUI(true);
            
            bool isOpenStatisticPop = Preference.LoadPreference(Pref.STATISTIC, false);
            if (isOpenStatisticPop)
                SceneUILayerManager.Instance.PushUILayerAsync<BattleStatisticsPopup>(_inGameUI.BottomUI).Forget();
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

        public void SetFocusSlotUI(SpecCharacter spec)
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
            return tile.IsOccupied() && tile.OccupiedCharacter.AllianceType == AllianceType.Player;
        }

        public void AddKillLog(CharacterController kill, CharacterController death, bool isPlayerKill)
        {
            _inGameUI.TopUI.AddKillLog(kill, death, isPlayerKill);
        }

        private async UniTask WaitUntilAnimationFinished(Animator animator, string animationName)
        {
            await UniTask.WaitWhile(() => animator.GetCurrentAnimatorStateInfo(0).IsName(animationName) && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1);
        }
    }
}
