using System.Collections.Generic;
using System.Text;
using CookApps.Analytics;
using CookApps.TeamBattle;

namespace CookApps.AutoBattler
{
    public class AppEventManager : Singleton<AppEventManager>
    {
        /// <summary>
        /// 이벤트 파라메터 데이터 클래스
        /// Dictionary 매번 선언하기 번거로워서 처리
        /// </summary>
        private class AppEventParameter : Dictionary<string, object>
        {
            public bool IsValid()
            {
                foreach (object value in this.Values)
                {
                    if (value == null)
                        return false;
                }

                return true;
            }
        }
        
        #region [AppEvent Common Function]
        
        // 특정 데이터를 앱이벤트용 문자열로 변환
        public string GetAppEventCustomDataList(params int[] dataList)
        {
            StringBuilder sbCharList = new StringBuilder();

            for (int i = 0; i < dataList.Length; ++i)
            {
                sbCharList.Append($"{dataList[i]};");
            }

            return sbCharList.ToString();
        }

        // 해당 덱의 캐릭터 리스트를 앱이벤트용 문자열로 변환
        public string GetAppEventTargetDeckList(InGameType targetType)
        {
            StringBuilder sbCharList = new StringBuilder();

            var deckData = ServerDataManager.Instance.Deck.GetDeck(targetType);
            if (deckData == null) return sbCharList.ToString();

            var placements = deckData.CharacterPlacements;
            for (int i = 0; i < placements.Count; ++i)
            {
                var characterData = ServerDataManager.Instance.Character.GetCharacter(placements[i].CharacterId);
                if (characterData != null)
                {
                    sbCharList.Append($"{characterData.CharacterId};");
                }
            }

            return sbCharList.ToString();
        }
        
        // 현재 장착 중인 지휘자 스킬 리스트를 앱이벤트용 문자열로 변환
        public string GetAppEventEquipCommandSkillList()
        {
            StringBuilder sbCharList = new StringBuilder();

            var targetSkillList = ServerDataManager.Instance.CommanderSkill.GetAllEquippedCommanderSkillIdList();

            for (int i = 0; i < targetSkillList.Count; ++i)
            {
                sbCharList.Append($"{targetSkillList[i]};");
            }

            return sbCharList.ToString();
        }
        
        // 최초 접속 가입 이후 경과일 계산
        public int GetSinceJoinDate()
        {
            var clientBasicData = ClientBasicData.Get();
            var joinTimeSpan = TimeManager.Instance.GetTimeSpanFromNow(clientBasicData.UserInstallDate);

            return joinTimeSpan.Days;
        }
        
        #endregion
        
        /// <summary>
        /// 이벤트 전송
        /// </summary>
        /// <param name="eventName">이벤트 이름</param>
        /// <param name="appEventParameter">이벤트 파라메터</param>
        private void SendEvent(string eventName, AppEventParameter appEventParameter)
        {
            CAppEvent.ReportCustom(eventName, appEventParameter);
        }

        /// <summary>
        /// 공통 파라메터를 생성
        /// </summary>
        /// <returns>공통 파라메터</returns>
        private AppEventParameter CreateCommonParam()
        {
            AppEventParameter appEventParameter = new AppEventParameter();
            appEventParameter.Add(AppEventStringConst.USER_ID, LocalDataManager.Instance.GetRecentAuthData()?.Id);
            // appEventParameter.Add(AppEventStringConst.PLAYER_ID, UserDataManager.Instance.UserBasicData.PlayerId);
            // appEventParameter.Add(AppEventStringConst.SERVER, UserDataManager.Instance.UserBasicData.ServerId);
            var targetLanguage = LanguageManager.Instance.CurrentLanguageType;
            appEventParameter.Add(AppEventStringConst.LANGUAGE, targetLanguage.ToString());

            var clientBasicData = ClientBasicData.Get();
            var playerData = ServerDataManager.Instance.PlayerData;
            var inventoryBridge = new InventoryDataBridge();

            appEventParameter.Add(AppEventStringConst.TOTAL_PLAY_TIME, clientBasicData.TotalPlayTime);
            appEventParameter.Add(AppEventStringConst.DAILY_VISIT_COUNT, clientBasicData.DailyVisitCount);
            appEventParameter.Add(AppEventStringConst.SINCE_JOIN_DATE, GetSinceJoinDate());
            var installDateTime = TimeManager.Instance.TimeStampToDateTimeLocal(clientBasicData.UserInstallDate);
            appEventParameter.Add(AppEventStringConst.USER_INSTALL_DATE, installDateTime.ToString("yyyyMMdd"));
            var specBestStageData = SpecDataManager.Instance.GetStageData((int)ServerDataManager.Instance.Battle.GetLatestClearedStageId());
            appEventParameter.Add(AppEventStringConst.BEST_STAGE, specBestStageData.id);
            appEventParameter.Add(AppEventStringConst.BEST_MISSION, (int)ServerDataManager.Instance.GuideMission.Order);
            // appEventParameter.Add(AppEventStringConst.USER_POWER, UserDataManager.Instance.GetAllCharacterBattlePower());
            appEventParameter.Add(AppEventStringConst.USER_LEVEL, playerData.Level);
            appEventParameter.Add(AppEventStringConst.USER_GRADE, clientBasicData.MaxSquadCount);
            appEventParameter.Add(AppEventStringConst.USER_STAR_AMOUNT, (int)ServerDataManager.Instance.Battle.TotalStarCount);
            appEventParameter.Add(AppEventStringConst.USER_ENERGY_AMOUNT, inventoryBridge.GetCurrency(IdMap.Item.ActionPoint));

            // yyyyMMddHHmm 에서 뒤에 4자리를 잘라서 yyyyMMdd 바꾸는 부분
            // string createdDate = DataManager.Instance.UserData.CreatedTime;
            // string dateTimeFormat = "yyyyMMdd";
            //
            // if (DateTime.TryParseExact(createdDate, dateTimeFormat, null, DateTimeStyles.None, out DateTime createDateTime) == false)
            // {
            //     createdDate = DataManager.Instance.UserData.CreatedTime.Substring(0, DataManager.Instance.UserData.CreatedTime.Length - 4);
            //     int createTime = Convert.ToInt32(createdDate);
            //     int year = createTime / 10000;
            //     createTime %= 10000;
            //     int month = createTime / 100;
            //     createTime %= 100;
            //     int day = createTime;
            //     createDateTime = new DateTime(year, month, day);
            // }

            AppEventParameter defaultData = new AppEventParameter();
            // defaultData.Add(AppEventStringConst.USER_ID, $"{NetworkManager.Instance.BaseUID}");
            // defaultData.Add(AppEventStringConst.SERVER_UID, $"{DataManager.Instance.UserData.Uid}");
            // defaultData.Add(AppEventStringConst.SERVER, $"{NetworkManager.Instance.RegionServer}");
            // defaultData.Add(AppEventStringConst.COUNTRY, $"{LanguageManager.Instance.Language}");

            return appEventParameter;
        }
        
        // 로그인 시 호출
        public void Login()
        {
            AppEventParameter appEventParameter = CreateCommonParam();
            appEventParameter.Add(AppEventStringConst.NICKNAME, ServerDataManager.Instance.PlayerData.Nickname);

            SendEvent("LOGIN", appEventParameter);
        }

        // 스테이지 종료 시 호출 (클리어 또는 패배 모두 적용)
        public void StageEnd(int id, int stageID, float playTime, int squadCount, int power, int enemy_power, string result,
            string reason, string clearCondition)
        {
            AppEventParameter appEventParameter = CreateCommonParam();
            appEventParameter.Add(AppEventStringConst.STAGE, id);
            appEventParameter.Add(AppEventStringConst.STAGE_ID, stageID);
            appEventParameter.Add(AppEventStringConst.PLAY_TIME, playTime);
            appEventParameter.Add(AppEventStringConst.SQUAD_COUNT, squadCount);
            appEventParameter.Add(AppEventStringConst.POWER, power);
            appEventParameter.Add(AppEventStringConst.ENEMY_POWER, enemy_power);
            appEventParameter.Add(AppEventStringConst.RESULT, result);
            appEventParameter.Add(AppEventStringConst.REASON, reason);
            appEventParameter.Add(AppEventStringConst.CLEAR_CONDITION, clearCondition);
            appEventParameter.Add(AppEventStringConst.COMMANDER_SKILL, GetAppEventEquipCommandSkillList());
            appEventParameter.Add(AppEventStringConst.DECK, GetAppEventTargetDeckList(InGameType.STAGE));
        
            SendEvent("STAGE_END", appEventParameter);
        }
        
        // 던전 종료 시 호출 (클리어 또는 패배 모두 적용)
        public void DungeonEnd(DungeonType dungeonType, int dungeonID, float playTime, int squadCount, int power, int enemy_power, string result,
            string reason, string clearCondition)
        {
            AppEventParameter appEventParameter = CreateCommonParam();
            appEventParameter.Add(AppEventStringConst.TYPE, dungeonType.ToString());
            appEventParameter.Add(AppEventStringConst.STAGE, dungeonID);
            appEventParameter.Add(AppEventStringConst.PLAY_TIME, playTime);
            appEventParameter.Add(AppEventStringConst.SQUAD_COUNT, squadCount);
            appEventParameter.Add(AppEventStringConst.POWER, power);
            appEventParameter.Add(AppEventStringConst.ENEMY_POWER, enemy_power);
            appEventParameter.Add(AppEventStringConst.RESULT, result);
            appEventParameter.Add(AppEventStringConst.REASON, reason);
            appEventParameter.Add(AppEventStringConst.CLEAR_CONDITION, clearCondition);
            appEventParameter.Add(AppEventStringConst.COMMANDER_SKILL, GetAppEventEquipCommandSkillList());
            appEventParameter.Add(AppEventStringConst.DECK, GetAppEventTargetDeckList(InGameType.TRIAL));
        
            SendEvent("DUNGEON_END", appEventParameter);
        }

        // // PVP 종료 시 호출 (승리 또는 패배 모두 적용)
        // public void PVPEnd(int season, bool isRevenge, PVPTierType tierType, int ranking, int rankPoint, float playTime, string result, int myPower
        //                     ,UserPVPBattleDetailData enemyData)
        // {
        //     string battleType = isRevenge ? "revenge" : "normal";
        //     // var enemyTierData = SpecDataManager.Instance.GetPVPTierData(enemyData.RankId);
        //     
        //     AppEventParameter appEventParameter = CreateCommonParam();
        //     appEventParameter.Add(AppEventStringConst.SEASON, season);
        //     appEventParameter.Add(AppEventStringConst.TYPE, battleType);
        //     appEventParameter.Add(AppEventStringConst.GRADE, tierType.ToString());
        //     appEventParameter.Add(AppEventStringConst.RANKING, ranking);
        //     appEventParameter.Add(AppEventStringConst.POINT, rankPoint);
        //     appEventParameter.Add(AppEventStringConst.PLAY_TIME, playTime);
        //     appEventParameter.Add(AppEventStringConst.RESULT, result);
        //     appEventParameter.Add(AppEventStringConst.DECK, GetAppEventTargetDeckList(InGameType.PVP));
        //     appEventParameter.Add(AppEventStringConst.POWER, myPower);
        //     
        //     appEventParameter.Add(AppEventStringConst.ENEMY_PLAYER_ID, enemyData.PlayerId);
        //     appEventParameter.Add(AppEventStringConst.ENEMY_POINT, enemyData.RankPoint);
        //     // appEventParameter.Add(AppEventStringConst.ENEMY_GRADE, enemyTierData?.pvp_tier_type.ToString());
        //     appEventParameter.Add(AppEventStringConst.ENEMY_DECK, GetAppEventTargetDeckList(enemyData.PvpDeckList.PvpCharacterDecks.ToList()));
        //     appEventParameter.Add(AppEventStringConst.ENEMY_DECK_POWER, enemyData.BattlePoint);
        //
        //     SendEvent("PVP_END", appEventParameter);
        // }

        // 가이드 미션 통과 (가이드 미션 완료 시 전송)
        public void GuideMissionClear(int guideID)
        {
            AppEventParameter appEventParameter = CreateCommonParam();
            appEventParameter.Add(AppEventStringConst.GUIDE_MISSION_ID, guideID);
        
            SendEvent("GUIDE_MISSION_PASS", appEventParameter);
        }
    }
}
