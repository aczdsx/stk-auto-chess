using System.Runtime.CompilerServices;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 보드 그리드 유틸리티. 좌표 변환, 거리 계산, 유효성 검사.
    /// 전투 그리드(7×8)와 플레이어 보드(7×4) 모두 지원.
    /// </summary>
    public static class BoardHelper
    {
        // 플레이어 보드 크기
        public const int Width = PlayerBoard.BoardWidth;   // 7
        public const int Height = PlayerBoard.BoardHeight; // 4

        // 전투 그리드 크기 (양쪽 4행씩)
        public const int CombatWidth = 7;
        public const int CombatHeight = 8;

        // ── 좌표 ↔ 인덱스 변환 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(int col, int row)
        {
            return col + row * Width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToIndex(int col, int row, int width)
        {
            return col + row * width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromIndex(int index, out int col, out int row)
        {
            col = index % Width;
            row = index / Width;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromIndex(int index, int width, out int col, out int row)
        {
            col = index % width;
            row = index / width;
        }

        // ── 유효성 검사 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidBoardPosition(int col, int row)
        {
            return col >= 0 && col < Width && row >= 0 && row < Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidCombatPosition(int col, int row)
        {
            return col >= 0 && col < CombatWidth && row >= 0 && row < CombatHeight;
        }

        // ── 거리 계산 ──

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ManhattanDistance(int c1, int r1, int c2, int r2)
        {
            int dc = c1 - c2;
            int dr = r1 - r2;
            if (dc < 0) dc = -dc;
            if (dr < 0) dr = -dr;
            return dc + dr;
        }

        /// <summary>체비셰프 거리 (대각선 포함, 사거리 2+ 판정용)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChebyshevDistance(int c1, int r1, int c2, int r2)
        {
            int dc = c1 - c2;
            int dr = r1 - r2;
            if (dc < 0) dc = -dc;
            if (dr < 0) dr = -dr;
            return dc > dr ? dc : dr;
        }

        /// <summary>사거리 내 판정 (Range 1 = Manhattan, Range 2+ = Chebyshev)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInRange(int c1, int r1, int c2, int r2, int range)
        {
            if (range <= 1)
                return ManhattanDistance(c1, r1, c2, r2) <= range;
            return ChebyshevDistance(c1, r1, c2, r2) <= range;
        }

        // ── 전투용 미러링 ──

        /// <summary>상대 보드 미러링: 플레이어A(row 0-3) ↔ 플레이어B(row 4-7)</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MirrorPosition(int col, int row, out int mirroredCol, out int mirroredRow)
        {
            mirroredCol = (CombatWidth - 1) - col;  // 6 - col
            mirroredRow = (CombatHeight - 1) - row;  // 7 - row
        }

        // ── Multi-Tile 유효성 검사 ──

        /// <summary>유닛 풋프린트가 플레이어 보드 범위 안인지</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidBoardFootprint(int col, int row, byte sizeW, byte sizeH)
        {
            return col >= 0 && col + sizeW <= Width && row >= 0 && row + sizeH <= Height;
        }

        /// <summary>유닛 풋프린트가 전투 그리드 범위 안인지</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidCombatFootprint(int col, int row, byte sizeW, byte sizeH)
        {
            return col >= 0 && col + sizeW <= CombatWidth && row >= 0 && row + sizeH <= CombatHeight;
        }

        // ── Multi-Tile 거리 계산 ──

        /// <summary>두 유닛(풋프린트) 간 최소 맨하탄 거리</summary>
        public static int MinManhattanDistance(
            int c1, int r1, byte w1, byte h1,
            int c2, int r2, byte w2, byte h2)
        {
            int dCol = MaxOf(c1 - (c2 + w2 - 1), c2 - (c1 + w1 - 1), 0);
            int dRow = MaxOf(r1 - (r2 + h2 - 1), r2 - (r1 + h1 - 1), 0);
            return dCol + dRow;
        }

        /// <summary>두 유닛(풋프린트) 간 최소 체비셰프 거리</summary>
        public static int MinChebyshevDistance(
            int c1, int r1, byte w1, byte h1,
            int c2, int r2, byte w2, byte h2)
        {
            int dCol = MaxOf(c1 - (c2 + w2 - 1), c2 - (c1 + w1 - 1), 0);
            int dRow = MaxOf(r1 - (r2 + h2 - 1), r2 - (r1 + h1 - 1), 0);
            return dCol > dRow ? dCol : dRow;
        }

        /// <summary>사거리 내 판정 (풋프린트 기반)</summary>
        public static bool IsInRangeMulti(
            int c1, int r1, byte w1, byte h1,
            int c2, int r2, byte w2, byte h2, int range)
        {
            if (range <= 1)
                return MinManhattanDistance(c1, r1, w1, h1, c2, r2, w2, h2) <= range;
            return MinChebyshevDistance(c1, r1, w1, h1, c2, r2, w2, h2) <= range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MaxOf(int a, int b, int c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }

        // ── 4방향 이동 ──
        // dx, dy 순서: 상, 하, 좌, 우
        public static readonly int[] DirCol4 = { 0, 0, -1, 1 };
        public static readonly int[] DirRow4 = { 1, -1, 0, 0 };

        // 8방향 이동 (4방향 + 대각선)
        public static readonly int[] DirCol8 = { 0, 0, -1, 1, -1, -1, 1, 1 };
        public static readonly int[] DirRow8 = { 1, -1, 0, 0, 1, -1, 1, -1 };
    }
}
