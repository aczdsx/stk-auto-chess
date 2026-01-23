using System;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 방치 보상 관련 공통 유틸리티
    /// </summary>
    public static class IdleRewardHelper
    {
        /// <summary>
        /// 마지막 보상 수령 후 경과 시간 반환
        /// </summary>
        public static TimeSpan GetElapsedTime()
        {
            var seconds = (long)ServerDataManager.Instance.Elpis.Simulation.LastClaimTime / 1000;
            return TimeManager.Instance.GetTimeSpanFromNow(seconds);
        }

        /// <summary>
        /// 최대 누적 시간 제한 (분 단위)
        /// </summary>
        public static int GetMaxTimeLimitMinutes()
        {
            return SpecDataManager.Instance.GetGameConfig<int>("idle_reward_acc_time_limit");
        }

        /// <summary>
        /// 현재 진행률 (0.0 ~ 1.0)
        /// </summary>
        public static float GetProgressRatio()
        {
            var elapsed = GetElapsedTime();
            int maxMinutes = GetMaxTimeLimitMinutes();
            return Math.Min((float)elapsed.TotalMinutes / maxMinutes, 1f);
        }

        /// <summary>
        /// 현재 진행률 (0 ~ 100 정수)
        /// </summary>
        public static int GetProgressPercent()
        {
            return (int)Math.Ceiling(GetProgressRatio() * 100);
        }

        /// <summary>
        /// 최대 시간에 도달했는지 여부
        /// </summary>
        public static bool IsFull()
        {
            return GetElapsedTime().TotalMinutes >= GetMaxTimeLimitMinutes();
        }

        /// <summary>
        /// 경과 시간을 HH:MM:SS 형식으로 포맷
        /// </summary>
        public static string FormatElapsedTime()
        {
            var elapsed = GetElapsedTime();
            int maxMinutes = GetMaxTimeLimitMinutes();

            // 최대 시간 초과 시 최대값으로 표시
            if (elapsed.TotalMinutes >= maxMinutes)
            {
                int maxHours = maxMinutes / 60;
                return $"{maxHours:D2}:00:00";
            }

            return $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        /// <summary>
        /// 슬라이더 텍스트 형식 (현재/최대)
        /// </summary>
        public static string FormatSliderText()
        {
            var elapsed = GetElapsedTime();
            int maxMinutes = GetMaxTimeLimitMinutes();
            int maxHours = maxMinutes / 60;

            string current = FormatElapsedTime();
            return $"{current} / {maxHours:D2}:00:00";
        }
    }
}
