/*
* Copyright (c) CookApps.
*/

using CookApps.NetLite.Utils;

namespace Tech.Hive.V1
{
    partial class PlayerDataBase
    {
        public string GetData() => GZipUtil.DecompressToUtf8String(Data.Memory);
    }
}
