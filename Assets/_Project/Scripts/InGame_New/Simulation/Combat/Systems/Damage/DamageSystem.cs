namespace CookApps.AutoChess
{
    /// <summary>
    /// 데미지 계산 시스템.
    /// 저항: damage *= 1 - clamp(Reduce/100 * (1 - Pierce/100), 0, RESIST_CAP)
    ///   물리 → AdReduce / AtkPierce
    ///   마법 → ApReduce / ResPierce
    /// DEF(Armor): damage = damage * 100 / (100 + Armor) — 물/마 공통 최종 감산
    /// 고정: 감소 없이 그대로 적용
    /// </summary>
    public static partial class DamageSystem
    {
        // 테스트 무적 (디버그 전용)
        public static bool PlayerInvincible;
        public static bool EnemyInvincible;

        // ── 상수 ──
        public const int MinDamage = 1;          // 최소 데미지
        private const int ResistCapPercent = 70;  // 저항 상한 70%

        /// <summary>저항 적용 (AdReduce/ApReduce + 관통)</summary>
        /// <param name="damage">원본 데미지</param>
        /// <param name="resistPercent">저항률 (정수 퍼센트, 0-100)</param>
        /// <param name="piercePercent">관통률 (정수 퍼센트, 0-100)</param>
        public static int ApplyResist(int damage, int resistPercent, int piercePercent = 0)
        {
            if (resistPercent <= 0) return damage;
            if (piercePercent < 0) piercePercent = 0;
            if (piercePercent > 100) piercePercent = 100;

            // effectResist = resist * (100 - pierce) / 100, 상한 ResistCapPercent
            int effectResist = resistPercent * (100 - piercePercent) / 100;
            if (effectResist > ResistCapPercent) effectResist = ResistCapPercent;
            if (effectResist <= 0) return damage;

            return damage * (100 - effectResist) / 100;
        }

        /// <summary>DEF(Armor) 최종 감산 — 물/마 공통</summary>
        public static int ApplyDef(int damage, int armor)
        {
            if (armor <= 0) return damage;
            return damage * 100 / (100 + armor);
        }

        /// <summary>타입별 데미지 계산 (공격자 없음 — 관통 미적용)</summary>
        public static int CalculateDamage(int rawDamage, DamageType type, ref CombatUnit target)
        {
            int damage = type switch
            {
                DamageType.Physical => ApplyResist(rawDamage, target.AdReduce),
                DamageType.Magical => ApplyResist(rawDamage, target.ApReduce),
                DamageType.True => rawDamage,
                _ => rawDamage,
            };

            // True 데미지가 아니면 DEF 최종 감산
            if (type != DamageType.True)
                damage = ApplyDef(damage, target.Def);

            if (damage < MinDamage) damage = MinDamage;
            return damage;
        }

        /// <summary>타입별 데미지 계산 (공격자 관통 반영)</summary>
        public static int CalculateDamage(int rawDamage, DamageType type, ref CombatUnit attacker, ref CombatUnit target)
        {
            int damage = type switch
            {
                DamageType.Physical => ApplyResist(rawDamage, target.AdReduce, attacker.AtkPierce),
                DamageType.Magical => ApplyResist(rawDamage, target.ApReduce, attacker.ResPierce),
                DamageType.True => rawDamage,
                _ => rawDamage,
            };

            // True 데미지가 아니면 DEF 최종 감산
            if (type != DamageType.True)
                damage = ApplyDef(damage, target.Def);

            if (damage < MinDamage) damage = MinDamage;
            return damage;
        }

        /// <summary>크리티컬 판정 및 배율 적용</summary>
        public static int ApplyCritical(int damage, ref CombatUnit attacker, ref DeterministicRNG rng, out bool isCrit)
        {
            isCrit = rng.Chance(attacker.CritRate);
            if (isCrit)
            {
                // CritPower는 퍼센트 (150 = 1.5x)
                damage = damage * attacker.CritPower / 100;
            }
            return damage;
        }

        /// <summary>
        /// 데미지 적용. 보호막 → HP 순으로 차감.
        /// 사망 처리 포함. 사망 시 true 반환.
        /// attackerIndex: Trait 콜백용 공격자 인덱스 (-1이면 Trait 콜백 생략)
        /// </summary>
        public static bool ApplyDamage(CombatMatchState state, ref CombatUnit target, int damage,
            int attackerIndex = -1, DamageType damageType = DamageType.Physical, bool isCrit = false,
            bool isBasicAttack = false)
        {
            if (!target.IsAlive) return false;

            int targetIndex = state.FindUnitIndex(target.CombatId);

            // 직업 패시브: 들어오는 데미지 보정 (피격자 — Guardian 쉴드)
            if (targetIndex >= 0)
                damage = JobPassiveLogic.ModifyIncomingDamage(state, targetIndex, damage, isBasicAttack);

            // Trait에 의해 데미지가 0 이하가 된 경우 (예: GuardianEndure 블록)
            // 이벤트 발행 + OnDamageTaken 콜백 후 HP 차감 스킵
            // TODO: 기획 확정 후 damage=0 시 데미지 텍스트 표현 변경 (예: "Block", "면역" 등)
            if (damage <= 0)
            {
                state.EventQueue?.PushUnitDamaged(target.CombatId,
                    attackerIndex >= 0 ? state.Units[attackerIndex].CombatId : CombatUnit.InvalidId,
                    0, damageType, isCrit);

                return false;
            }

            // 테스트 무적: HP 감소만 스킵
            if ((PlayerInvincible && target.TeamIndex == 0)
                || (EnemyInvincible && target.TeamIndex == 1))
            {
                state.EventQueue?.PushUnitDamaged(target.CombatId,
                    attackerIndex >= 0 ? state.Units[attackerIndex].CombatId : CombatUnit.InvalidId,
                    damage, damageType, isCrit);

                return false;
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
                damage, damageType, isCrit);

            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                target.IsAlive = false;
                target.State = CombatState.Dead;

                if (CombatLogger.Enabled) CombatLogger.LogDeath(target.CombatId, target.TeamIndex);

                // 직업 패시브: 처치 콜백 (스킬 킬 마나 리셋)
                if (attackerIndex >= 0)
                    JobPassiveLogic.OnKill(state, attackerIndex, ref target);

                // 그리드에서 제거 (multi-tile)
                state.ClearGridMulti(target.GridCol, target.GridRow,
                    target.SizeW > 0 ? target.SizeW : (byte)1,
                    target.SizeH > 0 ? target.SizeH : (byte)1);

                // 생존 수 업데이트
                if (target.TeamIndex == 0)
                    state.AliveCountA--;
                else
                    state.AliveCountB--;

                Debug.Log(
                    $"[InGame_New][Death] victimCombatId={target.CombatId} " +
                    $"killerCombatId={(attackerIndex >= 0 ? state.Units[attackerIndex].CombatId : CombatUnit.InvalidId)} " +
                    $"aliveA={state.AliveCountA} aliveB={state.AliveCountB}");

                // CombatId 기반으로 전달 (SourceEntityId는 PvE 몬스터가 -1이라 식별 불가)
                state.EventQueue?.PushUnitDied(target.CombatId,
                    attackerIndex >= 0 ? state.Units[attackerIndex].CombatId : CombatUnit.InvalidId,
                    target.CombatId);
                return true; // 사망
            }

            return false;
        }

        /// <summary>흡혈 효과 적용</summary>
        public static void ApplyLifeSteal(CombatMatchState state, ref CombatUnit attacker, int damageDealt)
        {
            if (attacker.LifeSteal <= 0) return;

            int heal = damageDealt * attacker.LifeSteal / 100;
            if (heal <= 0) return;

            attacker.CurrentHP += heal;
            if (attacker.CurrentHP > attacker.MaxHP)
                attacker.CurrentHP = attacker.MaxHP;

            state.EventQueue?.PushUnitHealed(attacker.CombatId, heal);
        }

        /// <summary>투사체 비행 프레임 계산 (풋프린트 기반 맨해튼 거리, ~0.125초/타일)</summary>
        public static int CalcProjectileTravelFrames(ref CombatUnit source, ref CombatUnit target, int tickRate)
        {
            int dist = BoardHelper.MinManhattanDistance(
                source.GridCol, source.GridRow,
                source.SizeW > 0 ? source.SizeW : (byte)1,
                source.SizeH > 0 ? source.SizeH : (byte)1,
                target.GridCol, target.GridRow,
                target.SizeW > 0 ? target.SizeW : (byte)1,
                target.SizeH > 0 ? target.SizeH : (byte)1);
            int frames = dist * tickRate / 8;
            return frames < 1 ? 1 : frames;
        }

        /// <summary>마나 충전 (공격자: 공격 시, 피격자: 피격 시)</summary>
        public static void ChargeMana(ref CombatUnit unit, int amount)
        {
            if (!unit.IsAlive) return;
            unit.CurrentMana += amount;
            if (unit.CurrentMana > unit.MaxMana)
                unit.CurrentMana = unit.MaxMana;
        }
    }
}
