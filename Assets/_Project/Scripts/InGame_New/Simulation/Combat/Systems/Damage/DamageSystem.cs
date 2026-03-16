namespace CookApps.AutoChess
{
    /// <summary>
    /// 데미지 계산 시스템.
    /// 물리: Attack × (100 / (100 + Armor))
    /// 마법: SpellPower × (100 / (100 + MagicResist))
    /// 고정: 감소 없이 그대로 적용
    /// </summary>
    public static partial class DamageSystem
    {
        // 테스트 무적 (디버그 전용)
        public static bool PlayerInvincible;
        public static bool EnemyInvincible;

        // ── 상수 ──
        public const int MinDamage = 1;          // 최소 데미지

        /// <summary>물리 데미지 계산 (방어력 + 관통 적용)</summary>
        public static int CalculatePhysicalDamage(int attack, int armor, int atkPierce = 0)
        {
            if (armor < 0) armor = 0;
            // 관통 적용: effectiveArmor = armor * (100 - pierce) / 100
            int effectiveArmor = armor * (100 - atkPierce) / 100;
            if (effectiveArmor < 0) effectiveArmor = 0;
            return attack * 100 / (100 + effectiveArmor);
        }

        /// <summary>마법 데미지 계산 (마법저항 + 관통 적용)</summary>
        public static int CalculateMagicDamage(int spellPower, int magicResist, int resPierce = 0)
        {
            if (magicResist < 0) magicResist = 0;
            int effectiveMR = magicResist * (100 - resPierce) / 100;
            if (effectiveMR < 0) effectiveMR = 0;
            return spellPower * 100 / (100 + effectiveMR);
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

        /// <summary>타입별 데미지 계산 (공격자 관통 반영)</summary>
        public static int CalculateDamage(int rawDamage, DamageType type, ref CombatUnit attacker, ref CombatUnit target)
        {
            int damage = type switch
            {
                DamageType.Physical => CalculatePhysicalDamage(rawDamage, target.Armor, attacker.AtkPierce),
                DamageType.Magical => CalculateMagicDamage(rawDamage, target.MagicResist, attacker.ResPierce),
                DamageType.True => rawDamage,
                _ => rawDamage,
            };

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
            int attackerIndex = -1, DamageType damageType = DamageType.Physical, bool isCrit = false)
        {
            if (!target.IsAlive) return false;

            // 테스트 무적: 데미지 이벤트 발행 + HP 감소 스킵
            if ((PlayerInvincible && target.TeamIndex == 0)
                || (EnemyInvincible && target.TeamIndex == 1))
            {
                state.EventQueue?.PushUnitDamaged(target.CombatId,
                    attackerIndex >= 0 ? state.Units[attackerIndex].CombatId : CombatUnit.InvalidId,
                    damage, damageType, isCrit);
                return false;
            }

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
                damage, damageType, isCrit);

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
    }
}
