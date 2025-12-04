/*
* Copyright (c) CookApps.
*/

namespace CookApps.NetLite.Editor
{
    internal static class Defines
    {
        /// 패키지가 포함하는 기본 서비스
        public static readonly string[] IncludeDefaultServices = {
          "auth",
          "lobby",
          "spec",
          "player",
          "player_data",
          "inventory_flow",
          "shop",
        };

        /// 패키지가 제외하는 기본 서비스
        public static readonly string[] ExcludeDefaultServices = {
          "health",
        };
    }
}
