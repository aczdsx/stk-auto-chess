namespace CookApps.AutoChess
{
    public static partial class DamageSystem
    {
        // ═══════════════════════════════════════════════
        //  범위 기본공격 (키프레임 타이밍 기반 멀티히트)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// 범위 기본공격 시작. 데미지/크리를 계산하고 첫 히트 타이머를 설정.
        /// 이후 매 틱 TickAreaAttack()에서 히트 타이밍 처리.
        /// </summary>
        private static void ExecuteAreaAttack(
            CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target,
            ref DeterministicRNG rng, int tickRate, ref AreaAttackPattern pattern)
        {
            int rawDamage = attacker.Attack;
            bool isCrit;
            rawDamage = ApplyCritical(rawDamage, ref attacker, ref rng, out isCrit);

            // facing 방향 계산 (타겟 기준)
            int dirCol = Sign(target.GridCol - attacker.GridCol);
            int dirRow = Sign(target.GridRow - attacker.GridRow);
            // 대각선 방지: 주축 1개만 사용 (row 우선)
            if (dirCol != 0 && dirRow != 0) dirCol = 0;
            // 방향이 없으면 기본 전방 (row+)
            if (dirCol == 0 && dirRow == 0) dirRow = 1;

            // 히트별 데미지 분할
            int hitDamage = pattern.HitCount > 0 ? rawDamage / pattern.HitCount : rawDamage;
            if (hitDamage < MinDamage) hitDamage = MinDamage;

            if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, rawDamage, isCrit, false);

            // 멀티히트 상태 저장
            attacker.IsAreaAttacking = true;
            attacker.AreaHitIndex = 0;
            attacker.AreaHitDamage = hitDamage;
            attacker.AreaHitIsCrit = isCrit;
            attacker.AreaDirCol = (sbyte)dirCol;
            attacker.AreaDirRow = (sbyte)dirRow;

            // 첫 히트 타이머 설정 (AttackSpeed 반영: slow 디버프 시 딜레이 증가)
            var firstHit = pattern.GetHit(0);
            attacker.AreaHitTimer = CalcHitDelayFrames(firstHit.DelayMs, tickRate, attacker.AttackSpeed);

            // 공격자 마나 충전
            ChargeMana(ref attacker, attacker.ManaGainOnAttack);
        }

        /// <summary>
        /// 범위 공격 틱. 매 프레임 호출하여 히트 타이밍 도달 시 해당 히트 실행.
        /// 모든 히트 완료 후 공격 쿨다운 설정.
        /// </summary>
        public static void TickAreaAttack(CombatMatchState state, ref CombatUnit unit, int tickRate)
        {
            if (!unit.IsAreaAttacking) return;

            unit.AreaHitTimer--;
            if (unit.AreaHitTimer > 0) return;

            // 패턴 조회
            if (!AreaAttackRegistry.TryGetPattern(unit.ChampionSpecId, out var pattern))
            {
                unit.IsAreaAttacking = false;
                unit.AttackCooldown = unit.GetAttackInterval(tickRate);
                return;
            }

            // 현재 히트 실행
            int hitIdx = unit.AreaHitIndex;
            var hit = pattern.GetHit(hitIdx);
            ApplyAreaHit(state, ref unit, unit.AreaHitDamage, unit.AreaHitIsCrit,
                unit.AreaDirCol, unit.AreaDirRow, ref hit);

            // 다음 히트로 진행
            unit.AreaHitIndex++;

            if (unit.AreaHitIndex >= pattern.HitCount)
            {
                // 모든 히트 완료
                unit.IsAreaAttacking = false;
                unit.AttackCooldown = unit.GetAttackInterval(tickRate);
            }
            else
            {
                // 다음 히트까지의 딜레이 (현재 히트와 다음 히트의 시간 차이)
                var nextHit = pattern.GetHit(unit.AreaHitIndex);
                int currentFrames = CalcHitDelayFrames(hit.DelayMs, tickRate, unit.AttackSpeed);
                int nextFrames = CalcHitDelayFrames(nextHit.DelayMs, tickRate, unit.AttackSpeed);
                unit.AreaHitTimer = nextFrames - currentFrames;
                if (unit.AreaHitTimer <= 0) unit.AreaHitTimer = 1;
            }
        }

        /// <summary>밀리초 딜레이를 프레임으로 변환 (AttackSpeed 반영)</summary>
        private static int CalcHitDelayFrames(int delayMs, int tickRate, int attackSpeed)
        {
            // AttackSpeed 100 = 1.0x 속도, slow(50) = 2x 딜레이, fast(200) = 0.5x 딜레이
            if (attackSpeed <= 0) attackSpeed = 100;
            int frames = delayMs * tickRate * 100 / (1000 * attackSpeed);
            return frames > 0 ? frames : 1;
        }

        /// <summary>단일 히트의 범위 판정 + 데미지 적용</summary>
        private static void ApplyAreaHit(
            CombatMatchState state, ref CombatUnit attacker,
            int hitDamage, bool isCrit, int dirCol, int dirRow, ref AreaAttackHit hit)
        {
            // 기준점: 공격자 위치 + facing 방향 * FrontOffset
            int originCol = attacker.GridCol + dirCol * hit.FrontOffset;
            int originRow = attacker.GridRow + dirRow * hit.FrontOffset;

            switch (hit.Shape)
            {
                case AreaAttackShape.Cross:
                    ApplyAreaCross(state, ref attacker, hitDamage, isCrit,
                        originCol, originRow, dirCol, dirRow, hit.Size);
                    break;
                case AreaAttackShape.Line:
                    ApplyAreaLine(state, ref attacker, hitDamage, isCrit,
                        attacker.GridCol, attacker.GridRow, dirCol, dirRow, hit.Size);
                    break;
                case AreaAttackShape.Radius:
                    ApplyAreaRadius(state, ref attacker, hitDamage, isCrit,
                        originCol, originRow, hit.Size);
                    break;
                default: // Single — 메인 타겟만
                    break;
            }
        }

        /// <summary>Cross 범위: facing 수직 방향 ±size칸</summary>
        private static void ApplyAreaCross(
            CombatMatchState state, ref CombatUnit attacker,
            int hitDamage, bool isCrit,
            int originCol, int originRow, int dirCol, int dirRow, int size)
        {
            // 수직 방향 벡터
            int perpCol = -dirRow;
            int perpRow = dirCol;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive || unit.TeamIndex == attacker.TeamIndex) continue;

                // 유닛 중심까지의 facing/perp 거리 계산
                int deltaCol = unit.GridCol - originCol;
                int deltaRow = unit.GridRow - originRow;

                // facing 방향 거리 (0이어야 같은 줄)
                int facingDist = deltaCol * dirCol + deltaRow * dirRow;
                if (facingDist != 0) continue;

                // 수직 방향 거리
                int perpDist = deltaCol * perpCol + deltaRow * perpRow;
                if (perpDist < -size || perpDist > size) continue;

                ApplyAreaDamageToUnit(state, ref attacker, ref state.Units[i], hitDamage, isCrit);
            }
        }

        /// <summary>Line 범위: facing 방향으로 size칸 직선</summary>
        private static void ApplyAreaLine(
            CombatMatchState state, ref CombatUnit attacker,
            int hitDamage, bool isCrit,
            int startCol, int startRow, int dirCol, int dirRow, int size)
        {
            int col = startCol;
            int row = startRow;

            for (int step = 0; step < size; step++)
            {
                col += dirCol;
                row += dirRow;

                if (!BoardHelper.IsValidCombatPosition(col, row)) break;

                int combatId = state.GetUnitAtGrid(col, row);
                if (combatId == CombatUnit.InvalidId) continue;

                int idx = state.FindUnitIndex(combatId);
                if (idx < 0) continue;

                ref var unit = ref state.Units[idx];
                if (!unit.IsAlive || unit.TeamIndex == attacker.TeamIndex) continue;

                ApplyAreaDamageToUnit(state, ref attacker, ref state.Units[idx], hitDamage, isCrit);
            }
        }

        /// <summary>Radius 범위: 체비셰프 거리 기반 원형</summary>
        private static void ApplyAreaRadius(
            CombatMatchState state, ref CombatUnit attacker,
            int hitDamage, bool isCrit,
            int centerCol, int centerRow, int radius)
        {
            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive || unit.TeamIndex == attacker.TeamIndex) continue;

                int dist = BoardHelper.MinChebyshevDistance(
                    centerCol, centerRow, 1, 1,
                    unit.GridCol, unit.GridRow,
                    unit.SizeW > 0 ? unit.SizeW : (byte)1,
                    unit.SizeH > 0 ? unit.SizeH : (byte)1);
                if (dist > radius) continue;

                ApplyAreaDamageToUnit(state, ref attacker, ref state.Units[i], hitDamage, isCrit);
            }
        }

        /// <summary>범위 공격 피격 유닛에 데미지 적용 + 이벤트 발행 (isPreTimed=true)</summary>
        private static void ApplyAreaDamageToUnit(
            CombatMatchState state, ref CombatUnit attacker, ref CombatUnit unit,
            int hitDamage, bool isCrit)
        {
            int finalDamage = CalculateDamage(hitDamage, DamageType.Physical, ref attacker, ref unit);
            // UnitAttacked를 ApplyDamage보다 먼저 발행 (데미지 폰트 중복 방지)
            // isPreTimed=true: 시뮬레이션에서 키프레임 타이밍에 맞춰 발행 → 뷰는 즉시 표시
            state.EventQueue?.PushUnitAttacked(attacker.CombatId, unit.CombatId, finalDamage, isCrit, false, isPreTimed: true);
            ApplyDamage(state, ref unit, finalDamage, isCrit: isCrit);
            ApplyLifeSteal(state, ref attacker, finalDamage);
            ChargeMana(ref unit, unit.ManaGainOnHit);
        }

        private static int Sign(int value)
        {
            if (value > 0) return 1;
            if (value < 0) return -1;
            return 0;
        }
    }
}
