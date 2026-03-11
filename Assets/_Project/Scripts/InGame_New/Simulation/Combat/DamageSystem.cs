namespace CookApps.AutoChess
{
    /// <summary>
    /// 데미지 계산 시스템.
    /// 물리: Attack × (100 / (100 + Armor))
    /// 마법: SpellPower × (100 / (100 + MagicResist))
    /// 고정: 감소 없이 그대로 적용
    /// </summary>
    public static class DamageSystem
    {
        // ── 상수 ──
        public const int ManaGainOnAttack = 10;  // 공격 시 마나 획득
        public const int ManaGainOnHit = 10;     // 피격 시 마나 획득
        public const int MinDamage = 1;          // 최소 데미지

        /// <summary>물리 데미지 계산 (방어력 감소 적용)</summary>
        public static int CalculatePhysicalDamage(int attack, int armor)
        {
            if (armor < 0) armor = 0;
            // attack × (100 / (100 + armor))  정수 연산
            return attack * 100 / (100 + armor);
        }

        /// <summary>마법 데미지 계산 (마법저항 감소 적용)</summary>
        public static int CalculateMagicDamage(int spellPower, int magicResist)
        {
            if (magicResist < 0) magicResist = 0;
            return spellPower * 100 / (100 + magicResist);
        }

        /// <summary>타입별 데미지 계산</summary>
        public static int CalculateDamage(int rawDamage, DamageType type, ref CombatUnit target)
        {
            int damage = type switch
            {
                DamageType.Physical => CalculatePhysicalDamage(rawDamage, target.Armor),
                DamageType.Magical => CalculateMagicDamage(rawDamage, target.MagicResist),
                DamageType.True => rawDamage,
                _ => rawDamage,
            };

            if (damage < MinDamage) damage = MinDamage;
            return damage;
        }

        /// <summary>크리티컬 판정 및 배율 적용</summary>
        public static int ApplyCritical(int damage, ref CombatUnit attacker, ref DeterministicRNG rng, out bool isCrit)
        {
            isCrit = rng.Chance(attacker.CritChance);
            if (isCrit)
            {
                // CritMultiplier는 퍼센트 (150 = 1.5x)
                damage = damage * attacker.CritMultiplier / 100;
            }
            return damage;
        }

        /// <summary>
        /// 데미지 적용. 보호막 → HP 순으로 차감.
        /// 사망 처리 포함. 사망 시 true 반환.
        /// attackerIndex: Trait 콜백용 공격자 인덱스 (-1이면 Trait 콜백 생략)
        /// </summary>
        public static bool ApplyDamage(CombatMatchState state, ref CombatUnit target, int damage,
            int attackerIndex = -1, DamageType damageType = DamageType.Physical)
        {
            if (!target.IsAlive) return false;

            int targetIndex = state.FindUnitIndex(target.CombatId);

            // Trait: 나가는 데미지 보정 (공격자)
            if (attackerIndex >= 0)
                damage = TraitSystem.InvokeModifyOutgoingDamage(state, attackerIndex, ref target, damage, damageType);

            // Trait: 들어오는 데미지 보정 (피격자)
            if (targetIndex >= 0)
            {
                // attackerIndex가 없으면 더미 참조 사용
                if (attackerIndex >= 0)
                    damage = TraitSystem.InvokeModifyIncomingDamage(state, ref state.Units[attackerIndex], targetIndex, damage, damageType);
            }

            // 데미지 감소 (DamageReduction 퍼센트)
            if (target.DamageReduction > 0)
            {
                damage = damage * (100 - target.DamageReduction) / 100;
                if (damage < MinDamage) damage = MinDamage;
            }

            // 보호막 먼저 차감 (StatusEffectSystem으로 위임)
            if (target.ShieldAmount > 0)
            {
                int unitIndex = targetIndex >= 0 ? targetIndex : state.FindUnitIndex(target.CombatId);
                damage = StatusEffectSystem.AbsorbShieldDamage(state, unitIndex, damage);
                if (damage <= 0) return false;
            }

            target.CurrentHP -= damage;

            if (CombatLogger.Enabled) CombatLogger.LogDamage(target.CombatId, damage, target.CurrentHP, target.MaxHP);

            state.EventQueue?.PushUnitDamaged(target.CombatId,
                attackerIndex >= 0 ? state.Units[attackerIndex].CombatId : CombatUnit.InvalidId,
                damage, damageType);

            // Trait: 피격 후 콜백 (피격자)
            if (targetIndex >= 0 && attackerIndex >= 0)
                TraitSystem.InvokeOnDamageTaken(state, targetIndex, ref state.Units[attackerIndex], damage);

            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                target.IsAlive = false;
                target.State = CombatState.Dead;

                if (CombatLogger.Enabled) CombatLogger.LogDeath(target.CombatId, target.TeamIndex);

                // Trait: 사망 콜백 (사망자)
                if (targetIndex >= 0 && attackerIndex >= 0)
                    TraitSystem.InvokeOnDeath(state, targetIndex, ref state.Units[attackerIndex]);

                // Trait: 처치 콜백 (공격자)
                if (attackerIndex >= 0)
                    TraitSystem.InvokeOnKill(state, attackerIndex, ref target);

                // 그리드에서 제거 (multi-tile)
                state.ClearGridMulti(target.GridCol, target.GridRow,
                    target.SizeW > 0 ? target.SizeW : (byte)1,
                    target.SizeH > 0 ? target.SizeH : (byte)1);

                // 생존 수 업데이트
                if (target.TeamIndex == 0)
                    state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
                else
                    state.AliveCountB = CombatSetupSystem.CountAliveByTeam(state, 1);

                state.EventQueue?.PushUnitDied(target.SourceEntityId,
                    attackerIndex >= 0 ? state.Units[attackerIndex].SourceEntityId : CombatUnit.InvalidId);
                return true; // 사망
            }

            return false;
        }

        /// <summary>흡혈 효과 적용</summary>
        public static void ApplyLifeSteal(ref CombatUnit attacker, int damageDealt)
        {
            if (attacker.LifeSteal <= 0) return;

            int heal = damageDealt * attacker.LifeSteal / 100;
            if (heal <= 0) return;

            attacker.CurrentHP += heal;
            if (attacker.CurrentHP > attacker.MaxHP)
                attacker.CurrentHP = attacker.MaxHP;
        }

        /// <summary>마나 충전 (공격자: 공격 시, 피격자: 피격 시)</summary>
        public static void ChargeMana(ref CombatUnit unit, int amount)
        {
            if (!unit.IsAlive) return;
            unit.CurrentMana += amount;
            if (unit.CurrentMana > unit.MaxMana)
                unit.CurrentMana = unit.MaxMana;
        }

        /// <summary>
        /// 기본 공격 실행 (근접: 즉시 데미지, 원거리: 투사체 생성)
        /// </summary>
        public static void ExecuteBasicAttack(
            CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target,
            ref DeterministicRNG rng, int tickRate)
        {

            // 범위 기본공격 분기
            if (attacker.HasAreaAttack && AreaAttackRegistry.TryGetPattern(attacker.ChampionSpecId, out var pattern))
            {
                ExecuteAreaAttack(state, ref attacker, ref target, ref rng, tickRate, ref pattern);
                return;
            }

            int attackerIndex = state.FindUnitIndex(attacker.CombatId);

            // Trait: 공격 전 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPreAttack(state, attackerIndex, ref target);

            int rawDamage = attacker.Attack;
            bool isCrit;
            rawDamage = ApplyCritical(rawDamage, ref attacker, ref rng, out isCrit);

            // Trait: 크리티컬 콜백
            if (isCrit && attackerIndex >= 0)
                TraitSystem.InvokeOnCritical(state, attackerIndex, ref target, rawDamage);

            if (attacker.AttackRange <= 1)
            {
                // 근접: 즉시 데미지
                int finalDamage = CalculateDamage(rawDamage, DamageType.Physical, ref target);

                if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, finalDamage, isCrit, false);

                ApplyDamage(state, ref target, finalDamage, attackerIndex, DamageType.Physical);
                ApplyLifeSteal(ref attacker, finalDamage);

                // 피격자 마나 충전
                ChargeMana(ref target, ManaGainOnHit);

                state.EventQueue?.PushUnitAttacked(attacker.SourceEntityId, target.SourceEntityId, finalDamage, isCrit, false);
            }
            else
            {
                // 원거리: 투사체 생성 (풋프린트 기반 거리)
                int dist = BoardHelper.MinManhattanDistance(
                    attacker.GridCol, attacker.GridRow,
                    attacker.SizeW > 0 ? attacker.SizeW : (byte)1,
                    attacker.SizeH > 0 ? attacker.SizeH : (byte)1,
                    target.GridCol, target.GridRow,
                    target.SizeW > 0 ? target.SizeW : (byte)1,
                    target.SizeH > 0 ? target.SizeH : (byte)1);
                int travelFrames = dist * 4; // 기본 4프레임/타일
                if (travelFrames < 1) travelFrames = 1;

                if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, rawDamage, isCrit, true);

                ProjectileSystem.CreateHomingProjectile(
                    state, attacker.CombatId, target.CombatId,
                    rawDamage, isCrit, DamageType.Physical, travelFrames);

                state.EventQueue?.PushUnitAttacked(attacker.SourceEntityId, target.SourceEntityId, rawDamage, isCrit, true);
            }

            // 공격자 마나 충전
            ChargeMana(ref attacker, ManaGainOnAttack);

            // 공격 쿨다운 재설정
            attacker.AttackCooldown = attacker.GetAttackInterval(tickRate);

            // Trait: 공격 후 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
        }

        /// <summary>대기 중인 근접 공격 히트 적용 (ATK 키프레임 도달 시점)</summary>
        public static void ExecutePendingMeleeHit(
            CombatMatchState state, ref CombatUnit attacker, ref DeterministicRNG rng, int tickRate)
        {
            int targetIdx = state.FindUnitIndex(attacker.PendingAtkTargetId);
            attacker.PendingAtkTargetId = CombatUnit.InvalidId;

            // 타겟 유효성 재검증
            if (targetIdx < 0 || !state.Units[targetIdx].IsAlive)
            {
                // 타겟 사망/무효: 데미지 없이 Idle 복귀
                attacker.State = CombatState.Idle;
                return;
            }

            ref var target = ref state.Units[targetIdx];
            int attackerIndex = state.FindUnitIndex(attacker.CombatId);

            // Trait: 공격 전 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPreAttack(state, attackerIndex, ref target);

            // 크리티컬은 CombatAISystem에서 선행 판정 완료 (PendingAtkIsCrit)
            bool isCrit = attacker.PendingAtkIsCrit;
            attacker.PendingAtkIsCrit = false;

            int rawDamage = attacker.Attack;
            if (isCrit)
                rawDamage = rawDamage * attacker.CritMultiplier / 100;

            // Trait: 크리티컬 콜백
            if (isCrit && attackerIndex >= 0)
                TraitSystem.InvokeOnCritical(state, attackerIndex, ref target, rawDamage);

            int finalDamage = CalculateDamage(rawDamage, DamageType.Physical, ref target);

            if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, finalDamage, isCrit, false);

            ApplyDamage(state, ref target, finalDamage, attackerIndex, DamageType.Physical);
            ApplyLifeSteal(ref attacker, finalDamage);
            ChargeMana(ref target, ManaGainOnHit);
            ChargeMana(ref attacker, ManaGainOnAttack);

            // View에 실제 데미지 전달 (isPreTimed: View가 딜레이 없이 즉시 표시)
            state.EventQueue?.PushUnitAttacked(
                attacker.SourceEntityId, target.SourceEntityId, finalDamage, isCrit, false, isPreTimed: true);

            // Trait: 공격 후 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
        }

        /// <summary>회피 판정. 회피 성공 시 true.</summary>
        public static bool TryDodge(ref CombatUnit target, ref DeterministicRNG rng)
        {
            if (target.DodgeChance <= 0) return false;
            return rng.Chance(target.DodgeChance);
        }

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
            ChargeMana(ref attacker, ManaGainOnAttack);
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
            int finalDamage = CalculateDamage(hitDamage, DamageType.Physical, ref unit);
            ApplyDamage(state, ref unit, finalDamage);
            ApplyLifeSteal(ref attacker, finalDamage);
            ChargeMana(ref unit, ManaGainOnHit);
            // isPreTimed=true: 시뮬레이션에서 키프레임 타이밍에 맞춰 발행 → 뷰는 즉시 표시
            state.EventQueue?.PushUnitAttacked(attacker.SourceEntityId, unit.SourceEntityId, finalDamage, isCrit, false, isPreTimed: true);
        }

        private static int Sign(int value)
        {
            if (value > 0) return 1;
            if (value < 0) return -1;
            return 0;
        }
    }
}
