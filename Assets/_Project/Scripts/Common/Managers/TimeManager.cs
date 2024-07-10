using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class TimeManager : Singleton<TimeManager>
    {
        public DateTime UtcNow()
        {
            return DateTime.UtcNow.ToLocalTime();
        }

        public long UtcNowTimeStamp()
        {
            return DateTimeToTimeStamp(UtcNow());
        }

        public DayOfWeek UtcDayOfWeek()
        {
            return UtcNow().DayOfWeek;
        }

        public DateTime Tommorrow()
        {
            DateTime now = UtcNow();
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
        }

        public long TommorrowTimeStamp()
        {
            return DateTimeToTimeStamp(Tommorrow());
        }

        public DateTime NextMonday()
        {
            DateTime now = UtcNow();
            for (int i = 1; i <= 7; i++)
            {
                DateTime cur = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(i);
                if (cur.DayOfWeek == DayOfWeek.Monday)
                {
                    return cur;
                }
            }

            return now;
        }

        public long NextMondayTimeStamp()
        {
            return DateTimeToTimeStamp(NextMonday());
        }

        public TimeSpan GetTimeSpanFromNow(long targetTimeStamp)
        {
            return UtcNow() - TimeStampToDateTime(targetTimeStamp);
        }

        public long GetLeftTimestampFromNow(long targetTimestamp)
        {
            return DateTimeToTimeStamp(UtcNow()) - targetTimestamp;
        }

        public DateTime GetLeftDateTimeFromNow(long targetTimestamp)
        {
            var leftTimestamp = GetLeftTimestampFromNow(targetTimestamp);
            return TimeStampToDateTime(leftTimestamp);
        }

        public long DateTimeToTimeStamp(DateTime value)
        {
            return ((DateTimeOffset)value).ToUnixTimeSeconds();
        }

        public DateTime TimeStampToDateTime(long value)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(value).ToLocalTime();
            return dt;
        }

        // 스펙 데이터의 datetime 데이터 문자열을 DateTime 형식으로 변환
        public DateTime ChangeDateStringToDateTime(string dateTimeString)
        {
            return DateTime.Parse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }

        // 스펙 데이터의 datetime 데이터 문자열을 long 형식으로 변환
        public long ChangeDateStringToTimeStamp(string dateTimeString)
        {
            var targetDateTime = DateTime.Parse(dateTimeString, null, System.Globalization.DateTimeStyles.RoundtripKind);

            return DateTimeToTimeStamp(targetDateTime);
        }
    }
}
