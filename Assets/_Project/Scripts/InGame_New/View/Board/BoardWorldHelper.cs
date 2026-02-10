using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 그리드 ↔ 월드 좌표 변환 유틸리티.
    /// 4개 보드가 2×2 배치. 각 보드는 7×8(전투) 또는 7×4(준비) 그리드.
    /// </summary>
    public static class BoardWorldHelper
    {
        public const float TileSize = 1.2f;
        public const float BoardSpacing = 20f;
        public const float BenchOffsetZ = -1.5f;  // 벤치는 보드 아래

        /// <summary>보드 인덱스(0-3)의 월드 원점</summary>
        public static Vector3 GetBoardOrigin(int boardIndex)
        {
            int col = boardIndex % 2;
            int row = boardIndex / 2;
            return new Vector3(col * BoardSpacing, 0f, -row * BoardSpacing);
        }

        /// <summary>보드 그리드 → 월드 좌표 (Preparation: 7×4)</summary>
        public static Vector3 BoardGridToWorld(int boardIndex, int col, int row)
        {
            Vector3 origin = GetBoardOrigin(boardIndex);
            return origin + new Vector3(col * TileSize, 0f, row * TileSize);
        }

        /// <summary>전투 그리드 → 월드 좌표 (Combat: 7×8)</summary>
        public static Vector3 CombatGridToWorld(int boardIndex, int col, int row)
        {
            return BoardGridToWorld(boardIndex, col, row);
        }

        /// <summary>벤치 슬롯 → 월드 좌표</summary>
        public static Vector3 BenchToWorld(int boardIndex, int benchSlot)
        {
            Vector3 origin = GetBoardOrigin(boardIndex);
            return origin + new Vector3(benchSlot * TileSize, 0f, BenchOffsetZ);
        }

        /// <summary>월드 좌표 → 보드 인덱스 + 그리드 좌표</summary>
        public static bool WorldToBoard(Vector3 worldPos, out int boardIndex, out int col, out int row)
        {
            int bCol = Mathf.RoundToInt(worldPos.x / BoardSpacing);
            int bRow = Mathf.RoundToInt(-worldPos.z / BoardSpacing);
            bCol = Mathf.Clamp(bCol, 0, 1);
            bRow = Mathf.Clamp(bRow, 0, 1);
            boardIndex = bCol + bRow * 2;

            Vector3 origin = GetBoardOrigin(boardIndex);
            Vector3 local = worldPos - origin;
            col = Mathf.RoundToInt(local.x / TileSize);
            row = Mathf.RoundToInt(local.z / TileSize);

            return col >= 0 && col < BoardHelper.Width && row >= 0 && row < BoardHelper.Height;
        }

        /// <summary>월드 좌표가 벤치 영역인지 판별</summary>
        public static bool WorldToBench(Vector3 worldPos, int boardIndex, out int benchSlot)
        {
            Vector3 origin = GetBoardOrigin(boardIndex);
            Vector3 local = worldPos - origin;

            benchSlot = Mathf.RoundToInt(local.x / TileSize);
            bool isBenchArea = local.z < 0f && local.z > BenchOffsetZ - TileSize * 0.5f;

            return isBenchArea && benchSlot >= 0 && benchSlot < PlayerBoard.BenchSize;
        }
    }
}
