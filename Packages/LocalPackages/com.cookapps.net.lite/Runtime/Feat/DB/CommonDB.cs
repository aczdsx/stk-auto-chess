/*
* Copyright (c) CookApps.
*/

namespace CookApps.NetLite.Feat.DB
{
    public class CommonDB : InternalLiteDB
    {
        public CommonDB() : base("[CommonDB]", "common")
        {
        }
    }
}
