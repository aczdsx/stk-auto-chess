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
        /// </summary>
        public static bool ApplyDamage(CombatMatchState state, ref CombatUnit target, int damage)
        {
            if (!target.IsAlive) return false;

            // 데미지 감소 (DamageReduction 퍼센트)
            if (target.DamageReduction > 0)
            {
                damage = damage * (100 - target.DamageReduction) / 100;
                if (damage < MinDamage) damage = MinDamage;
            }

            // 보호막 먼저 차감 (StatusEffectSystem으로 위임)
            if (target.ShieldAmount > 0)
            {
                int unitIndex = state.FindUnitIndex(target.CombatId);
                damage = StatusEffectSystem.AbsorbShieldDamage(state, unitIndex, damage);
                if (damage <= 0) return false;
            }

            target.CurrentHP -= damage;

            if (CombatLogger.Enabled) CombatLogger.LogDamage(target.CombatId, damage, target.CurrentHP, target.MaxHP);

            if (target.CurrentHP <= 0)
            {
                target.CurrentHP = 0;
                target.IsAlive = false;
                target.State = CombatState.Dead;

                if (CombatLogger.Enabled) CombatLogger.LogDeath(target.CombatId, target.TeamIndex);

                // 그리드에서 제거 (multi-tile)
                state.ClearGridMulti(target.GridCol, target.GridRow,
                    target.SizeW > 0 ? target.SizeW : (byte)1,
                    target.SizeH > 0 ? target.SizeH : (byte)1);

                // 생존 수 업데이트
                if (target.TeamIndex == 0)
                    state.AliveCountA = CombatSetupSystem.CountAliveByTeam(state, 0);
                else
                    state.AliveCountB = CombatSetupSystem.CountAliveByTeam(state, 1);

                state.EventQueue?.PushUnitDied(target.SourceEntityId, CombatUnit.InvalidId);
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
            int rawDamage = attacker.Attack;
            bool isCrit;
            rawDamage = ApplyCritical(rawDamage, ref attacker, ref rng, out isCrit);

            if (attacker.AttackRange <= 1)
            {
                // 근접: 즉시 데미지
                int finalDamage = CalculateDamage(rawDamage, DamageType.Physical, ref target);

                if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, finalDamage, isCrit, false);

                ApplyDamage(state, ref target, finalDamage);
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
        }

        /// <summary>회피 판정. 회피 성공 시 true.</summary>
        public static bool TryDodge(ref CombatUnit target, ref DeterministicRNG rng)
        {
            if (target.DodgeChance <= 0) return false;
            return rng.Chance(target.DodgeChance);
        }
    }
}
