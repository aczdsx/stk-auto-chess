using System.Collections.Generic;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 범위 기본공격 패턴 레지스트리.
    /// 보스 등 특수 유닛의 멀티히트 범위 공격 패턴을 데이터로 관리.
    /// </summary>
    public static class AreaAttackRegistry
    {
        private static Dictionary<int, AreaAttackPattern> _patterns;

        public static void Initialize()
        {
            _patterns = new Dictionary<int, AreaAttackPattern>();

            // 280109001 보스: Execute1Per2 (2히트)
            // Hit 0: 가로 3×1 (전방 수직 방향 ±1칸) — Cross, Size=1
            // Hit 1: 세로 1×3 (전방 직선 3칸)       — Line,  Size=3
            _patterns[280109001] = new AreaAttackPattern
            {
                HitCount = 2,
                Hit0 = new AreaAttackHit { Shape = AreaAttackShape.Cross, Size = 1, FrontOffset = 1 },
                Hit1 = new AreaAttackHit { Shape = AreaAttackShape.Line,  Size = 3, FrontOffset = 0 },
            };
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
