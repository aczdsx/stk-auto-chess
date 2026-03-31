using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 직업군 패시브 초기화 + Esper 폭발 처리 static 클래스.
    /// SOA 배열(CombatMatchState)에 직업별 패시브 데이터를 직접 설정.
    /// </summary>
    public static class JobPassiveSystem
    {
        /// <summary>
        /// 매치 내 모든 유닛에 직업 패시브 SOA 데이터 설정.
        /// CombatSetupSystem.SetupMatch / SetupPvEMatch 직후에 호출.
        /// </summary>
        public static void SetupJobPassives(CombatMatchState state, GameWorld world)
        {
            if (world.Pool == null) return;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive) continue;

                var spec = FindSpec(world, unit.ChampionSpecId);
                if (spec.PositionType == 0) continue;

                var posType = (CharacterPositionType)spec.PositionType;
                GetJobPassiveParams(posType, out int param0, out int param1);
                AttachJobPassive(state, i, posType, param0, param1, world.TickRate);
            }
        }

        /// <summary>SpecDataManager에서 직업 패시브 파라미터 직접 조회</summary>
        private static void GetJobPassiveParams(CharacterPositionType posType, out int param0, out int param1)
        {
            param0 = 0;
            param1 = 0;

            var specMgr = SpecDataManager.Instance;
            if (specMgr == null) return;

            var passiveList = specMgr.GetJobPassiveList(posType);
            if (passiveList == null || passiveList.Count == 0) return;
            if (passiveList[0] == null || passiveList[0].Count == 0) return;

            var data = passiveList[0][0]; // grade 0
            param0 = (int)(data.passive_rate * 100);
            param1 = (int)(data.passive_rate_2 * 100);
        }

        private static void AttachJobPassive(CombatMatchState state, int unitIndex,
            CharacterPositionType positionType, int param0, int param1, int tickRate)
        {
            switch (positionType)
            {
                case CharacterPositionType.SHARPSHOOTER:
                    // param0 = 확률 (정수 퍼센트)
                    if (param0 > 0)
                    {
                        state.SharpshooterPassives[unitIndex] = new SharpshooterPassive
                        {
                            Active = true,
                            ChancePercent = param0,
                        };
                    }
                    break;

                case CharacterPositionType.GHOST:
                    // 백라인 점프 (시너지와 무관하게 고유)
                    state.Units[unitIndex].HasBacklineJump = true;
                    // param0 = N타마다 확정 크리 (정수, ×100 되어있으므로 /100)
                    int maxStack = param0 / 100;
                    if (maxStack > 0)
                    {
                        state.GhostPassives[unitIndex] = new GhostPassive
                        {
                            Active = true,
                            MaxStack = maxStack,
                        };
                    }
                    break;

                case CharacterPositionType.STRIKER:
                {
                    // param0 = 쿨타임 (초 × 100 → 프레임 변환)
                    int cooldownFrames = param0 * tickRate / 100;
                    if (cooldownFrames > 0)
                    {
                        state.StrikerPassives[unitIndex] = new StrikerPassive
                        {
                            Active = true,
                            CooldownFrames = cooldownFrames,
                        };
                    }
                    break;
                }

                case CharacterPositionType.GUARDIAN:
                {
                    // param0 = 쿨타임 (초 × 100 → 프레임 변환), param1 = 충전 횟수 (×100이므로 /100, 기본 3)
                    int guardCooldown = param0 * tickRate / 100;
                    int charges = param1 > 0 ? param1 / 100 : 3;
                    if (guardCooldown > 0)
                    {
                        state.GuardianPassives[unitIndex] = new GuardianPassive
                        {
                            Active = true,
                            CooldownFrames = guardCooldown,
                            MaxCharges = charges,
                        };
                    }
                    break;
                }

                case CharacterPositionType.ORACLE:
                    // param0 = 회복 비율 (정수 퍼센트)
                    state.Units[unitIndex].IsHealer = true;
                    if (param0 > 0)
                    {
                        state.OraclePassives[unitIndex] = new OraclePassive
                        {
                            Active = true,
                            HealPercent = param0,
                        };
                    }
                    break;

                case CharacterPositionType.ESPER:
                {
                    // param0 = 확률 (정수 퍼센트), param1 = 데미지 퍼센트
                    int dmgPercent = param1 > 0 ? param1 : 100;
                    if (param0 > 0)
                    {
                        state.EsperPassives[unitIndex] = new EsperPassive
                        {
                            Active = true,
                            ChancePercent = param0,
                            DamagePercent = dmgPercent,
                        };
                    }
                    break;
                }
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
