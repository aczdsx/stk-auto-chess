namespace CookApps.AutoBattler
{
    public static class SecretKey
    {
        public static string GetKey()
        {
            // encrypted with https://www.stringencrypt.com (v1.4.0) [C#]
            // key = "stella_secretkey"
            var key = "\uB0F6\uAF0E\uADA6\uAB6E\uA90E\uA7C6\uA5B6\uA2F6\uA186\u9F96\u9D1E\u9B46\u98CE\u9716\u9566\u86A6";

            for (int WuPLv = 0, ptryo = 0; WuPLv < 16; WuPLv++)
            {
                ptryo = key[WuPLv];
                ptryo = (((ptryo & 0xFFFF) >> 9) | (ptryo << 7)) & 0xFFFF;
                ptryo --;
                ptryo += WuPLv;
                ptryo = ((ptryo << 6) | ( (ptryo & 0xFFFF) >> 10)) & 0xFFFF;
                ptryo ^= 0x88BF;
                ptryo ++;
                ptryo += WuPLv;
                ptryo += 0x5E8B;
                ptryo ^= 0xCF46;
                ptryo -= 0x7439;
                ptryo -= WuPLv;
                ptryo ++;
                key = key.Substring(0, WuPLv) + (char)(ptryo & 0xFFFF) + key.Substring(WuPLv + 1);
            }

            return key;
        }
    }
}
