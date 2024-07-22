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
                    LastLoginTimestamp = TimeManager.Instance.UtcNowTimeStamp(),
                    MaxSquadCount = SpecDataManager.Instance.GetGameConfig<int>("default_max_squad_count"),

                    TotalGachaCount = 0,
                };
                PrevAccountLevel = userBasicData.Level;

                return;
            }

            userBasicData = MessageUtility.FromBase64String<UserBasicData>(data);

            PrevAccountLevel = userBasicData.Level;

            RefreshLastLoginTimestamp(true);
            UpdateResetCharacterCount();
        }

        [Clear]
        private void Clear_BasicData()
        {
            userBasicData = null;
        }

        public void SaveUserBasic()
        {
            HatcheryGrpcManager.Instance.SetPlayerDataAsync(DataCategory.UserData.ToCategoryString(), UserBasicData);
        }

        public void RefreshLastLoginTimestamp(bool needSave)
        {
            UserBasicData.LastLoginTimestamp = TimeManager.Instance.UtcNowTimeStamp();

            if (needSave)
            {
                SaveUserBasic();
            }
        }

        public void AddUserLevelExp(int exp)
        {
            if (UserBasicData.Level >= SpecDataManager.Instance.GetAccountMaxLevel())
            {
                return;
            }

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

        public void SetMaxSquadCount(int count, bool needSave)
        {
            UserBasicData.MaxSquadCount = count;

            if (needSave)
            {
                SaveUserBasic();
            }
        }

        public void SetResetCharacterCount(int count, bool isAdd, bool needSave)
        {
            if (isAdd)
            {
                UserBasicData.ResetCharacterCount += count;
            }
            else
            {
                UserBasicData.ResetCharacterCount = count;
            }

            if (needSave)
            {
                SaveUserBasic();
            }
        }

        // 캐릭터 초기화 카운트 및 시간 갱신
        public void UpdateResetCharacterCount()
        {
            // 현재 날짜가 리셋 날짜보다 크거나 같으면 초기화
            if (UserBasicData.ResetCharacterTimestamp <= TimeManager.Instance.UtcNowTimeStamp())
            {
                SetResetCharacterCount(0, false, false);

                UserBasicData.ResetCharacterTimestamp = TimeManager.Instance.TommorrowTimeStamp();

                SaveUserBasic();
            }
        }

        public void CheatResetUserLevelData()
        {
            UserBasicData.Level = 1;
            UserBasicData.Exp = 0;
        }
    }
}
