using System.Collections.Generic;
using Cookapps.Autobattleproject.V1;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

namespace CookApps.AutoBattler
{
    public class InGameMainStateTrialDungeonUI : IGameStateUI
    {
        private SpecDungeonTrial _specDungeonTrial;

        public void SetInGameBottomUI()
        {
        }

        public void RefreshInGameTopUI()
        {
        }

        public UniTask Initialize(Transform canvasTransform, int id)
        {
            _specDungeonTrial = SpecDataManager.Instance.GetSpecDungeonTrialData(id);
            InGameManager.Instance.StartInGame<FlowStateTrialDungeonReady>(_specDungeonTrial, _specDungeonTrial);

            return UniTask.CompletedTask;
        }

        public void ReturnCharacter(CharacterController characterController)
        {
            throw new System.NotImplementedException();
        }

        public void AddCharacter(List<UserCharacterBattleDeck> battleDeckList)
        {
            throw new System.NotImplementedException();
        }

        public void SetInGameBottomUIInGuide()
        {
            throw new System.NotImplementedException();
        }

        public void ManagedUpdate(float dt)
        {
            throw new System.NotImplementedException();
        }

        public void SetReadyUI()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateCommanderSkillCoolTime()
        {
            throw new System.NotImplementedException();
        }

        public void SetFocusSlot(SpecCharacter spec)
        {
            throw new System.NotImplementedException();
        }

        public void UnSetFocusSlot(bool isDropFx)
        {
            throw new System.NotImplementedException();
        }

        public void SetCombatUI()
        {
            throw new System.NotImplementedException();
        }

        public void SetCommanderSkillUI(int index, int equippedCommanderSkillId)
        {
            throw new System.NotImplementedException();
        }

        public int GetVignetteID()
        {
            throw new System.NotImplementedException();
        }

        public void PlayBGM()
        {
        }

        public string GetStageName()
        {
            throw new System.NotImplementedException();
        }
    }
}
