using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;
using E = CookApps.AutoChess.SkillExecutionType;
using T = CookApps.AutoChess.SkillTargetType;
using F = CookApps.AutoChess.SkillTargetFilter;
using S = CookApps.AutoChess.SkillAreaShape;
using V = CookApps.AutoChess.SkillVfxPlacement;
using P = CookApps.AutoChess.ParamValueType;

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
            // SimSkillGeneric으로 완전 대체 (커스텀 클래스 삭제됨)
            // ══════════════════════════════

            // ── 필리아: DelayedApply, Damage + 3단계 VFX + 마커 ──
            Skill(215532401, E.DelayedApply, T.FarthestEnemy)
                .Param(1, P.Int, 200f)
                .OnCast(Vfx(0, V.AtGridPos))
                .AtHit(AddMarker(SkillMarkerType.PiliaSkillCast))
                .AtHit(Damage(paramIndex: 0))
                .AtHit(Vfx(1, V.AtCasterWithDir))
                .AtHit(Vfx(2, V.AtTarget))
                .Register();

            // ── 하티: DelayedApply, Damage + Knockback + 3단계 VFX ──
            Skill(217433303, E.DelayedApply, T.FarthestEnemy)
                .Param(1, P.Int, 200f)          // [0] 데미지 배율
                .Param(2, P.Int, 2f)             // [1] 넉백 거리
                .OnCast(Vfx(0, V.AtCaster))
                .OnCast(Vfx(1, V.AtTarget))
                .AtHit(Damage(paramIndex: 0))
                .AtHit(Vfx(2, V.AtCasterWithDir))
                .AtHit(Knockback(distParamIndex: 1))
                .Register();

            // ── 유니: DelayedApply, 최저HP 3명 Heal + RemoveDebuffs ──
            Skill(215252102, E.DelayedApply, T.LowestHPAlly)
                .Param(1, P.Int, 200f)
                .AtHit(Vfx(0, V.AtCaster))
                .AtHit(Heal(paramIndex: 0, filter: F.LowestHpAllies, range: 3))
                .AtHit(RemoveDebuffs(F.LowestHpAllies, range: 3))
                .Register();

            // ── 멘샤: DelayedApply, 같은 행 아군 Shield ──
            Skill(215422301, E.DelayedApply, T.Self)
                .Param(1, P.Int, 20f)            // [0] 실드 비율 (maxHP%)
                .Param(2, P.Frames, 3f)          // [1] 실드 지속
                .AtHit(Shield(0, 1, F.SameRowAllies))
                .Register();

            // ── 미사: DelayedApply, CC(Stun) + Marker ──
            Skill(217323201, E.DelayedApply, T.HighestAttackEnemy)
                .Param(1, P.Int, 200f)           // [0] (미사용)
                .Param(2, P.Frames, 3f)          // [1] 스턴 지속
                .AtHit(Vfx(0, V.AtTarget))
                .AtHit(Vfx(1, V.AtTarget))
                .AtHit(CC(CrowdControlType.Stun, durationParamIndex: 1))
                .AtHit(AddMarker(SkillMarkerType.MisaRestraint))
                .Register();

            // ── 클레이: Channeling, Zone 힐+데미지+디버프 ──
            Skill(217553404, E.Channeling, T.Self)
                .Param(1, P.Int, 200f)           // [0] 힐 배율
                .Param(2, P.Int, 80f)            // [1] 데미지 배율
                .Param(3, P.Int, 50f)            // [2] 회복감소%
                .Param(4, P.Frames, 3f)          // [3] 디버프 지속
                .AtHit(Vfx(0, V.AtCaster))
                .OnTick(WithRepeat(Heal(paramIndex: 0, filter: F.AlliesInArea, area: S.Diamond, range: 2),
                    intervalFrames: 15, dynamicFromClip: true))
                .OnTick(RemoveDebuffs(F.AlliesInArea, S.Diamond, 2))
                .OnTick(Damage(paramIndex: 1, filter: F.EnemiesInArea, area: S.Diamond, range: 2))
                .OnTick(Debuff(StatusEffectType.HealReduction, 2, 3, F.EnemiesInArea, S.Diamond, 2))
                .OnTick(AreaVfx(V.AreaEffect, 2, condition: SkillActionCondition.EveryNth2))
                .Register();

            // ── 엘리스: Channeling, 2단계 Diamond AoE ──
            Skill(215642501, E.Channeling, T.NearestEnemy)
                .Param(1, P.Int, 200f)
                .AtHit(AreaVfx(V.AreaEffect, 1))
                .AtHit(AreaVfx(V.PerTileInDiamond, 1, vfxIndex: 0))
                .OnTick(WithRepeat(Damage(paramIndex: 0, filter: F.EnemiesInArea, area: S.Diamond, range: 1),
                    count: 1, intervalMs: EllisDamageDelayMs))
                .Register();

            // ── 아트레시아: DelayedApply, 3칸 폭 직선 관통 투사체 ──
            Skill(217513401, E.DelayedApply, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .AtHit(SpawnLinearProjectile(paramIndex: 0, width: 3))
                .Register();

            // ── 메이: DelayedApply, Plus AoE + 넉백 + 방어 버프 ──
            Skill(215322201, E.DelayedApply, T.Self)
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .Param(2, P.Frames, 4f)          // [1] 버프 지속
                .Param(3, P.Int, 50f)            // [2] 방어력 버프%
                .OnCast(Vfx(0, V.AtCaster))
                .AtHit(DamageKnockback(S.Plus, 1, paramIndex: 0))
                .AtHit(Buff(StatModType.Def, 2, 1))
                .Register();

            // ── SingleProjectile (230101005, 230202004): Homing 투사체 ──
            Skill(230101005, E.DelayedApply, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .AtHit(SpawnProjectile(paramIndex: 0, vfxIndex: 1))
                .Register();

            Skill(230202004, E.DelayedApply, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .AtHit(SpawnProjectile(paramIndex: 0, vfxIndex: 1))
                .Register();

            // ══════════════════════════════
            // 커스텀 클래스 유지 — ParamSlots만 Recipe에서 관리
            // ══════════════════════════════

            // ── 테토라: Damage + 4칸 넉백 + 착지 AoE + 스턴 ──
            Skill(217413301, E.DelayedApply, T.NearestEnemy)
                .Param(1, P.Int, 200f)           // [0] 주 데미지 배율
                .Param(3, P.Int, 200f)           // [1] 후속 데미지 배율
                .Register();

            // ── 루키다: 여우불 마커 + 공속 버프 ──
            Skill(217263103, E.Instant, T.Self)
                .Param(1, P.Int, 2f)             // [0] 여우불 증가량
                .Param(2, P.Frames, 3f)          // [1] 버프 지속
                .Param(3, P.Int, 10f)            // [2] 공속 증가율%
                .Register();

            // ── 라키유: 베지어 투사체 → 범위 디버프 ──
            Skill(217353203, E.Channeling, T.NearestEnemy).Projectile()
                .Param(1, P.Frames, 3f)          // [0] 디버프 지속
                .Param(2, P.Int, 50f)            // [1] 회복감소%
                .Param(3, P.Int, 30f)            // [2] 방어감소%
                .Register();

            // ── 미노: 순차 미사일 ──
            Skill(217433302, E.Channeling, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)
                .Register();

            // ── 베인: 바운스 투사체 ──
            Skill(217363204, E.Channeling, T.NearestEnemy).Projectile()
                .Param(1, P.Int, 200f)           // [0] 데미지 배율
                .Param(2, P.Int, 20f)            // [1] 바운스 감소율%
                .Param(3, P.Frames, 3f)          // [2] 공속 버프 지속
                .Param(4, P.Int, 30f)            // [3] 공속 증가율%
                .Param(5, P.Int, 5f)             // [4] 최대 바운스
                .Register();

            // ── 마리에: 텔레포트 + 다단히트 ──
            Skill(217563405, E.Channeling, T.HighestAttackEnemy)
                .Param(2, P.Int, 200f)           // [0] 데미지 배율
                .Param(1, P.Int, 4f)             // [1] 히트수
                .Param(3, P.Frames, 3f)          // [2] 디버프 지속
                .Param(4, P.Int, 30f)            // [3] 디버프%
                .Register();

            // ── 에이프릴: 확장 콘 바라지 ──
            Skill(217333202, E.Channeling, T.NearestEnemy)
                .Param(2, P.Int, 100f)           // [0] 근거리 배율
                .Param(1, P.Int, 10f)            // [1] 히트 회수
                .Param(3, P.Int, 75f)            // [2] 중거리 배율
                .Param(4, P.Int, 50f)            // [3] 원거리 배율
                .Register();

            // ── 엔키: 보드 스윕 힐 투사체 ──
            Skill(217653505, E.Channeling, T.NearestEnemy)
                .Param(1, P.Int, 200f)           // [0] 힐 배율
                .Param(2, P.Frames, 6f)          // [1] HoT 지속
                .Param(3, P.Int, 50f)            // [2] HoT 위력%
                .Register();

            // ── 오데트: 2단계 텔레포트 ──
            Skill(217613501, E.Channeling, T.NearestEnemy)
                .Param(1, P.Int, 200f)
                .Param(2, P.Frames, 3f)
                .Param(3, P.Int, 30f)
                .Register();

            // ── 아드리아: 3단계 확장 패턴 ──
            Skill(217523403, E.Channeling, T.Self)
                .Param(1, P.Int, 200f)
                .Param(2, P.Int, 100f)
                .Param(3, P.Frames, 2f)
                .Register();

            // ── 시라유키: 순차 텔레포트 암살 ──
            Skill(217663506, E.Channeling, T.LowestHPEnemy)
                .Param(2, P.Int, 200f)           // [0] 데미지 배율
                .Param(1, P.Frames, 3f)          // [1] 지정불가 시간
                .Param(3, P.Frames, 3f)          // [2] 회피 버프 지속
                .Param(4, P.Int, 30f)            // [3] 회피 증가율%
                .Register();
        }
    }
}
