namespace CookApps.AutoChess
{
    /// <summary>
    /// 이동 시스템. BFS distance map 기반 경로탐색으로 타겟에 접근.
    /// 7×8 전투 그리드에서 작동. 4방향 기본, 8방향 옵션.
    /// </summary>
    public static class MovementSystem
    {
        // BFS distance map 재사용 버퍼 (최대 크기로 할당, GC 없음)
        private const int MaxGridSize = 256; // 충분한 최대 크기
        private static readonly int[] _distMap = new int[MaxGridSize];
        private static readonly int[] _bfsQueue = new int[MaxGridSize * 2]; // (col, row) 쌍

        /// <summary>
        /// 타겟 풋프린트 기준 역방향 BFS distance map 생성.
        /// 타겟 인접 빈 타일을 거리 1로 시작, 점유 타일은 통과 불가 (자기 자신 제외).
        /// </summary>
        private static void BuildDistanceMap(CombatMatchState state,
            int targetCol, int targetRow, byte targetSizeW, byte targetSizeH,
            int selfCombatId)
        {
            int gridSize = BoardHelper.CombatWidth * BoardHelper.CombatHeight;
            for (int i = 0; i < gridSize; i++)
                _distMap[i] = int.MaxValue;

            int gw = BoardHelper.CombatWidth;
            int head = 0, tail = 0;

            // 타겟 풋프린트 + 인접 1칸 범위를 순회
            for (int dc = -1; dc <= targetSizeW; dc++)
            {
                for (int dr = -1; dr <= targetSizeH; dr++)
                {
                    int c = targetCol + dc;
                    int r = targetRow + dr;
                    if (!BoardHelper.IsValidCombatPosition(c, r)) continue;

                    int idx = c + r * gw;
                    bool isInside = dc >= 0 && dc < targetSizeW && dr >= 0 && dr < targetSizeH;

                    if (isInside)
                    {
                        _distMap[idx] = 0;
                        continue;
                    }

                    int occupant = state.GridTiles[idx];
                    if (occupant != CombatUnit.InvalidId && occupant != selfCombatId) continue;

                    _distMap[idx] = 1;
                    _bfsQueue[tail++] = c;
                    _bfsQueue[tail++] = r;
                }
            }

            // BFS 확장 (4방향)
            while (head < tail)
            {
                int curCol = _bfsQueue[head++];
                int curRow = _bfsQueue[head++];
                int curDist = _distMap[curCol + curRow * gw];
                int nextDist = curDist + 1;

                for (int d = 0; d < 4; d++)
                {
                    int nc = curCol + BoardHelper.DirCol4[d];
                    int nr = curRow + BoardHelper.DirRow4[d];
                    if (!BoardHelper.IsValidCombatPosition(nc, nr)) continue;

                    int nIdx = nc + nr * gw;
                    if (_distMap[nIdx] <= nextDist) continue;

                    int occupant = state.GridTiles[nIdx];
                    if (occupant != CombatUnit.InvalidId && occupant != selfCombatId) continue;

                    _distMap[nIdx] = nextDist;
                    _bfsQueue[tail++] = nc;
                    _bfsQueue[tail++] = nr;
                }
            }
        }

        /// <summary>
        /// 풋프린트 내 타일 중 distance map 최솟값 반환.
        /// 1×1 유닛은 단일 조회, 다중 타일은 앵커 기준 전체 탐색.
        /// </summary>
        private static int GetFootprintDistance(int col, int row, byte sizeW, byte sizeH)
        {
            int gw = BoardHelper.CombatWidth;
            if (sizeW == 1 && sizeH == 1)
                return _distMap[col + row * gw];

            int min = int.MaxValue;
            for (int dc = 0; dc < sizeW; dc++)
                for (int dr = 0; dr < sizeH; dr++)
                {
                    int idx = (col + dc) + (row + dr) * gw;
                    if (_distMap[idx] < min) min = _distMap[idx];
                }
            return min;
        }

        /// <summary>
        /// 타겟을 향해 1칸 이동 시도. BFS distance map으로 최적 방향 결정.
        /// 이동 성공 시 true, 실패(막힘/불필요) 시 false.
        /// </summary>
        public static bool TryMoveToward(CombatMatchState state, ref CombatUnit unit, ref CombatUnit target,
            int tickRate, bool allowDiagonal = false)
        {
            byte sizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte sizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            byte tSizeW = target.SizeW > 0 ? target.SizeW : (byte)1;
            byte tSizeH = target.SizeH > 0 ? target.SizeH : (byte)1;

            // 이미 사거리 내면 이동 불필요
            if (BoardHelper.IsInRangeMulti(unit.GridCol, unit.GridRow, sizeW, sizeH,
                    target.GridCol, target.GridRow, tSizeW, tSizeH,
                    unit.AttackRange))
                return false;

            // 타겟이 가까이 접근 중이면 대기 (오버슈팅 방지)
            int currentDist = BoardHelper.MinManhattanDistance(
                unit.GridCol, unit.GridRow, sizeW, sizeH,
                target.GridCol, target.GridRow, tSizeW, tSizeH);

            if (currentDist <= unit.AttackRange + 1 && target.IsMoving)
            {
                int fromDist = BoardHelper.MinManhattanDistance(
                    unit.GridCol, unit.GridRow, sizeW, sizeH,
                    target.MoveFromCol, target.MoveFromRow, tSizeW, tSizeH);

                if (fromDist > currentDist)
                    return false; // 타겟이 접근 중 → 대기
            }

            // 타겟 기준 역방향 BFS distance map 생성
            BuildDistanceMap(state, target.GridCol, target.GridRow, tSizeW, tSizeH, unit.CombatId);

            int bestCol = -1;
            int bestRow = -1;
            int bestDist = int.MaxValue;

            int dirCount = allowDiagonal ? 8 : 4;
            var dirCols = allowDiagonal ? BoardHelper.DirCol8 : BoardHelper.DirCol4;
            var dirRows = allowDiagonal ? BoardHelper.DirRow8 : BoardHelper.DirRow4;

            for (int d = 0; d < dirCount; d++)
            {
                int nc = unit.GridCol + dirCols[d];
                int nr = unit.GridRow + dirRows[d];

                // 풋프린트 범위 검사
                if (!BoardHelper.IsValidCombatFootprint(nc, nr, sizeW, sizeH)) continue;

                // 풋프린트 영역이 비어있는지 검사
                if (!state.IsFootprintClear(nc, nr, sizeW, sizeH, unit.CombatId)) continue;

                // distance map 기반 거리 (풋프린트 내 최솟값)
                int dist = GetFootprintDistance(nc, nr, sizeW, sizeH);
                if (dist == int.MaxValue) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestCol = nc;
                    bestRow = nr;
                }
                else if (dist == bestDist)
                {
                    // 동률: 직선 방향 우선 (대각선보다)
                    bool curIsDiag = dirCols[d] != 0 && dirRows[d] != 0;
                    bool bestIsDiag = (bestCol != unit.GridCol) && (bestRow != unit.GridRow);
                    if (!curIsDiag && bestIsDiag)
                    {
                        bestCol = nc;
                        bestRow = nr;
                    }
                    else if (curIsDiag == bestIsDiag)
                    {
                        // row 방향(상하) 우선
                        bool curIsRowDir = dirCols[d] == 0;
                        bool bestIsRowDir = bestCol == unit.GridCol;
                        if (curIsRowDir && !bestIsRowDir)
                        {
                            bestCol = nc;
                            bestRow = nr;
                        }
                    }
                }
            }

            if (bestCol < 0) return false;

            // 출발 좌표 기록 (View 보간용)
            unit.MoveFromCol = unit.GridCol;
            unit.MoveFromRow = unit.GridRow;

            // 그리드 업데이트: 출발 풋프린트 해제, 도착 풋프린트 점유
            state.ClearGridMulti(unit.GridCol, unit.GridRow, sizeW, sizeH);
            unit.GridCol = (byte)bestCol;
            unit.GridRow = (byte)bestRow;
            state.SetGridMulti(bestCol, bestRow, sizeW, sizeH, unit.CombatId);

            // 이동 타이머 설정 (MoveSpeed 기반)
            int moveFrames = unit.GetMoveFrames(tickRate);
            unit.MoveDuration = moveFrames;
            unit.MoveTimer = moveFrames;

            if (CombatLogger.Enabled) CombatLogger.LogMove(unit.CombatId, unit.MoveFromCol, unit.MoveFromRow, bestCol, bestRow);

            state.EventQueue?.PushUnitMoved(unit.SourceEntityId, (byte)bestCol, (byte)bestRow);

            return true;
        }

        /// <summary>
        /// 암살자 백라인 점프. 전투 시작 시 1회 실행.
        /// 적 후열의 빈 타일로 텔레포트.
        /// </summary>
        public static bool TryBacklineJump(CombatMatchState state, ref CombatUnit unit, int tickRate)
        {
            if (!unit.HasBacklineJump || unit.BacklineJumpDone) return false;

            unit.BacklineJumpDone = true;

            byte sizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte sizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            // 후열에서 빈 타일 탐색 (적에게 가장 가까운 위치 선호)
            int bestCol = -1;
            int bestRow = -1;
            int bestEnemyDist = int.MaxValue;

            int gh = BoardHelper.CombatHeight;
            int gw = BoardHelper.CombatWidth;
            int halfHeight = gh / 2;

            for (int expandRow = 0; expandRow < gh; expandRow++)
            {
                // 후열부터 확장: 상대 진영 끝 → 내 진영 방향
                int checkRow = unit.GridRow < halfHeight
                    ? gh - 1 - expandRow
                    : expandRow;

                for (int col = 0; col <= gw - sizeW; col++)
                {
                    // 풋프린트 범위 체크
                    if (!BoardHelper.IsValidCombatFootprint(col, checkRow, sizeW, sizeH)) continue;
                    if (!state.IsFootprintClear(col, checkRow, sizeW, sizeH, unit.CombatId)) continue;

                    // 가장 가까운 적까지 풋프린트 거리 계산
                    int minEnemyDist = int.MaxValue;
                    for (int i = 0; i < state.UnitCount; i++)
                    {
                        ref var enemy = ref state.Units[i];
                        if (!enemy.IsValidTarget || enemy.TeamIndex == unit.TeamIndex) continue;
                        byte eSizeW = enemy.SizeW > 0 ? enemy.SizeW : (byte)1;
                        byte eSizeH = enemy.SizeH > 0 ? enemy.SizeH : (byte)1;
                        int dist = BoardHelper.MinManhattanDistance(col, checkRow, sizeW, sizeH,
                            enemy.GridCol, enemy.GridRow, eSizeW, eSizeH);
                        if (dist < minEnemyDist) minEnemyDist = dist;
                    }

                    if (minEnemyDist < bestEnemyDist)
                    {
                        bestEnemyDist = minEnemyDist;
                        bestCol = col;
                        bestRow = checkRow;
                    }
                }

                if (bestCol >= 0) break;
            }

            if (bestCol < 0) return false;

            // 텔레포트
            int jumpFromCol = unit.GridCol;
            int jumpFromRow = unit.GridRow;
            state.ClearGridMulti(unit.GridCol, unit.GridRow, sizeW, sizeH);
            unit.GridCol = (byte)bestCol;
            unit.GridRow = (byte)bestRow;
            state.SetGridMulti(bestCol, bestRow, sizeW, sizeH, unit.CombatId);

            // 이동 시간 적용 (이동 중 타겟팅 제외)
            unit.MoveFromCol = (byte)jumpFromCol;
            unit.MoveFromRow = (byte)jumpFromRow;
            int moveFrames = unit.GetMoveFrames(tickRate);
            unit.MoveDuration = moveFrames;
            unit.MoveTimer = moveFrames;
            unit.IsBacklineJumping = true;
            unit.State = CombatState.Moving;

            if (CombatLogger.Enabled) CombatLogger.LogBacklineJump(unit.CombatId, jumpFromCol, jumpFromRow, bestCol, bestRow);

            return true;
        }
    }
}
