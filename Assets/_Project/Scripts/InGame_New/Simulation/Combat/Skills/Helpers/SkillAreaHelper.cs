namespace CookApps.AutoChess
{
    /// <summary>범위 타겟 검색 + 순회 헬퍼</summary>
    public static class SkillAreaHelper
    {
        public delegate void AreaAction(ref CombatUnit target, int targetIndex);

        // ══════════════════════════════
        // 통합 순회 메서드 — 팀 필터 + 범위 형태를 파라미터로 받음
        // ══════════════════════════════

        /// <summary>범위 내 유닛 순회 (팀 필터 + 범위 형태 통합)</summary>
        public static void ForEachInArea(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius,
            bool ally, SkillAreaShape shape, AreaAction action)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;

                // 팀 필터: ally=true면 같은 팀만, false면 다른 팀만
                if (ally ? unit.TeamIndex != casterTeam : unit.TeamIndex == casterTeam)
                    continue;

                if (!IsInArea(shape, centerCol, centerRow, radius, ref unit))
                    continue;

                action(ref unit, i);
            }
        }

        /// <summary>유닛이 지정된 범위 형태 내에 있는지 판정</summary>
        public static bool IsInArea(SkillAreaShape shape, int centerCol, int centerRow, int radius,
            ref CombatUnit unit)
        {
            byte sw = unit.SizeW > 0 ? unit.SizeW : (byte)1;
            byte sh = unit.SizeH > 0 ? unit.SizeH : (byte)1;

            switch (shape)
            {
                case SkillAreaShape.Circle:
                {
                    int dist = BoardHelper.MinChebyshevDistance(centerCol, centerRow, 1, 1,
                        unit.GridCol, unit.GridRow, sw, sh);
                    return dist <= radius;
                }
                case SkillAreaShape.Diamond:
                {
                    int dist = BoardHelper.MinManhattanDistance(centerCol, centerRow, 1, 1,
                        unit.GridCol, unit.GridRow, sw, sh);
                    return dist <= radius;
                }
                case SkillAreaShape.Plus:
                {
                    int dc = unit.GridCol - centerCol;
                    int dr = unit.GridRow - centerRow;
                    if (dc < 0) dc = -dc;
                    if (dr < 0) dr = -dr;
                    return (dc == 0 && dr <= radius) || (dr == 0 && dc <= radius);
                }
                default:
                {
                    // 기본: 체비셰프 (Circle)
                    int dist = BoardHelper.MinChebyshevDistance(centerCol, centerRow, 1, 1,
                        unit.GridCol, unit.GridRow, sw, sh);
                    return dist <= radius;
                }
            }
        }

        // ══════════════════════════════
        // 하위 호환 래퍼 — 기존 호출부 수정 없이 사용
        // ══════════════════════════════

        /// <summary>원형 범위(체비셰프 거리) 내 적 순회</summary>
        public static void ForEachEnemyInRadius(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
            => ForEachInArea(state, casterTeam, centerCol, centerRow, radius, false, SkillAreaShape.Circle, action);

        /// <summary>다이아몬드 범위(맨해튼 거리) 내 적 순회</summary>
        public static void ForEachEnemyInDiamond(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
            => ForEachInArea(state, casterTeam, centerCol, centerRow, radius, false, SkillAreaShape.Diamond, action);

        /// <summary>Plus(+) 형태 범위 내 적 순회</summary>
        public static void ForEachEnemyInPlus(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
            => ForEachInArea(state, casterTeam, centerCol, centerRow, radius, false, SkillAreaShape.Plus, action);

        /// <summary>원형 범위 내 아군 순회</summary>
        public static void ForEachAllyInRadius(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
            => ForEachInArea(state, casterTeam, centerCol, centerRow, radius, true, SkillAreaShape.Circle, action);

        /// <summary>다이아몬드 범위 내 아군 순회</summary>
        public static void ForEachAllyInDiamond(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
            => ForEachInArea(state, casterTeam, centerCol, centerRow, radius, true, SkillAreaShape.Diamond, action);

        /// <summary>Plus(+) 형태 범위 내 아군 순회</summary>
        public static void ForEachAllyInPlus(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius, AreaAction action)
            => ForEachInArea(state, casterTeam, centerCol, centerRow, radius, true, SkillAreaShape.Plus, action);

        // ══════════════════════════════
        // 직선 순회 (구조가 다르므로 별도 유지)
        // ══════════════════════════════

        /// <summary>직선 범위 내 적 순회</summary>
        public static void ForEachEnemyInLine(CombatMatchState state, byte casterTeam,
            int startCol, int startRow, int dirCol, int dirRow, int length, AreaAction action)
        {
            int col = startCol;
            int row = startRow;

            for (int step = 0; step < length; step++)
            {
                col += dirCol;
                row += dirRow;

                if (!BoardHelper.IsValidCombatPosition(col, row)) break;

                int combatId = state.GetUnitAtGrid(col, row);
                if (combatId == CombatUnit.InvalidId) continue;

                int idx = state.FindUnitIndex(combatId);
                if (idx < 0) continue;

                ref var unit = ref state.Units[idx];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == casterTeam) continue;

                action(ref unit, idx);
            }
        }

        // ══════════════════════════════
        // 타겟 검색
        // ══════════════════════════════

        /// <summary>최적 AoE 타겟 찾기 (가장 많은 적을 포함하는 적 유닛의 CombatId)</summary>
        public static int FindBestAoETarget(CombatMatchState state, ref CombatUnit caster, int radius)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestCount = 0;
            int bestDist = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var candidate = ref state.Units[i];
                if (!candidate.IsAlive) continue;
                if (candidate.TeamIndex == caster.TeamIndex) continue;

                int count = CountEnemiesInRadius(state, caster.TeamIndex,
                    candidate.GridCol, candidate.GridRow, radius);

                int dist = BoardHelper.MinManhattanDistance(
                    caster.GridCol, caster.GridRow,
                    caster.SizeW > 0 ? caster.SizeW : (byte)1,
                    caster.SizeH > 0 ? caster.SizeH : (byte)1,
                    candidate.GridCol, candidate.GridRow,
                    candidate.SizeW > 0 ? candidate.SizeW : (byte)1,
                    candidate.SizeH > 0 ? candidate.SizeH : (byte)1);

                if (count > bestCount || (count == bestCount && dist < bestDist))
                {
                    bestTarget = candidate.CombatId;
                    bestCount = count;
                    bestDist = dist;
                }
            }

            return bestTarget;
        }

        /// <summary>최저 HP 아군 찾기 (CombatId 반환)</summary>
        public static int FindLowestHPAlly(CombatMatchState state, byte teamIndex)
        {
            int bestTarget = CombatUnit.InvalidId;
            int bestHP = int.MaxValue;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex != teamIndex) continue;

                if (unit.CurrentHP < bestHP)
                {
                    bestTarget = unit.CombatId;
                    bestHP = unit.CurrentHP;
                }
            }

            return bestTarget;
        }

        /// <summary>HP가 가장 낮은 아군 N명의 CombatId 배열 반환. 실제 찾은 수 반환.</summary>
        public static int FindLowestHPAllies(CombatMatchState state, byte teamIndex, int count, int[] resultBuffer)
        {
            int found = 0;
            for (int c = 0; c < count; c++)
            {
                int bestIdx = -1;
                int bestHP = int.MaxValue;
                for (int i = 0; i < state.UnitCount; i++)
                {
                    ref var u = ref state.Units[i];
                    if (u.TeamIndex != teamIndex || !u.IsAlive) continue;

                    bool alreadySelected = false;
                    for (int j = 0; j < found; j++)
                    {
                        if (resultBuffer[j] == u.CombatId) { alreadySelected = true; break; }
                    }
                    if (alreadySelected) continue;

                    if (u.CurrentHP < bestHP)
                    {
                        bestHP = u.CurrentHP;
                        bestIdx = i;
                    }
                }
                if (bestIdx < 0) break;
                resultBuffer[found++] = state.Units[bestIdx].CombatId;
            }
            return found;
        }

        /// <summary>범위 내 적 수 카운트</summary>
        public static int CountEnemiesInRadius(CombatMatchState state, byte casterTeam,
            int centerCol, int centerRow, int radius)
        {
            int count = 0;
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == casterTeam) continue;

                int dist = BoardHelper.MinChebyshevDistance(centerCol, centerRow, 1, 1,
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1);
                if (dist <= radius)
                    count++;
            }
            return count;
        }
    }
}
