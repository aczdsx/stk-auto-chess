using System.Collections.Generic;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 범위 기본공격 패턴 레지스트리.
    /// 보스 등 특수 유닛의 멀티히트 범위 공격 패턴을 데이터로 관리.
    /// 형상(Shape/Size)은 수동 등록, 히트 타이밍(DelayMs)은 AnimKeyframeData에서 자동 추출.
    /// </summary>
    public static class AreaAttackRegistry
    {
        private static Dictionary<int, AreaAttackPattern> _patterns;

        public static void Initialize()
        {
            _patterns = new Dictionary<int, AreaAttackPattern>();

            // 280109001 공허의 토마: Execute1Per2 (2히트)
            // Hit 0: 가로 3×1 (전방 수직 방향 ±1칸) — Cross, Size=1
            // Hit 1: 세로 1×3 (전방 직선 3칸)       — Line,  Size=3
            // 타이밍: AnimKeyframeData characterId=80109001, Back_ATK
            RegisterWithAutoTiming(280109001, 80109001, new AreaAttackPattern
            {
                HitCount = 2,
                Hit0 = new AreaAttackHit { Shape = AreaAttackShape.Cross, Size = 1, FrontOffset = 1 },
                Hit1 = new AreaAttackHit { Shape = AreaAttackShape.Line,  Size = 3, FrontOffset = 0 },
            });
        }

        /// <summary>
        /// 패턴 등록 + AnimKeyframeData의 ATK 히트 타이밍 자동 적용.
        /// characterId: AnimKeyframeData에서 사용하는 컨트롤러 ID.
        /// </summary>
        private static void RegisterWithAutoTiming(int champSpecId, int characterId, AreaAttackPattern pattern)
        {
            int atkKey = AnimKeyframeData.MakeKey(characterId, false, AnimClipType.ATK);

            if (AnimKeyframeData.AttackHitTimes.TryGetValue(atkKey, out float[] hitTimes))
            {
                // AttackHitTimes에서 DelayMs 자동 설정
                int hitCount = pattern.HitCount < hitTimes.Length ? pattern.HitCount : hitTimes.Length;
                for (int i = 0; i < hitCount; i++)
                {
                    var hit = pattern.GetHit(i);
                    hit.DelayMs = (int)(hitTimes[i] * 1000f + 0.5f);
                    SetHit(ref pattern, i, hit);
                }
            }

            _patterns[champSpecId] = pattern;
        }

        private static void SetHit(ref AreaAttackPattern p, int i, AreaAttackHit hit)
        {
            switch (i)
            {
                case 0: p.Hit0 = hit; break;
                case 1: p.Hit1 = hit; break;
                case 2: p.Hit2 = hit; break;
                case 3: p.Hit3 = hit; break;
            }
        }

        public static bool TryGetPattern(int champSpecId, out AreaAttackPattern pattern)
        {
            if (_patterns != null && _patterns.TryGetValue(champSpecId, out pattern))
                return true;
            pattern = default;
            return false;
        }
    }
}
