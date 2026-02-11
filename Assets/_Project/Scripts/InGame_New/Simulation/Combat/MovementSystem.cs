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
            byte sizeW = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte sizeH = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            // 이미 사거리 내면 이동 불필요
            if (BoardHelper.IsInRangeMulti(unit.GridCol, unit.GridRow, sizeW, sizeH,
                    target.GridCol, target.GridRow,
                    target.SizeW > 0 ? target.SizeW : (byte)1,
                    target.SizeH > 0 ? target.SizeH : (byte)1,
                    unit.AttackRange))
                return false;

            int bestCol = -1;
            int bestRow = -1;
            int bestDist = int.MaxValue;

            byte tSizeW = target.SizeW > 0 ? target.SizeW : (byte)1;
            byte tSizeH = target.SizeH > 0 ? target.SizeH : (byte)1;

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

                // 타겟까지 풋프린트 거리 계산
                int dist = BoardHelper.MinManhattanDistance(nc, nr, sizeW, sizeH,
                    target.GridCol, target.GridRow, tSizeW, tSizeH);

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

            for (int expandRow = 0; expandRow < CombatGrid.Height; expandRow++)
            {
                // 후열부터 확장: row7 → row6 → ... 또는 row0 → row1 → ...
                int checkRow = unit.GridRow < 4
                    ? CombatGrid.Height - 1 - expandRow
                    : expandRow;

                for (int col = 0; col <= CombatGrid.Width - sizeW; col++)
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
            unit.State = CombatState.Moving;

            if (CombatLogger.Enabled) CombatLogger.LogBacklineJump(unit.CombatId, jumpFromCol, jumpFromRow, bestCol, bestRow);

            return true;
        }
    }
}
