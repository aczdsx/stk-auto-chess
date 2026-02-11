namespace CookApps.AutoChess
{
    /// <summary>
    /// 이동 시스템. 그리디 1칸 이동으로 타겟에 접근.
    /// 7×8 전투 그리드에서 작동. 4방향 기본, 8방향 옵션.
    /// </summary>
    public static class MovementSystem
    {
        /// <summary>
        /// 타겟을 향해 1칸 이동 시도.
        /// 이동 성공 시 true, 실패(막힘/불필요) 시 false.
        /// </summary>
        public static bool TryMoveToward(CombatMatchState state, ref CombatUnit unit, ref CombatUnit target,
            int tickRate, bool allowDiagonal = false)
        {
            // 이미 사거리 내면 이동 불필요
            if (BoardHelper.IsInRange(unit.GridCol, unit.GridRow,
                    target.GridCol, target.GridRow, unit.AttackRange))
                return false;

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

                // 범위 검사
                if (!BoardHelper.IsValidCombatPosition(nc, nr)) continue;

                // 빈 타일인지 검사
                if (state.GetUnitAtGrid(nc, nr) != CombatUnit.InvalidId) continue;

                // 타겟까지 거리 계산
                int dist = BoardHelper.ManhattanDistance(nc, nr, target.GridCol, target.GridRow);

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
                        // 행 방향(전진) 우선
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

            if (bestCol < 0) return false; // 이동 가능한 칸 없음

            // 출발 좌표 기록 (View 보간용)
            unit.MoveFromCol = unit.GridCol;
            unit.MoveFromRow = unit.GridRow;

            // 그리드 업데이트: 출발 칸 해제, 도착 칸 점유 (즉시)
            state.ClearGrid(unit.GridCol, unit.GridRow);
            unit.GridCol = (byte)bestCol;
            unit.GridRow = (byte)bestRow;
            state.SetGrid(bestCol, bestRow, unit.CombatId);

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

            // 대상 후열 행 결정: 자신이 하단(0-3)이면 상단 후열(7), 상단(4-7)이면 하단 후열(0)
            int targetRow = unit.GridRow < 4 ? CombatGrid.Height - 1 : 0;

            // 후열에서 빈 타일 탐색 (적에게 가장 가까운 위치 선호)
            int bestCol = -1;
            int bestRow = -1;
            int bestEnemyDist = int.MaxValue;

            for (int expandRow = 0; expandRow < CombatGrid.Height; expandRow++)
            {
                // 후열부터 확장: row7 → row6 → ... 또는 row0 → row1 → ...
                int checkRow = unit.GridRow < 4
                    ? CombatGrid.Height - 1 - expandRow
                    : expandRow;

                for (int col = 0; col < CombatGrid.Width; col++)
                {
                    if (state.GetUnitAtGrid(col, checkRow) != CombatUnit.InvalidId) continue;

                    // 가장 가까운 적까지 거리 계산
                    int minEnemyDist = int.MaxValue;
                    for (int i = 0; i < state.UnitCount; i++)
                    {
                        ref var enemy = ref state.Units[i];
                        if (!enemy.IsValidTarget || enemy.TeamIndex == unit.TeamIndex) continue;
                        int dist = BoardHelper.ManhattanDistance(col, checkRow, enemy.GridCol, enemy.GridRow);
                        if (dist < minEnemyDist) minEnemyDist = dist;
                    }

                    if (minEnemyDist < bestEnemyDist)
                    {
                        bestEnemyDist = minEnemyDist;
                        bestCol = col;
                        bestRow = checkRow;
                    }
                }

                if (bestCol >= 0) break; // 빈 타일을 찾았으면 탐색 종료
            }

            if (bestCol < 0) return false; // 모든 타일 점유

            // 텔레포트
            int jumpFromCol = unit.GridCol;
            int jumpFromRow = unit.GridRow;
            state.ClearGrid(unit.GridCol, unit.GridRow);
            unit.GridCol = (byte)bestCol;
            unit.GridRow = (byte)bestRow;
            state.SetGrid(bestCol, bestRow, unit.CombatId);

            // 이동 시간 적용 (이동 중 타겟팅 제외)
            unit.MoveFromCol = (byte)jumpFromCol;
            unit.MoveFromRow = (byte)jumpFromRow;
            int moveFrames = unit.GetMoveFrames(tickRate);
            unit.MoveDuration = moveFrames;
            unit.MoveTimer = moveFrames;
            unit.State = CombatState.Moving;

            if (CombatLogger.Enabled) CombatLogger.LogBacklineJump(unit.CombatId, jumpFromCol, jumpFromRow, bestCol, bestRow);

            return true;
        }
    }
}
