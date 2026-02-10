using System.Runtime.CompilerServices;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 결정론적 난수 생성기 (xorshift64).
    /// 동일 시드 → 동일 시퀀스 보장. 멀티플레이 동기화에 필수.
    /// </summary>
    public struct DeterministicRNG
    {
        private ulong _state;

        public DeterministicRNG(ulong seed)
        {
            // seed가 0이면 xorshift가 영원히 0을 반환하므로 방지
            _state = seed != 0 ? seed : 1;
        }

        /// <summary>raw 64-bit 난수</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextULong()
        {
            _state ^= _state << 13;
            _state ^= _state >> 7;
            _state ^= _state << 17;
            return _state;
        }

        /// <summary>raw 32-bit 난수</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt()
        {
            return (uint)(NextULong() >> 32);
        }

        /// <summary>[minInclusive, maxExclusive) 범위 정수</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) return minInclusive;

            uint range = (uint)(maxExclusive - minInclusive);
            // 모듈러 바이어스 제거 (rejection sampling)
            uint threshold = (uint)(-range) % range;
            uint value;
            do
            {
                value = NextUInt();
            } while (value < threshold);

            return minInclusive + (int)(value % range);
        }

        /// <summary>0~100 퍼센트 확률 체크</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Chance(int percent)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;
            return Range(0, 100) < percent;
        }

        /// <summary>Fisher-Yates 셔플 (배열의 앞 count개만)</summary>
        public void Shuffle<T>(T[] array, int count)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>현재 상태 (스냅샷/복원용)</summary>
        public ulong State
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state;
        }
    }
}
