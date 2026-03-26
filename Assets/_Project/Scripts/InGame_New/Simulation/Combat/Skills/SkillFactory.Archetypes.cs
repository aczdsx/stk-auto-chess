using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;
using E = CookApps.AutoChess.SkillExecutionType;
using T = CookApps.AutoChess.SkillTargetType;
using F = CookApps.AutoChess.SkillTargetFilter;
using S = CookApps.AutoChess.SkillAreaShape;

namespace CookApps.AutoChess
{
    public static partial class SkillFactory
    {
        /// <summary>아키타입 Recipe 등록. 기존 아키타입 클래스를 대체.</summary>
        private static void RegisterArchetypeRecipes()
        {
            DefineArchetype(SimSkillArchetype.SingleDamage,
                ArchetypeBuilder(E.Instant, T.NearestEnemy)
                    .OnCast(Damage())
                    .Build());

            DefineArchetype(SimSkillArchetype.AoEDamage,
                ArchetypeBuilder(E.Instant, T.BestAoETarget)
                    .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Build());

            DefineArchetype(SimSkillArchetype.MultiHit,
                ArchetypeBuilder(E.Instant, T.NearestEnemy)
                    .OnCast(MultiHit())
                    .Build());

            DefineArchetype(SimSkillArchetype.Heal,
                ArchetypeBuilder(E.Instant, T.LowestHPAlly)
                    .OnCast(Heal())
                    .Build());

            DefineArchetype(SimSkillArchetype.MultiTargetHeal,
                ArchetypeBuilder(E.Instant, T.LowestHPAlly)
                    .OnCast(Heal(filter: F.LowestHpAllies, range: 3))
                    .Build());

            DefineArchetype(SimSkillArchetype.DamageCC,
                ArchetypeBuilder(E.Instant, T.NearestEnemy)
                    .OnCast(Damage())
                    .OnCast(CC(CrowdControlType.Stun))
                    .Build());

            DefineArchetype(SimSkillArchetype.LineDamage,
                ArchetypeBuilder(E.DelayedApply, T.NearestEnemy).Projectile()
                    .AtHit(SpawnLinearProjectile())
                    .Build());

            DefineArchetype(SimSkillArchetype.ConeDamage,
                ArchetypeBuilder(E.Instant, T.NearestEnemy)
                    .OnCast(Damage(filter: F.EnemiesInArea, area: S.Line, range: 2))
                    .Build());

            DefineArchetype(SimSkillArchetype.DiamondAoE,
                ArchetypeBuilder(E.DelayedApply, T.NearestEnemy)
                    .AtHit(Vfx(0, SkillVfxPlacement.AtGridPos))
                    .AtHit(AreaVfx(SkillVfxPlacement.AreaEffect, 2))
                    .AtHit(AreaVfx(SkillVfxPlacement.PerTileInDiamond, 2, vfxIndex: 1))
                    .AtHit(Damage(filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                    .Build());

            DefineArchetype(SimSkillArchetype.PatternDamage,
                ArchetypeBuilder(E.Instant, T.BestAoETarget)
                    .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .OnCast(AreaCC(CrowdControlType.Stun, S.Circle, 1))
                    .Build());

            DefineArchetype(SimSkillArchetype.TeleportStrike,
                ArchetypeBuilder(E.Instant, T.NearestEnemy)
                    .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .OnCast(AreaCC(CrowdControlType.Stun, S.Circle, 1))
                    .Build());

            DefineArchetype(SimSkillArchetype.Buff,
                ArchetypeBuilder(E.Instant, T.Self)
                    .OnCast(ModifyStat())
                    .Build());

            DefineArchetype(SimSkillArchetype.Debuff,
                ArchetypeBuilder(E.Instant, T.NearestEnemy)
                    .OnCast(ModifyStat(F.PrimaryTarget))
                    .Build());
        }
    }
}
