using System.Collections;
using System.Collections.Generic;
using CookApps.AnalyticsLite;
using CookApps.TeamBattle;
using UnityEngine;
using UnityEngine.PlayerLoop;

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

        /// <summary>
        /// 이벤트 전송
        /// </summary>
        /// <param name="eventName">이벤트 이름</param>
        /// <param name="appEventParameter">이벤트 파라메터</param>
        private void SendEvent(string eventName, AppEventParameter appEventParameter)
        {
            CAppEventLite.ReportCustom(eventName, appEventParameter);
        }

        /// <summary>
        /// 공통 파라메터를 생성
        /// </summary>
        /// <returns>공통 파라메터</returns>
        private AppEventParameter CreateCommonParam()
        {
            AppEventParameter appEventParameter = new AppEventParameter();
            appEventParameter.Add(AppEventStringConst.USER_ID, UserDataManager.Instance.UserBasicData.Uid);
            appEventParameter.Add(AppEventStringConst.PLAYER_ID, UserDataManager.Instance.UserBasicData.PlayerId);
            appEventParameter.Add(AppEventStringConst.SERVER, UserDataManager.Instance.UserBasicData.ServerId);
            appEventParameter.Add(AppEventStringConst.TOTAL_PLAY_TIME, UserDataManager.Instance.UserBasicData.TotalPlayTime);
            appEventParameter.Add(AppEventStringConst.DAILY_VISIT_COUNT, UserDataManager.Instance.UserBasicData.DailyVisitCount);
            appEventParameter.Add(AppEventStringConst.SINCE_JOIN_DATE, UserDataManager.Instance.UserBasicData.SinceJoinDate);
            appEventParameter.Add(AppEventStringConst.USER_INSTALL_DATE, TimeManager.Instance.TimeStampToDateTime(UserDataManager.Instance.UserBasicData.UserInstallDate));
            appEventParameter.Add(AppEventStringConst.BEST_STAGE, UserDataManager.Instance.GetLatestClearUserStageID());
            appEventParameter.Add(AppEventStringConst.BEST_MISSION, UserDataManager.Instance.GetCurrentGuideMissionData().MissionId);
            appEventParameter.Add(AppEventStringConst.USER_POWER, UserDataManager.Instance.GetAllCharacterBattlePower());
            appEventParameter.Add(AppEventStringConst.USER_LEVEL, UserDataManager.Instance.UserBasicData.Level);
            appEventParameter.Add(AppEventStringConst.USER_GRADE, UserDataManager.Instance.UserBasicData.MaxSquadCount);
            appEventParameter.Add(AppEventStringConst.USER_STAR_AMOUNT, UserDataManager.Instance.GetAllTotalChapterStarCount());
            appEventParameter.Add(AppEventStringConst.USER_ENERGY_AMOUNT, UserDataManager.Instance.UserWallet.Ap);

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

        /// <summary>
        /// 스테이지 클리어 이벤트
        /// </summary>
        /// <param name="stage_id">스테이지 아이디</param>
        /// <param name="chapter_id">스테이지 아이디</param>
        /// <param name="stage_number">스테이지 아이디</param>
        /// <param name="stage_type">스테이지 아이디</param>
        /// <param name="stage_star">스테이지 아이디</param>
        /// <param name="is_win">스테이지 아이디</param>
        // public void StageClear(int stage_id, int chapter_id, int stage_number, StageType stage_type, int stage_star, bool is_win)
        // {
        //     AppEventParameter appEventParameter = CreateCommonParam();
        //     appEventParameter.Add(AppEventStringConst.STAGE_ID, stage_id);
        //     appEventParameter.Add(AppEventStringConst.CHAPTER_ID, chapter_id);
        //     appEventParameter.Add(AppEventStringConst.STAGE_NUMBER, stage_number);
        //     appEventParameter.Add(AppEventStringConst.STAGE_TYPE, stage_type.ToString());
        //     appEventParameter.Add(AppEventStringConst.STAGE_STAR, stage_star);
        //     appEventParameter.Add(AppEventStringConst.IS_WIN, is_win);
        //
        //     SendEvent(AppEventStringConst.STAGE_CLEAR, appEventParameter);
        // }
        
        // 가이드 미션 통과 (가이드 미션 완료 시 전송)
        public void GuideMissionClear(int guide_id)
        {
            AppEventParameter appEventParameter = CreateCommonParam();
            appEventParameter.Add(AppEventStringConst.GUIDE_MISSION_ID, guide_id);
        
            SendEvent("GUIDE_MISSION_PASS", appEventParameter);
        }
    }
}
