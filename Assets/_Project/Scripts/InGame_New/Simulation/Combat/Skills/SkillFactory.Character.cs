using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;
using static CookApps.AutoChess.SkillFactory.ValueRef;
using E = CookApps.AutoChess.SkillExecutionType;
using T = CookApps.AutoChess.SkillTargetType;
using F = CookApps.AutoChess.SkillTargetFilter;
using S = CookApps.AutoChess.SkillAreaShape;
using V = CookApps.AutoChess.SkillVfxPlacement;
using P = CookApps.AutoChess.ParamValueType;
using Evt = CookApps.AutoChess.SkillEvent;

namespace CookApps.AutoChess
{
    public static partial class SkillFactory
    {
        /// <summary>엘리스: 얼음 VFX 스폰 후 데미지까지 딜레이.</summary>
        private const short EllisDamageDelayMs = 800;

        /// <summary>플레이어 스킬 Recipe 등록</summary>
        private static void RegisterPlayerRecipes()
        {
            // ── 필리아: Damage + 3단계 VFX + 마커 ──
            Skill(215532401, E.DelayedApply, T.FarthestEnemy)
                .On(Evt.Cast)
                    .Do(Vfx(0, V.AtGridPos))
                .On(Evt.Execute1)
                    .Do(AddMarker(SkillMarkerType.PiliaSkillCast))
                    .Do(Damage(power: Spec(1, 200f)))
                    .Do(Vfx(1, V.AtCasterWithDir))
                    .Do(Vfx(2, V.AtTarget))
                .Register();

            // ── 하티: Damage + Knockback + 3단계 VFX ──
            Skill(217433303, E.DelayedApply, T.FarthestEnemy)
                .On(Evt.Cast)
                    .Do(Vfx(0, V.AtCaster))
                    .Do(Vfx(1, V.AtTarget))
                .On(Evt.Execute1)
                    .Do(Damage(power: Spec(1, 200f)))
                    .Do(Vfx(2, V.AtCasterWithDir))
                    .Do(Knockback(Spec(2, 2f)))
                .Register();

            // ── 유니: 최저HP 3명 Heal + RemoveDebuffs ──
            Skill(215252102, E.DelayedApply, T.LowestHPAlly)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtCaster))
                    .Do(Heal(power: Spec(1, 200f), filter: F.LowestHpAllies, range: 3))
                    .Do(RemoveDebuffs(F.LowestHpAllies, range: 3))
                .Register();

            // ── 멘샤: 같은 행 아군 Shield ──
            Skill(215422301, E.DelayedApply, T.Self)
                .On(Evt.Execute1)
                    .Do(Shield(percent: Spec(1, 20f), duration: Spec(2, P.Frames, 3f), filter: F.SameRowAllies))
                .Register();

            // ── 미사: CC(Stun) + Marker ──
            Skill(217323201, E.DelayedApply, T.HighestAttackEnemy)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtTarget))
                    .Do(Vfx(1, V.AtTarget))
                    .Do(CC(CrowdControlType.Stun, duration: Spec(2, P.Frames, 3f)))
                    .Do(AddMarker(SkillMarkerType.MisaRestraint))
                .Register();

            // ── 클레이: Zone 힐+데미지+디버프 ──
            Skill(217553404, E.Channeling, T.Self)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtCaster))
                .On(Evt.Tick)
                    .Do(WithRepeat(Heal(power: Spec(1, 200f), filter: F.AlliesInArea, area: S.Diamond, range: 2),
                        intervalFrames: 15, dynamicFromClip: true))
                    .Do(RemoveDebuffs(F.AlliesInArea, S.Diamond, 2))
                    .Do(Damage(power: Spec(2, 80f), filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                    .Do(Debuff(StatusEffectType.HealReduction, value: Spec(3, 50f), duration: Spec(4, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                    .Do(AreaVfx(V.AreaEffect, 2, condition: SkillActionCondition.EveryNth2))
                .Register();

            // ── 엘리스: 2단계 Diamond AoE ──
            Skill(215642501, E.Channeling, T.NearestEnemy)
                .On(Evt.Execute1)
                    .Do(AreaVfx(V.AreaEffect, 1))
                    .Do(AreaVfx(V.PerTileInDiamond, 1, vfxIndex: 0))
                .On(Evt.Tick)
                    .Do(WithRepeat(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Diamond, range: 1),
                        count: 1, intervalMs: EllisDamageDelayMs))
                .Register();

            // ── 아트레시아: 3칸 폭 직선 관통 투사체 ──
            Skill(217513401, E.DelayedApply, T.NearestEnemy).Projectile()
                .On(Evt.Execute1)
                    .Do(SpawnLinearProjectile(power: Spec(1, 200f), width: 3))
                .Register();

            // ── 메이: Plus AoE + 넉백 + 방어 버프 ──
            Skill(215322201, E.DelayedApply, T.Self)
                .On(Evt.Cast)
                    .Do(Vfx(0, V.AtCaster))
                .On(Evt.Execute1)
                    .Do(DamageKnockback(S.Plus, 1, power: Spec(1, 200f)))
                    .Do(Buff(StatModType.Def, value: Spec(3, 50f), duration: Spec(2, P.Frames, 4f)))
                .Register();

            // ── SingleProjectile (230101005, 230202004): Homing 투사체 ──
            Skill(230101005, E.DelayedApply, T.NearestEnemy).Projectile()
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(power: Spec(1, 200f), vfxIndex: 1))
                .Register();

            Skill(230202004, E.DelayedApply, T.NearestEnemy).Projectile()
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(power: Spec(1, 200f), vfxIndex: 1))
                .Register();

            // ── 시이나: Damage + CC(Silence) ──
            Skill(215362202, E.Instant, T.NearestEnemy)
                .On(Evt.Cast)
                    .Do(Damage(power: Spec(1, 200f)))
                    .Do(CC(CrowdControlType.Silence, duration: Spec(2, P.Frames, 3f)))
                .Register();

            // ── 블린: Diamond AoE (범위 2) ──
            Skill(217243102, E.DelayedApply, T.NearestEnemy)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtGridPos))
                    .Do(AreaVfx(V.AreaEffect, 2))
                    .Do(AreaVfx(V.PerTileInDiamond, 2, vfxIndex: 1))
                    .Do(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                .Register();

            // ── 아란: 단일 대상 Heal ──
            Skill(1406031, E.Instant, T.LowestHPAlly)
                .On(Evt.Cast)
                    .Do(Heal(power: Spec(1, 200f)))
                .Register();

            // ══════════════════════════════
            // Recipe 전환 스킬 (Custom 클래스 삭제됨)
            // ══════════════════════════════

            // ── 테토라: Damage + 넉백 4칸 + 벽 충돌 시 착지 AoE ──
            Skill(217413301, E.DelayedApply, T.NearestEnemy)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtCasterWithDir))
                    .Do(Damage(power: Spec(1, 200f)))
                    .Do(Knockback(fixedDistance: 4))
                .On(Evt.KnockbackWall)
                    .Do(Vfx(1, V.AtGridPos))
                    .Do(Damage(power: Spec(3, 200f), filter: F.EnemiesInArea, area: S.Circle, range: 1))
                .WithTags(TraitTag.Damage | TraitTag.Knockback | TraitTag.CC | TraitTag.AoE)
                .Register();

            // ── 루키다: 여우불 마커 + 공속 버프 (Custom 유지 — ParamSlots만) ──
            Skill(217263103, E.Instant, T.Self)
                .WithTags(TraitTag.Buff)
                .Register();

            // ── 라키유: 베지어 투사체 → 3×3 범위 디버프 ──
            Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(vfxIndex: 0, travelFrames: 15, useBezier: true, arrivalVfxIndex: 1))
                .On(Evt.ProjectileArrive)
                    .Do(TileEffect(S.Circle, range: 1, at: F.PrimaryTarget, isBox: true))
                    .Do(Debuff(StatModType.AdReduce, value: Spec(3, 30f), duration: Spec(1, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(Debuff(StatModType.ApReduce, value: Spec(3, 30f), duration: Spec(1, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(Debuff(StatusEffectType.HealReduction, value: Spec(2, 50f), duration: Spec(1, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Circle, range: 1))
                .WithTags(TraitTag.Projectile | TraitTag.Debuff | TraitTag.AoE)
                .Register();

            // ── 미노: 최저HP 3명 순차 미사일 + Plus 스플래시 ──
            Skill(217433302, E.Channeling, T.LowestHPEnemy).Projectile()
                .On(Evt.Tick)
                    .Do(Retarget(F.LowestHpAllies, excludeHit: true))
                    .Do(WithRepeat(
                        SpawnProjectile(power: Spec(1, 200f), vfxIndex: 0, travelFrames: 15, useBezier: true, arrivalVfxIndex: 1),
                        count: 3, intervalMs: 300))
                .On(Evt.ProjectileArrive)
                    .Do(Damage(power: Spec(1, 200f)))
                    .Do(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Plus, range: 1, excludePrimary: true))
                .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.MultiHit)
                .Register();

            // ── 베인: 바운스 투사체 + 감쇠 + 공속 버프(히트당) ──
            Skill(217363204, E.Channeling, T.NearestEnemy).Projectile()
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(power: Spec(1, 200f), vfxIndex: 0, travelFrames: 9, arrivalVfxIndex: 1))
                .On(Evt.ProjectileArrive)
                    .Do(DamageWithDecay(power: Spec(1, 200f), decayPercent: Spec(2, 20f)))
                    .Do(Retarget(F.NearestEnemy, excludeHit: true))
                    .Do(SpawnProjectile(power: Spec(1, 200f), vfxIndex: 0, travelFrames: 9, arrivalVfxIndex: 1))
                .On(Evt.Complete)
                    .Do(Buff(StatModType.AttackSpeed,
                        value: Spec(4, 30f), duration: Spec(3, P.Frames, 3f), scaleByHitCount: true))
                .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.Buff)
                .Register();

            // ── 마리에: 텔레포트 + 다단히트 + 디버프 ──
            Skill(217563405, E.Channeling, T.HighestAttackEnemy)
                .On(Evt.Execute1)
                    .Do(Teleport())
                    .Do(Vfx(0, V.AtTarget))
                .On(Evt.Tick)
                    .Do(WithRepeat(Damage(power: Spec(2, 200f)), dynamicFromClip: true))
                    .Do(Debuff(StatModType.Attack, value: Spec(4, 30f), duration: Spec(3, P.Frames, 3f), filter: F.PrimaryTarget))
                    .Do(Debuff(StatModType.Def, value: Spec(4, 30f), duration: Spec(3, P.Frames, 3f), filter: F.PrimaryTarget))
                .On(Evt.Complete)
                    .Do(AddMarker(SkillMarkerType.MarieAracne))
                .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.MultiHit)
                .Register();

            // ── 에이프릴: 확장 콘 바라지 (Custom 유지 — ParamSlots만) ──
            Skill(217333202, E.Channeling, T.NearestEnemy)
                .WithTags(TraitTag.Damage | TraitTag.AoE)
                .Register();

            // ── 엔키: 보드 스윕 힐 투사체 (Custom 유지 — ParamSlots만) ──
            Skill(217653505, E.Channeling, T.Self)
                .WithTags(TraitTag.Heal)
                .Register();

            // ── 오데트: 2단계 — Execute1 Rect AoE + Execute2 텔레포트 3×3 ──
            Skill(217613501, E.Channeling, T.NearestEnemy)
                .On(Evt.Execute1)
                    .Do(TileEffect(S.Rect))
                    .Do(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Rect, range: 1, rectDepth: 1))
                    .Do(Debuff(StatModType.AttackSpeed, value: Spec(3, 30f), duration: Spec(2, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Rect, range: 1))
                    .Do(AddMarker(SkillMarkerType.OdetteCold))
                    .Do(Vfx(0, V.AtCasterWithDir))
                .On(Evt.Execute2)
                    .Do(Teleport(distance: 2))
                    .Do(TileEffect(S.Circle, range: 1, isBox: true))
                    .Do(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(Debuff(StatModType.AttackSpeed, value: Spec(3, 30f), duration: Spec(2, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(AddMarker(SkillMarkerType.OdetteCold))
                    .Do(Vfx(1, V.AtCaster))
                .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.AoE)
                .Register();

            // ── 아드리아: 3단계 확장 패턴 (Custom 유지 — ParamSlots만) ──
            Skill(217523403, E.Channeling, T.Self)
                .WithTags(TraitTag.Damage | TraitTag.AoE)
                .Register();

            // ── 시라유키: 순차 텔레포트 암살 + 지정불가 + 회피버프 ──
            Skill(217663506, E.Channeling, T.LowestHPEnemy)
                .On(Evt.Cast)
                    .Do(ApplyStatusEffect(StatusEffectType.TargetImpossible, F.Self,
                        duration: Spec(1, P.Frames, 3f)))
                    .Do(Vfx(0, V.AtCaster))
                .On(Evt.Execute1)
                    .Do(Teleport())
                    .Do(Vfx(1, V.AtTarget))
                    .Do(Damage(power: Spec(2, 200f)))
                    .Do(Retarget(F.LowestHpAllies, excludeHit: true))
                .On(Evt.Execute2)
                    .Do(Teleport())
                    .Do(Vfx(1, V.AtTarget))
                    .Do(Damage(power: Spec(2, 200f)))
                    .Do(Retarget(F.LowestHpAllies, excludeHit: true))
                .On(Evt.Execute3)
                    .Do(Teleport())
                    .Do(Vfx(1, V.AtTarget))
                    .Do(Damage(power: Spec(2, 200f)))
                .On(Evt.Complete)
                    .Do(Buff(StatModType.DodgeChance,
                        value: Spec(4, 30f), duration: Spec(3, P.Frames, 3f)))
                        // value: Fixed(100f), duration: Spec(3, P.Frames, 3f)))
                .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Buff)
                .Register();
        }
    }
}
