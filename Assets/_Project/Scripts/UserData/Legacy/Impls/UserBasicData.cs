using Cookapps.Stkauto.V1;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public partial class UserDataManager
    {
        private UserBasicData userBasicData;

        public UserBasicData UserBasicData => userBasicData;

        public int PrevAccountLevel { get; set; } = 1; // 유저 계정 레벨업 체크용 이전 레벨 데이터

        [Initialize(DataCategory.UserData)]
        private void Initialize_BasicData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                userBasicData = new UserBasicData
                {
                    Level = 1,
                    Exp = 0,
                    Nickname = "StellaKnights",
                    UserIconId = 40101,
                    LastLoginTimestamp = TimeManager.Instance.UtcNowTimeStampLocal(),
                    MaxSquadCount = SpecDataManager.Instance.GetGameConfig<int>("default_max_squad_count"),
                    UserInstallDate = TimeManager.Instance.UtcNowTimeStamp(),

                    DailyVisitCount = 1,
                    DailyVisitCountTimestamp = TimeManager.Instance.TommorrowTimeStampLocal(),
                    TotalGachaCount = 0,
                    UserStageLoseCount = 0,
                    UserDungeonLoseCount = 0
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
            QueueSave(DataCategory.UserData.ToCategoryString(), UserBasicData);
        }
        

        public void RefreshLastLoginTimestamp(bool needSave)
        {
            UserBasicData.LastLoginTimestamp = TimeManager.Instance.UtcNowTimeStampLocal();

            if (needSave) SaveUserBasic();
        }

        public void UpdateDailyVisitCount(bool needSave)
        {
            if (UserBasicData.DailyVisitCountTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
            {
                UserBasicData.DailyVisitCount++;
                UserBasicData.DailyVisitCountTimestamp = TimeManager.Instance.TommorrowTimeStampLocal();

                if (needSave) SaveUserBasic();
            }
        }

        // 유저 스테이지 패배 횟수 증가
        public void AddUserStageLoseCount(bool needSave)
        {
            UserBasicData.UserStageLoseCount++;

            if (needSave) SaveUserBasic();

            DialogueManager.Instance.UpdateDialogueEvent(DialogueEventType.FAIL, Instance.UserBasicData.UserStageLoseCount.ToString(),
                () =>
                {
                    if (UserBasicData.UserStageLoseCount == 1) HandleLoseAsync().Forget();
                });
        }

        private async UniTask HandleLoseAsync()
        {
            var lastPlayStageID = (int)LocalDataManager.Instance.GetLastPlayStageId();
            var specLastStageData = SpecDataManager.Instance.GetStageData(lastPlayStageID);
            SceneTransition.Create<SceneTransition_FadeInOut>();
            await SceneTransition.FadeInAsync();

            SceneLoading.GoToNextScene("Lobby", specLastStageData.chapter_id);

            SceneUILayerManager.OnSceneLoadedEvent += OpenCharacterCollectionPopupAction;
        }

        private void OpenCharacterCollectionPopupAction(string scenename)
        {
            if (scenename == "Lobby")
            {
                SceneUILayerManager.Instance.PushUILayerAsync<CharacterCollectionPopup>().Forget();

                ToastManager.Instance.ShowToastByTokenKey("MSG_GROWTH_CHARACTER");

                SceneUILayerManager.OnSceneLoadedEvent -= OpenCharacterCollectionPopupAction;
            }
        }

        public void SetResetCharacterCount(int count, bool isAdd, bool needSave)
        {
            if (isAdd)
                UserBasicData.ResetCharacterCount += count;
            else
                UserBasicData.ResetCharacterCount = count;

            if (needSave) SaveUserBasic();
        }

        // 캐릭터 초기화 카운트 및 시간 갱신
        public void UpdateResetCharacterCount()
        {
            // 현재 날짜가 리셋 날짜보다 크거나 같으면 초기화
            if (UserBasicData.ResetCharacterTimestamp <= TimeManager.Instance.UtcNowTimeStampLocal())
            {
                SetResetCharacterCount(0, false, false);

                UserBasicData.ResetCharacterTimestamp = TimeManager.Instance.TommorrowTimeStampLocal();

                SaveUserBasic();
            }
        }
    }
}