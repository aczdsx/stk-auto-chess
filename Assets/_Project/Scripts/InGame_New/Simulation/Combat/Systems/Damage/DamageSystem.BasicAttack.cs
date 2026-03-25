namespace CookApps.AutoChess
{
    public static partial class DamageSystem
    {
        /// <summary>
        /// 기본 공격 실행 (근접: 즉시 데미지, 원거리: 투사체 생성)
        /// Oracle 힐러가 아군 타겟일 경우 힐 경로로 분기.
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

            // 회피 판정은 CombatAISystem에서 처리 (공격 시작 전 판정)

            // Trait: 공격 전 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPreAttack(state, attackerIndex, ref target);

            // ── Oracle 힐러: 아군 타겟이면 힐 경로 ──
            if (attacker.IsHealer && target.TeamIndex == attacker.TeamIndex)
            {
                var healTrait = attackerIndex >= 0
                    ? TraitSystem.FindTrait<OracleHealerTrait>(state, attackerIndex)
                    : null;
                if (healTrait == null) return;

                int healAmount = healTrait.CalculateHealAmount(ref attacker, ref target);

                if (attacker.AttackRange <= 1)
                {
                    // 근접 힐: 즉시 적용
                    SkillDamageHelper.Heal(state, ref target, healAmount);
                    if (CombatLogger.Enabled) CombatLogger.LogHeal(target.CombatId, healAmount, target.CurrentHP, target.MaxHP);

                    state.EventQueue?.PushUnitAttacked(attacker.CombatId, target.CombatId, 0, false, false);
                }
                else
                {
                    // 원거리 힐: 투사체 생성 (도착 시 힐 적용)
                    int travelFrames = CalcProjectileTravelFrames(ref attacker, ref target, tickRate);
                    ProjectileSystem.CreateHomingProjectile(
                        state, attacker.CombatId, target.CombatId,
                        healAmount, false, DamageType.Physical, travelFrames,
                        hitBehavior: ProjectileHitBehavior.HealAlly);

                    state.EventQueue?.PushUnitAttacked(attacker.CombatId, target.CombatId, 0, false, true);
                }

                // 공통: 마나 충전, 쿨다운, PostAttack
                ChargeMana(ref attacker, attacker.ManaGainOnAttack);
                attacker.AttackCooldown = attacker.GetAttackInterval(tickRate);

                if (attackerIndex >= 0)
                    TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
                return;
            }

            // ── 통상 데미지 경로 ──
            int rawDamage = attacker.Attack;
            bool isCrit;
            rawDamage = ApplyCritical(rawDamage, ref attacker, ref rng, out isCrit);

            // Trait: 크리티컬 콜백
            if (isCrit && attackerIndex >= 0)
                TraitSystem.InvokeOnCritical(state, attackerIndex, ref target, rawDamage);

            if (attacker.AttackRange <= 1)
            {
                // 근접: 즉시 데미지 (관통 반영)
                int finalDamage = CalculateDamage(rawDamage, DamageType.Physical, ref attacker, ref target);

                if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, finalDamage, isCrit, false);

                // UnitAttacked를 ApplyDamage보다 먼저 발행 (데미지 폰트 중복 방지)
                state.EventQueue?.PushUnitAttacked(attacker.CombatId, target.CombatId, finalDamage, isCrit, false);

                ApplyDamage(state, ref target, finalDamage, attackerIndex, DamageType.Physical, isCrit, isBasicAttack: true);
                ApplyLifeSteal(state, ref attacker, finalDamage);

                // 피격자 마나 충전
                ChargeMana(ref target, target.ManaGainOnHit);

                // 공격자 마나 충전
                ChargeMana(ref attacker, attacker.ManaGainOnAttack);

                // 공격 쿨다운 재설정
                attacker.AttackCooldown = attacker.GetAttackInterval(tickRate);

                // Trait: 공격 후 콜백 (근접: 데미지 적용 후 즉시)
                if (attackerIndex >= 0)
                    TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
            }
            else
            {
                // 원거리: 투사체 생성
                int travelFrames = CalcProjectileTravelFrames(ref attacker, ref target, tickRate);

                if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, rawDamage, isCrit, true);

                // 투사체 VFX 오버라이드 소비 (Sharpshooter 관통 등)
                byte projVfxOverride = attacker.ProjectileVfxOverride;
                if (projVfxOverride != ProjectileVfxId.None) attacker.ProjectileVfxOverride = ProjectileVfxId.None;

                ProjectileSystem.CreateHomingProjectile(
                    state, attacker.CombatId, target.CombatId,
                    rawDamage, isCrit, DamageType.Physical, travelFrames,
                    projectileVfxOverride: projVfxOverride);

                state.EventQueue?.PushUnitAttacked(attacker.CombatId, target.CombatId, rawDamage, isCrit, true);

                // 공격자 마나 충전
                ChargeMana(ref attacker, attacker.ManaGainOnAttack);

                // 공격 쿨다운 재설정
                attacker.AttackCooldown = attacker.GetAttackInterval(tickRate);
            }
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

            // 회피 판정은 CombatAISystem에서 처리 (공격 시작 전 판정)

            // Trait: 공격 전 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPreAttack(state, attackerIndex, ref target);

            // ── Oracle 힐러: 아군 타겟이면 힐 경로 ──
            if (attacker.IsHealer && target.TeamIndex == attacker.TeamIndex)
            {
                var healTrait = attackerIndex >= 0
                    ? TraitSystem.FindTrait<OracleHealerTrait>(state, attackerIndex)
                    : null;
                if (healTrait == null) return;

                int healAmount = healTrait.CalculateHealAmount(ref attacker, ref target);
                SkillDamageHelper.Heal(state, ref target, healAmount);

                if (CombatLogger.Enabled) CombatLogger.LogHeal(target.CombatId, healAmount, target.CurrentHP, target.MaxHP);

                // 공격자 마나만 충전 (힐은 피격이 아님)
                ChargeMana(ref attacker, attacker.ManaGainOnAttack);

                if (attackerIndex >= 0)
                    TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
                return;
            }

            // ── 통상 데미지 경로 ──
            // 크리티컬은 CombatAISystem에서 선행 판정 완료 (PendingAtkIsCrit)
            bool isCrit = attacker.PendingAtkIsCrit;
            attacker.PendingAtkIsCrit = false;

            int rawDamage = attacker.Attack;
            if (isCrit)
                rawDamage = rawDamage * attacker.CritPower / 100;

            // Trait: 크리티컬 콜백
            if (isCrit && attackerIndex >= 0)
                TraitSystem.InvokeOnCritical(state, attackerIndex, ref target, rawDamage);

            int finalDamage = CalculateDamage(rawDamage, DamageType.Physical, ref attacker, ref target);

            if (CombatLogger.Enabled) CombatLogger.LogAttack(attacker.CombatId, target.CombatId, finalDamage, isCrit, false);

            // View에 실제 데미지 전달 (isPreTimed: View가 딜레이 없이 즉시 표시)
            // ApplyDamage보다 먼저 발행해야 View의 _pendingMeleeTargetIds에 등록된 후
            // UnitDamaged 이벤트가 스킵됨 (데미지 폰트 중복 방지)
            state.EventQueue?.PushUnitAttacked(
                attacker.CombatId, target.CombatId, finalDamage, isCrit, false, isPreTimed: true);

            ApplyDamage(state, ref target, finalDamage, attackerIndex, DamageType.Physical, isCrit, isBasicAttack: true);
            ApplyLifeSteal(state, ref attacker, finalDamage);

            ChargeMana(ref target, target.ManaGainOnHit);
            ChargeMana(ref attacker, attacker.ManaGainOnAttack);

            // Trait: 공격 후 콜백
            if (attackerIndex >= 0)
                TraitSystem.InvokeOnPostAttack(state, attackerIndex, ref target);
        }

        /// <summary>
        /// 명중/회피 판정. 미스 시 true 반환.
        /// HitChance(%) = attacker.HitChance - target.DodgeChance
        /// HitChance가 100 이상이면 무조건 명중, 0 이하이면 무조건 미스.
        /// </summary>
        public static bool TryDodge(ref CombatUnit attacker, ref CombatUnit target, ref DeterministicRNG rng)
        {
            int hitChance = attacker.HitChance - target.DodgeChance;
            if (hitChance >= 100) return false; // 무조건 명중
            if (hitChance <= 0) return true;    // 무조건 미스
            return !rng.Chance(hitChance);       // hitChance% 확률로 명중 → 실패 시 회피
        }
    }
}
