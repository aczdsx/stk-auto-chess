using UnityEngine;

namespace CookApps.AutoChess.View
{
    /// <summary>
    /// 보드 그리드 ↔ 월드 좌표 변환 유틸리티.
    /// 스테이지 프리팹에 배치된 타일의 실제 월드 좌표를 기반으로 동작.
    /// Initialize()로 타일 위치를 등록한 뒤 사용.
    /// </summary>
    public static class BoardWorldHelper
    {
        // Initialize에서 세팅되는 값
        private static int _gridWidth;
        private static int _combatHeight;
        private static int _tilesPerBoard;

        // 타일 월드 좌표 캐시: [boardIndex * _tilesPerBoard + row * _gridWidth + col]
        private static Vector3[] _tilePositions;
        private static int _boardCount;
        private static bool _initialized;

        // ── 초기화 ──

        /// <summary>
        /// 스테이지 프리팹의 실제 타일 위치를 등록.
        /// tiles 순서: board0 row0 col0..W-1, row1 col0..W-1, ... boardN rowH-1 colW-1
        /// </summary>
        public static void Initialize(Transform[] tiles, int boardCount)
        {
            _gridWidth = BoardHelper.CombatWidth;
            _combatHeight = BoardHelper.CombatHeight;
            _tilesPerBoard = _gridWidth * _combatHeight;

            _boardCount = boardCount;
            int totalTiles = _tilesPerBoard * boardCount;
            _tilePositions = new Vector3[totalTiles];

            int count = Mathf.Min(tiles.Length, totalTiles);
            for (int i = 0; i < count; i++)
            {
                _tilePositions[i] = tiles[i] != null ? tiles[i].position : Vector3.zero;
            }

            _initialized = true;
        }

        // ── 좌표 변환 ──

        /// <summary>보드 그리드 → 월드 좌표 (Preparation/Combat 공용)</summary>
        public static Vector3 BoardGridToWorld(int boardIndex, int col, int row)
        {
            if (!_initialized)
            {
                Debug.LogError("[BoardWorldHelper] Not initialized. Call Initialize() first.");
                return Vector3.zero;
            }

            int index = boardIndex * _tilesPerBoard + row * _gridWidth + col;
            if (index < 0 || index >= _tilePositions.Length)
                return Vector3.zero;

            return _tilePositions[index];
        }

        /// <summary>전투 그리드 → 월드 좌표</summary>
        public static Vector3 CombatGridToWorld(int boardIndex, int col, int row)
        {
            return BoardGridToWorld(boardIndex, col, row);
        }

        /// <summary>벤치 슬롯 → 월드 좌표 (TODO: 벤치 타일도 프리팹에서 등록 시 구현)</summary>
        public static Vector3 BenchToWorld(int boardIndex, int benchSlot)
        {
            Vector3 row0Pos = BoardGridToWorld(boardIndex, benchSlot, 0);
            Vector3 offset = GetTileSpacing(boardIndex);
            return row0Pos - new Vector3(0f, 0f, offset.z > 0 ? offset.z : 1.2f);
        }

        /// <summary>Multi-tile 유닛의 풋프린트 중심 오프셋 (앵커 기준)</summary>
        public static Vector3 GetFootprintCenterOffset(byte sizeW, byte sizeH)
        {
            if (!_initialized || (sizeW <= 1 && sizeH <= 1))
                return Vector3.zero;

            Vector3 spacing = GetTileSpacing(0);
            return new Vector3(
                (sizeW - 1) * 0.5f * spacing.x,
                0f,
                (sizeH - 1) * 0.5f * spacing.z);
        }

        /// <summary>월드 좌표 → 보드 인덱스 + 그리드 좌표 (가장 가까운 타일)</summary>
        public static bool WorldToBoard(Vector3 worldPos, out int boardIndex, out int col, out int row)
        {
            boardIndex = 0;
            col = 0;
            row = 0;

            if (!_initialized) return false;

            int prepHeight = BoardHelper.Height;
            float bestDistSq = float.MaxValue;
            int bestBoard = 0;
            int bestCol = 0;
            int bestRow = 0;

            for (int b = 0; b < _boardCount; b++)
            {
                for (int r = 0; r < prepHeight; r++)
                {
                    for (int c = 0; c < _gridWidth; c++)
                    {
                        int index = b * _tilesPerBoard + r * _gridWidth + c;
                        float distSq = (worldPos - _tilePositions[index]).sqrMagnitude;
                        if (distSq < bestDistSq)
                        {
                            bestDistSq = distSq;
                            bestBoard = b;
                            bestCol = c;
                            bestRow = r;
                        }
                    }
                }
            }

            boardIndex = bestBoard;
            col = bestCol;
            row = bestRow;
            return true;
        }

        // ── 내부 헬퍼 ──

        /// <summary>인접 타일 간 간격 벡터 (col 방향, row 방향)</summary>
        /// <summary>그리드 방향(sbyte)을 월드 방향 벡터로 변환 (크기 = 타일 1칸 간격)</summary>
        public static Vector3 GridDirToWorld(sbyte dirCol, sbyte dirRow)
        {
            if (!_initialized) return new Vector3(dirCol, 0f, dirRow);
            Vector3 spacing = GetTileSpacing(0);
            return new Vector3(dirCol * spacing.x, 0f, dirRow * spacing.z);
        }

        private static Vector3 GetTileSpacing(int boardIndex)
        {
            int baseIdx = boardIndex * _tilesPerBoard;

            // col 방향 간격: (0,0) → (1,0)
            Vector3 colSpacing = Vector3.zero;
            if (baseIdx + 1 < _tilePositions.Length)
                colSpacing = _tilePositions[baseIdx + 1] - _tilePositions[baseIdx];

            // row 방향 간격: (0,0) → (0,1)
            Vector3 rowSpacing = Vector3.zero;
            if (baseIdx + _gridWidth < _tilePositions.Length)
                rowSpacing = _tilePositions[baseIdx + _gridWidth] - _tilePositions[baseIdx];

            return new Vector3(
                Mathf.Abs(colSpacing.x) > 0.01f ? Mathf.Abs(colSpacing.x) : Mathf.Abs(colSpacing.z),
                0f,
                Mathf.Abs(rowSpacing.z) > 0.01f ? Mathf.Abs(rowSpacing.z) : Mathf.Abs(rowSpacing.x));
        }
    }
}
