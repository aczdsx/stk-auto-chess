using System;
using CookApps.AutoBattler;

namespace Tech.Hive.V1
{
    public partial class ElpisFacility
    {
        public bool IsBuilding
        {
            get
            {
                if (builtAt_ == 0)
                    return false;

                return TimeManager.Instance.UtcNow() < BuildCompleteTime;
            }
        }

        public bool IsJustCompleted
        {
            get
            {
                if (builtAt_ == 0)
                    return false;

                return TimeManager.Instance.UtcNow() >= BuildCompleteTime;
            }
        }

        public DateTime BuildCompleteTime
        {
            get
            {
                if (builtAt_ == 0)
                    return DateTime.MinValue;

                var buildInfo = SpecDataManager.Instance.GetElpisBuildInfoData((int)buildId_, (int)level_);
                var startTime = TimeManager.Instance.TimeStampToDateTime((long)builtAt_ / 1000);
                var completeTime = startTime.AddSeconds(buildInfo.build_time);
                return completeTime;
            }
        }
    }
}