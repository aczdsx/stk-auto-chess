using Cookapps.Autobattleproject.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserBasicData userBasicData;

        public UserBasicData UserBasicData => userBasicData;

        public int PrevAccountLevel { get; set; } = 1;      // 유저 계정 레벨업 체크용 이전 레벨 데이터

        [Initialize(DataCategory.UserData)]
        private void Initialize_BasicData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userBasicData = new UserBasicData
                {
                    Uid = 0,
                    Level = 1,
                    Exp = 0,
                    Nickname = "StellaKnights",
                    UserIconId = 40101,

                    TotalGachaCount = 0,
                };
                PrevAccountLevel = userBasicData.Level;

                return;
            }

            userBasicData = MessageUtility.FromBase64String<UserBasicData>(data);

            PrevAccountLevel = userBasicData.Level;
        }

        [Clear]
        private void Clear_BasicData()
        {
            userBasicData = null;
        }

        public void AddUserLevelExp(int exp)
        {
            UserBasicData.Exp += exp;

            int userLevel = SpecDataManager.Instance.GetAccountLevelByExp(userBasicData.Exp);

            UserBasicData.Level = userLevel;

            SaveUserBasic();
        }

        public void AddUserGachaCount(int count)
        {
            UserBasicData.TotalGachaCount += count;

            SaveUserBasic();
        }

        public void SaveUserBasic()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserData.ToCategoryString(), UserBasicData);
        }

        public void CheatResetUserLevelData()
        {
            UserBasicData.Level = 1;
            UserBasicData.Exp = 0;
        }
    }
}
