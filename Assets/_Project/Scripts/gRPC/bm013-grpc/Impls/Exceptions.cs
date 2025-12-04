/*
* Copyright (c) CookApps.
*/

using System;
using Tech.Hive.V1;

namespace CookApps.AutoBattler
{
    /// 강제 업데이트가 필요한경우
    public class UpdateStatusForceException : Exception
    {
        public UpdateStatusForceException()
            : base("강제 업데이트가 필요합니다.")
        {
        }
    }

    /// 서버 점검중인 경우
    public class UnderMaintenanceException : Exception
    {
        public UnderMaintenanceException()
            : base($"서버 점검중입니다.")
        {
        }
    }

    /// Spec 다운로드에 실패한 경우
    public class SpecDownloadFailedException : Exception
    {
        public SpecDownloadFailedException(SpecType type, uint version)
            : base($"Spec 다운로드에 실패했습니다. (type: {type}, version: {version})")
        {
        }
    }
}
