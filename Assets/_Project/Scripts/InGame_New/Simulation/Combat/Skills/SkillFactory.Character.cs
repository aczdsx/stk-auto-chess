using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;
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
        // ── 스킬별 타이밍 상수 (ms) ──

        /// <summary>엘리스: 얼음 VFX 스폰 후 데미지까지 딜레이. 얼음이 깨지는 연출 대기.</summary>
        private const short EllisDamageDelayMs = 800;


        /// <summary>플레이어 스킬 Recipe 등록</summary>
        private static void RegisterPlayerRecipes()
        {
            // ══════════════════════════════
            // SimSkillGeneric으로 완전 대체
            // ══════════════════════════════

            // ── 필리아: Damage + 3단계 VFX + 마커 ──
            Skill(215532401, E.DelayedApply, T.FarthestEnemy)
                .Param(1, P.Int, 200f)
                .On(Evt.Cast)
                    .Do(Vfx(0, V.AtGridPos))
                .On(Evt.Execute1)
                    .Do(AddMarker(SkillMarkerType.PiliaSkillCast))
                    .Do(Damage(paramIndex: 0))
                    .Do(Vfx(1, V.AtCasterWithDir))
                    .Do(Vfx(2, V.AtTarget))
                .Register();

            // ── 하티: Damage + Knockback + 3단계 VFX ──
            Skill(217433303, E.DelayedApply, T.FarthestEnemy)
                .Param(1, P.Int, 200f)          // [0] 데미지 배율
                .Param(2, P.Int, 2f)             // [1] 넉백 거리
                .On(Evt.Cast)
                    .Do(Vfx(0, V.AtCaster))
                    .Do(Vfx(1, V.AtTarget))
                .On(Evt.Execute1)
                    .Do(Damage(paramIndex: 0))
                    .Do(Vfx(2, V.AtCasterWithDir))
                    .Do(Knockback(distParamIndex: 1))
                .Register();

            // ── 유니: 최저HP 3명 Heal + RemoveDebuffs ──
            Skill(215252102, E.DelayedApply, T.LowestHPAlly)
                .Param(1, P.Int, 200f)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtCaster))
                    .Do(Heal(paramIndex: 0, filter: F.LowestHpAllies, range: 3))
                    .Do(RemoveDebuffs(F.LowestHpAllies, range: 3))
                .Register();

            // ── 멘샤: 같은 행 아군 Shield ──
            Skill(215422301, E.DelayedApply, T.Self)
                .Param(1, P.Int, 20f)            // [0] 실드 비율 (maxHP%)
                .Param(2, P.Frames, 3f)          // [1] 실드 지속
                .On(Evt.Execute1)
                    .Do(Shield(0, 1, F.SameRowAllies))
                .Register();

            // ── 미사: CC(Stun) + Marker ──
            Skill(217323201, E.DelayedApply, T.HighestAttackEnemy)
                .Param(1, P.Int, 200f)           // [0] (미사용)
                .Param(2, P.Frames, 3f)          // [1] 스턴 지속
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtTarget))
                    .Do(Vfx(1, V.AtTarget))
                    .Do(CC(CrowdControlType.Stun, durationParamIndex: 1))
                    .Do(AddMarker(SkillMarkerType.MisaRestraint))
                .Register();

            // ── 클레이: Zone 힐+데미지+디버프 ──
            Skill(217553404, E.Channeling, T.Self)
                .Param(1, P.Int, 200f)           // [0] 힐 배율
                .Param(2, P.Int, 80f)            // [1] 데미지 배율
                .Param(3, P.Int, 50f)            // [2] 회복감소%
                .Param(4, P.Frames, 3f)          // [3] 디버프 지속
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtCaster))
                .On(Evt.Tick)
                    .Do(WithRepeat(Heal(paramIndex: 0, filter: F.AlliesInArea, area: S.Diamond, range: 2),
                        intervalFrames: 15, dynamicFromClip: true))
                    .Do(RemoveDebuffs(F.AlliesInArea, S.Diamond, 2))
                    .Do(Damage(paramIndex: 1, filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                    .Do(Debuff(StatusEffectType.HealReduction, 2, 3, F.EnemiesInArea, S.Diamond, 2))
                    .Do(AreaVfx(V.AreaEffect, 2, condition: SkillActionCondition.EveryNth2))
                .Register();

            // ── 엘리스: 2단계 Diamond AoE ──
            Skill(215642501, E.Channeling, T.NearestEnemy)
                .Param(1, P.Int, 200f)
                .On(Evt.Execute1)
                    .Do(AreaVfx(V.AreaEffect, 1))
                    .Do(AreaVfx(V.PerTileInDiamond, 1, vfxIndex: 0))
                .On(Evt.Tick)
                    .Do(WithRepeat(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Diamond, range: 1),
                        count: 1, intervalMs: EllisDamageDelayMs))
                .Register();

            // ── 아트레시아: 3칸 폭 직선 관통 투사체 ──
            Skill(217513401, E.DelayedApply, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .On(Evt.Execute1)
                    .Do(SpawnLinearProjectile(paramIndex: 0, width: 3))
                .Register();

            // ── 메이: Plus AoE + 넉백 + 방어 버프 ──
            Skill(215322201, E.DelayedApply, T.Self)
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .Param(2, P.Frames, 4f)          // [1] 버프 지속
                .Param(3, P.Int, 50f)            // [2] 방어력 버프%
                .On(Evt.Cast)
                    .Do(Vfx(0, V.AtCaster))
                .On(Evt.Execute1)
                    .Do(DamageKnockback(S.Plus, 1, paramIndex: 0))
                    .Do(Buff(StatModType.Def, 2, 1))
                .Register();

            // ── SingleProjectile (230101005, 230202004): Homing 투사체 ──
            Skill(230101005, E.DelayedApply, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(paramIndex: 0, vfxIndex: 1))
                .Register();

            Skill(230202004, E.DelayedApply, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(paramIndex: 0, vfxIndex: 1))
                .Register();

            // ══════════════════════════════
            // 아키타입 의존 스킬 → 개별 Recipe 전환
            // ══════════════════════════════

            // ── 시이나: Damage + CC(Silence) ──
            Skill(215362202, E.Instant, T.NearestEnemy)
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .Param(2, P.Frames, 3f)          // [1] 침묵 지속
                .On(Evt.Cast)
                    .Do(Damage(paramIndex: 0))
                    .Do(CC(CrowdControlType.Silence, durationParamIndex: 1))
                .Register();

            // ── 블린: Diamond AoE (범위 2) ──
            Skill(217243102, E.DelayedApply, T.NearestEnemy)
                .Param(1, P.Int, 200f)
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtGridPos))
                    .Do(AreaVfx(V.AreaEffect, 2))
                    .Do(AreaVfx(V.PerTileInDiamond, 2, vfxIndex: 1))
                    .Do(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                .Register();

            // ── 아란: 단일 대상 Heal ──
            Skill(1406031, E.Instant, T.LowestHPAlly)
                .Param(1, P.Int, 200f)
                .On(Evt.Cast)
                    .Do(Heal(paramIndex: 0))
                .Register();

            // ══════════════════════════════
            // 커스텀 클래스 유지 — ParamSlots만 Recipe에서 관리
            // ══════════════════════════════

            // ── 테토라: Damage + 넉백 4칸 + 벽 충돌 시 착지 AoE ──
            Skill(217413301, E.DelayedApply, T.NearestEnemy)
                .Param(1, P.Int, 200f)           // [0] 주 데미지
                .Param(3, P.Int, 200f)           // [1] 착지 AoE 데미지
                .On(Evt.Execute1)
                    .Do(Vfx(0, V.AtCasterWithDir))
                    .Do(Damage(paramIndex: 0))
                    .Do(Knockback(fixedDistance: 4))
                .On(Evt.KnockbackWall)
                    .Do(Vfx(1, V.AtGridPos))
                    .Do(Damage(paramIndex: 1, filter: F.EnemiesInArea, area: S.Circle, range: 1))
                .WithTags(TraitTag.Damage | TraitTag.Knockback | TraitTag.CC | TraitTag.AoE)
                .Register();

            // ── 루키다: 여우불 마커 + 공속 버프 ──
            Skill(217263103, E.Instant, T.Self)
                .Param(1, P.Int, 2f)             // [0] 여우불 증가량
                .Param(2, P.Frames, 3f)          // [1] 버프 지속
                .Param(3, P.Int, 10f)            // [2] 공속 증가율%
                .WithTags(TraitTag.Buff)
                .Register();

            // ── 라키유: 베지어 투사체 → 3×3 범위 디버프 ──
            Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
                .Param(1, P.Frames, 3f)          // [0] 디버프 지속
                .Param(2, P.Int, 50f)            // [1] 회복감소%
                .Param(3, P.Int, 30f)            // [2] 방어감소%
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(paramIndex: -1, vfxIndex: 0, travelFrames: 15, useBezier: true, arrivalVfxIndex: 1))
                .On(Evt.ProjectileArrive)
                    .Do(TileEffect(S.Circle, range: 1, at: F.PrimaryTarget, isBox: true))
                    .Do(Debuff(StatModType.AdReduce, 2, 0, F.EnemiesInArea, S.Circle, 1))
                    .Do(Debuff(StatModType.ApReduce, 2, 0, F.EnemiesInArea, S.Circle, 1))
                    .Do(Debuff(StatusEffectType.HealReduction, 1, 0, F.EnemiesInArea, S.Circle, 1))
                .WithTags(TraitTag.Projectile | TraitTag.Debuff | TraitTag.AoE)
                .Register();

            // ── 미노: 최저HP 3명 순차 미사일 + Plus 스플래시 ──
            Skill(217433302, E.Channeling, T.LowestHPEnemy).Projectile()
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .On(Evt.Tick)
                    .Do(Retarget(F.LowestHpAllies, excludeHit: true))
                    .Do(WithRepeat(
                        SpawnProjectile(paramIndex: 0, vfxIndex: 0, travelFrames: 15, useBezier: true, arrivalVfxIndex: 1),
                        count: 3, intervalMs: 300))
                .On(Evt.ProjectileArrive)
                    .Do(Damage(paramIndex: 0))
                    .Do(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Plus, range: 1, excludePrimary: true))
                .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.MultiHit)
                .Register();

            // ── 베인: 바운스 투사체 + 감쇠 + 공속 버프(히트당) ──
            Skill(217363204, E.Channeling, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .Param(2, P.Int, 20f)            // [1] 감쇠율%
                .Param(3, P.Frames, 3f)          // [2] 공속 버프 지속
                .Param(4, P.Int, 30f)            // [3] 공속 증가율% (히트당)
                .Param(5, P.Int, 5f)             // [4] 최대 바운스
                .On(Evt.Execute1)
                    .Do(SpawnProjectile(paramIndex: 0, vfxIndex: 0, travelFrames: 9, arrivalVfxIndex: 1))
                .On(Evt.ProjectileArrive)
                    .Do(DamageWithDecay(paramIndex: 0, decayParamIndex: 1))
                    .Do(Retarget(F.NearestEnemy, excludeHit: true))
                    .Do(SpawnProjectile(paramIndex: 0, vfxIndex: 0, travelFrames: 9, arrivalVfxIndex: 1))
                .On(Evt.Complete)
                    .Do(BuffScaled(StatModType.AttackSpeed, 3, 2, scaleByHitCount: true))
                .WithTags(TraitTag.Damage | TraitTag.Projectile | TraitTag.Buff)
                .Register();

            // ── 마리에: 텔레포트 + 다단히트 + 디버프 ──
            Skill(217563405, E.Channeling, T.HighestAttackEnemy)
                .Param(2, P.Int, 200f)           // [0] 데미지 배율
                .Param(1, P.Int, 4f)             // [1] 히트수
                .Param(3, P.Frames, 3f)          // [2] 디버프 지속
                .Param(4, P.Int, 30f)            // [3] 디버프%
                .On(Evt.Execute1)
                    .Do(Teleport())
                    .Do(Vfx(0, V.AtTarget))
                .On(Evt.Tick)
                    .Do(WithRepeat(Damage(paramIndex: 0), dynamicFromClip: true))
                    .Do(Debuff(StatModType.Attack, 3, 2, F.PrimaryTarget))
                    .Do(Debuff(StatModType.Def, 3, 2, F.PrimaryTarget))
                .On(Evt.Complete)
                    .Do(AddMarker(SkillMarkerType.MarieAracne))
                .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.MultiHit)
                .Register();

            // ── 에이프릴: 확장 콘 바라지 ──
            Skill(217333202, E.Channeling, T.NearestEnemy)
                .Param(2, P.Int, 100f)           // [0] 근거리 배율
                .Param(1, P.Int, 10f)            // [1] 히트 회수
                .Param(3, P.Int, 75f)            // [2] 중거리 배율
                .Param(4, P.Int, 50f)            // [3] 원거리 배율
                .WithTags(TraitTag.Damage | TraitTag.AoE)
                .Register();

            // ── 엔키: 보드 스윕 힐 투사체 ──
            Skill(217653505, E.Channeling, T.Self)
                .Param(1, P.Int, 200f)           // [0] 힐 배율
                .Param(2, P.Frames, 6f)          // [1] HoT 지속
                .Param(3, P.Int, 50f)            // [2] HoT 위력%
                .WithTags(TraitTag.Heal)
                .Register();

            // ── 오데트: 2단계 — Execute1 Rect AoE + Execute2 텔레포트 3×3 ──
            Skill(217613501, E.Channeling, T.NearestEnemy)
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .Param(2, P.Frames, 3f)          // [1] 디버프 지속
                .Param(3, P.Int, 30f)            // [2] 공속감소%
                .On(Evt.Execute1)
                    .Do(TileEffect(S.Rect))
                    .Do(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Rect, range: 1, rectDepth: 1))
                    .Do(Debuff(StatModType.AttackSpeed, 2, 1, F.EnemiesInArea, S.Rect, 1))
                    .Do(AddMarker(SkillMarkerType.OdetteCold))
                    .Do(Vfx(0, V.AtCasterWithDir))
                .On(Evt.Execute2)
                    .Do(Teleport(distance: 2))
                    .Do(TileEffect(S.Circle, range: 1, isBox: true))
                    .Do(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(Debuff(StatModType.AttackSpeed, 2, 1, F.EnemiesInArea, S.Circle, 1))
                    .Do(AddMarker(SkillMarkerType.OdetteCold))
                    .Do(Vfx(1, V.AtCaster))
                .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Debuff | TraitTag.AoE)
                .Register();

            // ── 아드리아: 3단계 확장 패턴 ──
            Skill(217523403, E.Channeling, T.Self)
                .Param(1, P.Int, 200f)
                .Param(2, P.Int, 100f)
                .Param(3, P.Frames, 2f)
                .WithTags(TraitTag.Damage | TraitTag.AoE)
                .Register();

            // ── 시라유키: 순차 텔레포트 암살 + 지정불가 + 회피버프 ──
            Skill(217663506, E.Channeling, T.LowestHPEnemy)
                .Param(2, P.Int, 200f)           // [0] 데미지 배율
                .Param(1, P.Frames, 3f)          // [1] 지정불가 시간
                .Param(3, P.Frames, 3f)          // [2] 회피 버프 지속
                .Param(4, P.Int, 30f)            // [3] 회피 증가율%
                .On(Evt.Cast)
                    .Do(ApplyStatusEffect(StatusEffectType.TargetImpossible, F.Self, durationParamIndex: 1))
                    .Do(Vfx(0, V.AtCaster))
                .On(Evt.Tick)
                    .Do(WithRepeat(Teleport(), count: 3, dynamicFromClip: true))
                    .Do(Vfx(1, V.AtTarget))
                    .Do(Damage(paramIndex: 0))
                    .Do(Retarget(F.LowestHpAllies, excludeHit: true))
                .On(Evt.Complete)
                    .Do(Buff(StatModType.DodgeChance, 3, 2))
                .WithTags(TraitTag.Damage | TraitTag.Teleport | TraitTag.Buff)
                .Register();
        }
    }
}
