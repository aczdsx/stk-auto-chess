/*
 * Copyright (c) CookApps.
 */

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

namespace CookApps.NetLite.Utils
{
    /// <summary>
    /// ULID 생성기
    /// </summary>
    public class Ulid
    {
        private const string Base32Chars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        // 추가 카운터를 16비트 사용 (최대 65535)
        private static int _additionalCounter;
        private static long _lastTimestamp;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetAdditionalCounter(long timestamp)
        {
#if UNITY_64 || UNITY_EDITOR_64 // 64bit의 경우 그냥 읽어와도 됨
            if (timestamp == _lastTimestamp)
#else
            if (timestamp == Interlocked.Read(ref _lastTimestamp))
#endif
            {
                Interlocked.Increment(ref _additionalCounter);
            }
            else
            {
                Interlocked.Exchange(ref _lastTimestamp, timestamp);
                Interlocked.Exchange(ref _additionalCounter, 0);
            }

            // 하위 16비트만 사용 (0 ~ 65535)
            return _additionalCounter & 0xFFFF;
        }

        /// <summary>
        /// 표준 ULID 생성
        /// </summary>
        /// <returns></returns>
        public static string NewUlid()
        {
            // 타임스탬프(10) + 랜덤(16) = 26 글자
            Span<char> ulidChars = stackalloc char[26];
            GeneratorDefaultUlid(ulidChars);
            return new string(ulidChars);
        }

        /// <summary>
        /// 첫글자가 1인 클라이언트에서 생성한 ULID 생성 (인벤토리 Id 생성 시 사용)
        /// </summary>
        /// <returns></returns>
        public static string NewClientInventoryId()
        {
            // 타임스탬프(10) + 랜덤(16) = 26 글자
            Span<char> ulidChars = stackalloc char[26];
            GeneratorDefaultUlid(ulidChars);
            // 강제로 첫글자 1 변경
            ulidChars[0] = '1';
            return new string(ulidChars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GeneratorDefaultUlid(Span<char> ulidChars)
        {
            // 타임스탬프(10) + 랜덤(16) = 26 글자
            if (ulidChars.Length != 26)
            {
                throw new ArgumentException("ulidChars 길이는 26이어야 합니다.");
            }

            // 48비트 타임스탬프를 밀리초 단위로 가져와 10글자로 인코딩
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EncodeTimestamp(timestamp, ulidChars);

            // 80비트 랜덤 데이터 생성 및 Base32 인코딩
            // 인덱스 10부터 16글자 할당 (총 16글자)
            Span<byte> randomBytes = stackalloc byte[10]; // 80비트 = 10 바이트
            RandomNumberGenerator.Fill(randomBytes);
            EncodeRandom(randomBytes, ulidChars[10..]);
        }

        /// <summary>
        /// 정렬 가능한 ULID 생성 (비표준)
        /// </summary>
        /// <remarks>
        /// - 같은 timestamp 내에서 중복 생성을 막기 위해 추가 카운터를 16비트(65535) 사용
        /// - 인코딩: 타임스탬프(10글자, Base32 5비트씩) + 추가 카운터(4글자, 각 4비트) + 랜덤(16글자, 80비트 랜덤)
        /// - 결과 ULID 문자열 길이: 10 + 4 + 16 = 30글자
        /// - 오름차순 정렬 가능
        /// </remarks>
        /// <returns>정렬 가능한 ULID 문자열 (30글자)</returns>
        public static string NewOrderedUlid()
        {
            // 타임스탬프(10) + 추가 카운터(4) + 랜덤(16) = 30 글자
            Span<char> ulidChars = stackalloc char[30];

            // 48비트 타임스탬프를 밀리초 단위로 가져와 10글자로 인코딩
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            EncodeTimestamp(timestamp, ulidChars);

            // 같은 timestamp 내에서의 중복 생성을 막기 위한 16비트 추가 카운터
            int additionalValue = GetAdditionalCounter(timestamp);

            // 16비트를 4글자(각 4비트)로 인코딩 (인덱스 10, 11, 12, 13)
            for (int i = 3; i >= 0; i--)
            {
                ulidChars[10 + i] = Base32Chars[additionalValue & 0x0F]; // 하위 4비트 추출 (0x0F)
                additionalValue >>= 4;
            }

            // 80비트 랜덤 데이터 생성 및 Base32 인코딩
            // 인덱스 14부터 16글자 할당 (총 16글자)
            Span<byte> randomBytes = stackalloc byte[10]; // 80비트 = 10 바이트
            RandomNumberGenerator.Fill(randomBytes);
            EncodeRandom(randomBytes, ulidChars[14..]);

            return new string(ulidChars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeTimestamp(long timestamp, Span<char> buffer)
        {
            // 48비트 타임스탬프를 10글자(Base32, 5비트씩)로 인코딩
            for (int i = 9; i >= 0; i--)
            {
                buffer[i] = Base32Chars[(int) (timestamp & 0x1F)]; // 하위 5비트 추출 (0x1F == 31)
                timestamp >>= 5;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeRandom(Span<byte> data, Span<char> buffer)
        {
            int bits = 0;
            int currentByte = 0;
            int bufferIndex = 0;

            for (int index = 0; index < data.Length; index++)
            {
                byte b = data[index];
                currentByte = (currentByte << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    bits -= 5;
                    buffer[bufferIndex++] = Base32Chars[(currentByte >> bits) & 0x1F];
                }
            }

            if (bits > 0)
            {
                buffer[bufferIndex] = Base32Chars[(currentByte << (5 - bits)) & 0x1F];
            }
        }
    }
}
