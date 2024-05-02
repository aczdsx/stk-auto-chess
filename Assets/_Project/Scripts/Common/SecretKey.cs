namespace CookApps.SampleTeamBattle
{
    public static class SecretKey
    {
        public static string GetKey()
        {
            // encrypted with https://www.stringencrypt.com (v1.4.0) [C#]
            // key = "tb$temp#gg"
            var key = "\u0F5B\u402A\u41D9\u2F58\u2FCF\u5F8E\u5FB5\u91D1\u8FF0\uBFEF";

            for (int dDBjV = 0, GCyDB = 0; dDBjV < 10; dDBjV++)
            {
                GCyDB = key[dDBjV];
                GCyDB = ~GCyDB;
                GCyDB -= dDBjV;
                GCyDB -= 0x15BA;
                GCyDB = ((GCyDB << 6) | ((GCyDB & 0xFFFF) >> 10)) & 0xFFFF;
                GCyDB ^= 0x9419;
                GCyDB++;
                GCyDB -= dDBjV;
                GCyDB ^= 0x22B6;
                GCyDB = ~GCyDB;
                GCyDB -= 0xF672;
                GCyDB -= dDBjV;
                GCyDB ^= 0x8066;
                GCyDB += 0x6C1F;
                GCyDB = (((GCyDB & 0xFFFF) >> 9) | (GCyDB << 7)) & 0xFFFF;
                GCyDB--;
                key = key.Substring(0, dDBjV) + (char) (GCyDB & 0xFFFF) + key.Substring(dDBjV + 1);
            }

            return key;
        }
    }
}
