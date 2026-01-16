using System.Linq;
using Cookapps.Stkauto.V1;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserQuest userQuest;

        public UserQuest UserQuest => userQuest;


        public void SaveUserQuestData()
        {
            QueueSave(DataCategory.UserQuest.ToCategoryString(), userQuest);
        }

        public UserQuestData GetUserQuestData(int questID)
        {
            var specQuestData = SpecDataManager.Instance.GetSpecQuestData(questID);

            if (specQuestData.term_type == TermType.DAILY)
            {
                if (userQuest.UserQuestDailyDatas.TryGetValue(questID, out var resultData)) return resultData;
            }
            else if (specQuestData.term_type == TermType.WEEKLY)
            {
                if (userQuest.UserQuestWeeklyDatas.TryGetValue(questID, out var resultData)) return resultData;
            }

            return null;
        }
    }
}