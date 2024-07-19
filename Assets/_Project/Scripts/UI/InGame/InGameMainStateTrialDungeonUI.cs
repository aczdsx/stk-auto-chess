using System.Collections.Generic;
using Cookapps.Stkauto.V1;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameMainStateTrialDungeonUI : IGameStateUI
    {
        private InGameTopUI _InGameTopUI;
        private InGameBottomCharacterUI _inGameBottomCharacterUI;
        private SpecDungeonTrial _specTrialDungeon;

        private float _updateTimer = 0f;
        private const float UpdateInterval = 0.2f;
        private const float InGameMaxTime = 60f;

        public async UniTask Initialize(Transform canvasTransform, int id)
        {
            var topUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/Top.prefab").Task;
            var bottomUIObj = await Addressables.LoadAssetAsync<GameObject>($"Prefabs/UI/InGame/Bottom.prefab").Task;

            _InGameTopUI = GameObject.Instantiate(topUIObj, canvasTransform).GetComponent<InGameTopUI>();
            _inGameBottomCharacterUI = GameObject.Instantiate(bottomUIObj, canvasTransform)
                .GetComponent<InGameBottomCharacterUI>();

            _specTrialDungeon = SpecDataManager.Instance.GetSpecDungeonTrialData(id);
            _InGameTopUI.SetStageName($"시련 던전 {_specTrialDungeon.dungeon_id}");

            InGameManager.Instance.StartInGame<FlowStateTrialDungeonReady>(_specTrialDungeon);
        }

        public void RefreshInGameTopUI()
        {
            InGameObjectManager.Instance.ClearSynergyFx();
            _InGameTopUI.UpdateSynergyUI(AllianceType.Player, true);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy, true);

            _InGameTopUI.UpdateAttrUI(AllianceType.Player);
            _InGameTopUI.UpdateAttrUI(AllianceType.Enemy);
        }

        public void RefreshInGameTopUI(bool isCombat)
        {
            if (!isCombat)
                InGameObjectManager.Instance.ClearSynergyFx();
            _InGameTopUI.UpdateSynergyUI(AllianceType.Player, isCombat);
            _InGameTopUI.UpdateSynergyUI(AllianceType.Enemy, isCombat);

            _InGameTopUI.UpdateAttrUI(AllianceType.Player);
            _InGameTopUI.UpdateAttrUI(AllianceType.Enemy);
        }

        public void ReturnCharacterUI(CharacterController characterController)
        {
            _inGameBottomCharacterUI.ReturnCharacter(characterController);
            InGameManager.Instance.UpdateSynergyAndAttr();
        }

        public void AddCharacterUI(List<UserCharacterBattleDeck> battleDeckList)
        {
            _inGameBottomCharacterUI.AddCharacter(battleDeckList);
        }

        public void SetInGameBottomUIInGuideUI()
        {
            _inGameBottomCharacterUI.CheckNewCharacter();
        }

        public void ManagedUpdate(float dt)
        {
            if (InGameMainFlowManager.Instance.CurrentFlowState is FlowStateStageCombat)
            {
                _updateTimer += dt;
                InGameMain.GetInGameMain().SetInGameTime(InGameMain.GetInGameMain().InGameTime - dt);
                _inGameBottomCharacterUI.UpdateCommanderSkillCoolTime();

                if (_updateTimer >= UpdateInterval)
                {
                    _InGameTopUI.UpdateTopHpUI(AllianceType.Player);
                    _InGameTopUI.UpdateTopHpUI(AllianceType.Enemy);
                    _InGameTopUI.UpdateTimeUI(InGameMain.GetInGameMain().InGameTime);

                    _updateTimer -= UpdateInterval;
                }
            }
        }

        public void SetReadyStateUI()
        {
            _inGameBottomCharacterUI.InitData();
            RefreshInGameTopUI();
            InGameMain.GetInGameMain().SetInGameTime(InGameMaxTime);
        }

        public void SetFocusSlotUI(SpecCharacter spec)
        {
            _inGameBottomCharacterUI.SetFocusCharacterUI(spec);
        }

        public void UnSetFocusSlotUI(bool isDropFx)
        {
            _inGameBottomCharacterUI.UnSetFocusCharacterUI(isDropFx);
        }
        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            _inGameBottomCharacterUI.SetCommanderSkillUI(index, equippedCommanderSkillId);
        }
    }
}
