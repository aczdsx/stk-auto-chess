namespace CookApps.NetLite.Feat.DB
{
    public class PlatformDB : InternalLiteDB
    {
        //───────────────────────────────────────────────────────────────────────────────────────
        public PlatformDB() : base("[PlatformDB]", "platform")
        {
        }
    }
}
