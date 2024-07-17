using CookApps.BattleSystem;

namespace CookApps.AutoBattler
{
    public class InGameMainStateTrialDungeonUI : IGameStateUI
    {
        private SpecDungeonTrial _specDungeonTrial;

        public void SetInGameBottomUI()
        {
        }

        public void SetInGameTopUI()
        {
        }

        public void Initialize(int id)
        {
            _specDungeonTrial = SpecDataManager.Instance.GetSpecDungeonTrialData(id);
            InGameManager.Instance.StartInGame<FlowStateTrialDungeonReady>(_specDungeonTrial, _specDungeonTrial);
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
