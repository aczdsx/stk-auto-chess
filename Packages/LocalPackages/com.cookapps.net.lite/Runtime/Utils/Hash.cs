/*
* Copyright (c) CookApps.
*/

namespace CookApps.NetLite.Utils
{
    public static class Hash
    {
        /// FNV-1a 64-bit variant hashing algorithm
        public static long FNV1aHash(in string data)
        {
            const ulong fnvOffsetBasis = 0xCBF29CE484222325;
            const ulong fnvPrime = 0x100000001B3;
            ulong hash = fnvOffsetBasis;

            foreach (char t in data)
            {
                unchecked
                {
                    hash ^= (byte) t; // XOR 연산
                    hash *= fnvPrime; // 곱셈 연산
                }
            }
            return unchecked((long)hash);
        }
    }
}
