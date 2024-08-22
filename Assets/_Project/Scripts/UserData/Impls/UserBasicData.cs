using Cookapps.Stkauto.V1;
using CookApps.gRPC.Hatchery;
using CookApps.gRPC.Universal;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

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
                    ServerId = 1,
                    PlayerId = "",
                    Level = 1,
                    Exp = 0,
                    Nickname = "StellaKnights",
                    UserIconId = 40101,
                    LastLoginTimestamp = TimeManager.Instance.UtcNowTimeStamp(),
                    MaxSquadCount = SpecDataManager.Instance.GetGameConfig<int>("default_max_squad_count"),
                    UserInstallDate = TimeManager.Instance.UtcNowTimeStamp(),

                    DailyVisitCount = 1,
                    DailyVisitCountTimestamp = TimeManager.Instance.TommorrowTimeStamp(),
                    TotalGachaCount = 0,
                    UserStageLoseCount = 0,
                    UserDungeonLoseCount = 0,
                };
                PrevAccountLevel = userBasicData.Level;

                return;
            }

            userBasicData = MessageUtility.FromBase64String<UserBasicData>(data);

            PrevAccountLevel = userBasicData.Level;

            RefreshLastLoginTimestamp(true);
            
            UpdateDailyVisitCount(true);
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

        public void SetUserLoginData(int UID, int serverID, string playerID, string username)
        {
            UserBasicData.Uid = UID;
            UserBasicData.ServerId = serverID;
            UserBasicData.PlayerId = playerID;
            UserBasicData.Nickname = username;
            
            Preference.SavePreference(Pref.GUEST_ID, UserBasicData.PlayerId);   // 기기자체에도 저장 (첫 로그인 판별용)
            
            SaveUserBasic();
        }

        // 이전에 로그인 데이터를 가지고 있는지 체크
        public bool IsHaveLoginData()
        {
            return UserBasicData.Uid > 0 && UserBasicData.ServerId > 0 && !string.IsNullOrWhiteSpace(UserBasicData.PlayerId);
        }
        
        public void SetUserTotalPlayTime(int minute, bool needSave)
        {
            UserBasicData.TotalPlayTime += minute;

            if (needSave)
            {
                SaveUserBasic();
            }
        }
        
        public void RefreshLastLoginTimestamp(bool needSave)
        {
            UserBasicData.LastLoginTimestamp = TimeManager.Instance.UtcNowTimeStamp();

            if (needSave)
            {
                SaveUserBasic();
            }
        }
        
        public void UpdateDailyVisitCount(bool needSave)
        {
            if (UserBasicData.DailyVisitCountTimestamp <= TimeManager.Instance.UtcNowTimeStamp())
            {
                UserBasicData.DailyVisitCount++;
                UserBasicData.DailyVisitCountTimestamp = TimeManager.Instance.TommorrowTimeStamp();
                
                if (needSave)
                {
                    SaveUserBasic();
                }
            }
        }

        // 유저 스테이지 패배 횟수 증가
        public void AddUserStageLoseCount(bool needSave)
        {
            UserBasicData.UserStageLoseCount++;
            
            if (needSave)
            {
                SaveUserBasic();
            }
            
            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.FAIL, Instance.UserBasicData.UserStageLoseCount.ToString(),
                () =>
                {
                    if (UserBasicData.UserStageLoseCount == 1)
                    {
                        HandleLoseAsync().Forget();
                    }
                });
        }
        
        private async UniTask HandleLoseAsync()
        {
            int lastPlayStageID = GetLastPlayStageID();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            var transition = SceneTransition_FadeInOut.Create();

            await SceneLoading.GoToNextScene("Lobby", (int) specLastStageData.chapter_id, transition);

            SceneUILayerManager.OnSceneLoadedEvent += OpenCharacterCollectionPopupAction;
        }
        
        private void OpenCharacterCollectionPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();
            
                SceneUILayerManager.OnSceneLoadedEvent -= OpenCharacterCollectionPopupAction;
            }
        }
        
        // 유저 던전 패배 횟수 증가
        public void AddUserDungeonLoseCount(bool needSave)
        {
            UserBasicData.UserDungeonLoseCount++;
            
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
