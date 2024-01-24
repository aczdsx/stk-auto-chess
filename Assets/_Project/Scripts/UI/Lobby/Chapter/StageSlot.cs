using Com.Cookapps.Sampleteambattle;
using CookApps.TeamBattle;

namespace CookApps.SampleTeamBattle
{
    public class StageSlot : CachedMonoBehaviour
    {
        public void SetStageData(int chapter, int stageIndex)
        {
            SpecStage specStage = SpecDataManager.Instance.GetSpecStage(chapter, stageIndex);
            UserStage userStage = UserDataManager.UserStage.GetUserStage(specStage.stage_id);
        }
    }
}
