using System.Collections;
using System.Collections.Generic;
using CookApps.AnalyticsLite;
using CookApps.TeamBattle;
using UnityEngine;

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
            appEventParameter.Add(AppEventStringConst.USER_ID, "");
            appEventParameter.Add(AppEventStringConst.SERVER_UID, "");
            appEventParameter.Add(AppEventStringConst.SERVER, "");
            appEventParameter.Add(AppEventStringConst.COUNTRY, "");

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

            return defaultData;
        }
    }
}
