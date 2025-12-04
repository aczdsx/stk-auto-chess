/*
* Copyright (c) CookApps.
*/

using System.Threading.Tasks;
using Tech.Hive.V1;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// 이부분은 PlatformAuth 패키지를 사용하여 교체 해 주세요.
    public static class AuthPlatformUtil
    {
        public static  Task<string> GetAuthId(AuthPlatform platform)
        {
            return Task.FromResult(SystemInfo.deviceUniqueIdentifier + "-" + platform);
        }
    }
}
