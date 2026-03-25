namespace CookApps.AutoChess
{
    /// <summary>
    /// 직업군 패시브 로직 집중 static 클래스.
    /// Trait 클래스는 얇은 디스패처 역할만 하고, 실제 로직은 여기서 처리.
    /// </summary>
    public static class JobPassiveSystem
    {
        // CharacterPositionType 값 (SpecEnums.cs 참조)
        private const byte PosGuardian = 1;
        private const byte PosEsper = 7;
        private const byte PosSharpshooter = 8;
        private const byte PosGhost = 9;
        private const byte PosStriker = 12;

        /// <summary>
        /// 매치 내 모든 유닛에 직업 패시브 Trait 부착.
        /// CombatSetupSystem.SetupMatch / SetupPvEMatch 직후에 호출.
        /// ChampionSpec.PositionType과 JobPassiveParam0/1로 Trait 생성.
        /// </summary>
        public static void SetupJobPassives(CombatMatchState state, GameWorld world)
        {
            if (world.Pool == null) return;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;

                // ChampionSpec에서 PositionType과 파라미터 조회
                var spec = FindSpec(world, unit.ChampionSpecId);
                if (spec.PositionType == 0) continue;

                AttachJobPassive(state, i, spec.PositionType, spec.JobPassiveParam0, spec.JobPassiveParam1,
                    world.TickRate);
            }
        }

        private static void AttachJobPassive(CombatMatchState state, int unitIndex,
            byte positionType, int param0, int param1, int tickRate)
        {
            switch (positionType)
            {
                case PosSharpshooter:
                    // param0 = 확률 (정수 퍼센트)
                    if (param0 > 0)
                        TraitSystem.AddTrait(state, unitIndex, new SharpshooterPierceTrait(param0));
                    break;

                case PosGhost:
                    // 백라인 점프 (시너지와 무관하게 고유)
                    state.Units[unitIndex].HasBacklineJump = true;
                    // param0 = N타마다 확정 크리 (정수, ×100 되어있으므로 /100)
                    int maxStack = param0 / 100;
                    if (maxStack > 0)
                        TraitSystem.AddTrait(state, unitIndex, new GhostCritStackTrait(maxStack));
                    break;

                case PosStriker:
                    // param0 = 쿨타임 (초 × 100 → 프레임 변환)
                    int cooldownFrames = param0 * tickRate / 100;
                    if (cooldownFrames > 0)
                        TraitSystem.AddTrait(state, unitIndex, new StrikerCCImmuneTrait(cooldownFrames));
                    break;

                case PosGuardian:
                    // param0 = 쿨타임 (초 × 100 → 프레임 변환), param1 = 충전 횟수 (×100이므로 /100, 기본 3)
                    int guardCooldown = param0 * tickRate / 100;
                    int charges = param1 > 0 ? param1 / 100 : 3;
                    if (guardCooldown > 0)
                        TraitSystem.AddTrait(state, unitIndex, new GuardianEndureTrait(guardCooldown, charges));
                    break;

                case PosEsper:
                    // param0 = 확률 (정수 퍼센트), param1 = 데미지 퍼센트
                    int dmgPercent = param1 > 0 ? param1 : 100;
                    if (param0 > 0)
                        TraitSystem.AddTrait(state, unitIndex, new EsperExplosionTrait(param0, dmgPercent));
                    break;
            }
        }

        private static ChampionSpec FindSpec(GameWorld world, int championSpecId)
        {
            for (int i = 0; i < world.Pool.SpecCount; i++)
            {
                if (world.Pool.Specs[i].ChampionId == championSpecId)
                    return world.Pool.Specs[i];
            }
            return default;
        }

        /// <summary>
        /// Esper 폭발 처리: 타겟 위치 기준 반경 1 (체비셰프 = 3×3) 범위 적에게 데미지.
        /// </summary>
        public static void ProcessEsperExplosion(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target, int damagePercent)
        {
            int attackerIndex = state.FindUnitIndex(attacker.CombatId);
            int explosionDamage = attacker.Attack * damagePercent / 100;
            if (explosionDamage < DamageSystem.MinDamage) explosionDamage = DamageSystem.MinDamage;

            // VFX 이벤트: 타일 하이라이트 + Esper 폭발 프리팹
            state.EventQueue?.PushSkillAreaEffect(
                attacker.CombatId, target.GridCol, target.GridRow, 1, isBox: true,
                areaVfxType: CombatVfxType.JobEsper);

            SkillAreaHelper.ForEachEnemyInRadius(state, attacker.TeamIndex,
                target.GridCol, target.GridRow, 1,
                (ref CombatUnit aoeTarget, int aoeTargetIndex) =>
                {
                    ref var atk = ref state.Units[attackerIndex];
                    int finalDamage = DamageSystem.CalculateDamage(
                        explosionDamage, DamageType.Magical, ref atk, ref aoeTarget);
                    DamageSystem.ApplyDamage(state, ref aoeTarget, finalDamage, attackerIndex, DamageType.Magical);
                });
        }
    }
}
