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
        /// <summary>보스탱커: 타일 간 순차 타격 간격.</summary>
        private const short BossTankLineIntervalMs = 200;

        // ── Preset 함수 (파라미터 없는 고정 패턴) ──

        static SkillRecipeBuilder PresetDamageStun(SkillRecipeBuilder b)
            => b.On(Evt.Cast).Do(Damage()).Do(CC(CrowdControlType.Stun));

        static SkillRecipeBuilder PresetSingleDamage(SkillRecipeBuilder b)
            => b.On(Evt.Cast).Do(Damage());

        static SkillRecipeBuilder PresetConeDamage(SkillRecipeBuilder b)
            => b.On(Evt.Cast).Do(Damage(filter: F.EnemiesInArea, area: S.Line, range: 2));

        static SkillRecipeBuilder PresetMultiHit(SkillRecipeBuilder b)
            => b.On(Evt.Cast).Do(MultiHit());

        static SkillRecipeBuilder PresetMultiTargetHeal(SkillRecipeBuilder b)
            => b.On(Evt.Cast).Do(Heal(filter: F.LowestHpAllies, range: 3));

        /// <summary>몬스터 스킬 Recipe 등록</summary>
        private static void RegisterMonsterRecipes()
        {
            // ── DamageCC (데미지 + 스턴) ──
            // 1102061=5챕터 탱커, 230404002/230505002/230606002=4/5/2챕터 탱커
            // 240407301=사막 전갈, 250208101=1챕터 보스
            foreach (var id in new[] { 1102061, 230404002, 230505002, 230606002,
                                        240407301, 250208101 })
                Skill(id, E.Instant, T.NearestEnemy).Apply(PresetDamageStun).Register();

            // ── 베놈 (240107001): Damage + DOT(독) ──
            // 원본: {1}% 데미지 + {2}초간 공격력×{3}% DOT
            Skill(240107001, E.Instant, T.NearestEnemy)
                .On(Evt.Cast)
                    .Do(Damage(power: Spec(1, 200f)))
                    .Do(Debuff(StatusEffectType.DamageOverTime,
                        value: AtkPercent(3, 10f),
                        duration: Spec(2, P.Frames, 3f)))
                .Register();

            // ── ConeDamage (전방 직선 데미지) ──
            // 230101002=0챕터 가디언, 230404001/230505001/230606001=4/5/3챕터 가디언
            foreach (var id in new[] { 230101002, 230404001, 230505001, 230606001 })
                Skill(id, E.Instant, T.NearestEnemy).Apply(PresetConeDamage).Register();

            // ── 공허의 토마 (280109001): 텔레포트 + 3×3 AoE + 방어/공속 감소 ──
            // 원본: 공격력 최고 적 근처 빈 타일 텔레포트 → 3×3 {1}% 데미지 + {2}초 {3}% 방어/공속 감소
            Skill(280109001, E.DelayedApply, T.HighestAttackEnemy)
                .On(Evt.Execute1)
                    .Do(Teleport())
                    .Do(TileEffect(S.Circle, range: 1, isBox: true))
                    .Do(Vfx(2, V.AtCaster))
                    .Do(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(Debuff(StatModType.Def, value: Spec(3, 10f), duration: Spec(2, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .Do(Debuff(StatModType.AttackSpeed, value: Spec(3, 10f), duration: Spec(2, P.Frames, 3f),
                        filter: F.EnemiesInArea, area: S.Circle, range: 1))
                .Register();

            // ── DiamondAoE (맨허튼 거리 범위, range=1) ──
            // 230202003=1챕터 마법사, 230606003=2챕터 마법사
            foreach (var id in new[] { 230202003, 230606003 })
                Skill(id, E.DelayedApply, T.NearestEnemy)
                    .On(Evt.Execute1)
                        .Do(Vfx(0, V.AtGridPos))
                        .Do(AreaVfx(V.AreaEffect, 1))
                        .Do(AreaVfx(V.PerTileInDiamond, 1, vfxIndex: 1))
                        .Do(Damage(filter: F.EnemiesInArea, area: S.Diamond, range: 1))
                    .Register();

            // ── PatternDamage (범위 데미지 + 스턴) ──
            // 1103041=6챕터 마법사, 1203021=5챕터 독 두꺼비, 230505003=5챕터 마법사
            // 250608501=2챕터 보스, 280109002=라플라스마녀
            foreach (var id in new[] { 1103041, 1203021, 230505003, 250608501, 280109002 })
                Skill(id, E.Instant, T.BestAoETarget)
                    .On(Evt.Cast)
                        .Do(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                        .Do(AreaCC(CrowdControlType.Stun, S.Circle, 1))
                    .Register();

            // ── LineDamage (직선 관통 투사체) ──
            // 1104081=6챕터 저격수, 230404004/230505004/230606004=4/5/3챕터 저격수
            foreach (var id in new[] { 1104081, 230404004, 230505004, 230606004 })
                Skill(id, E.DelayedApply, T.NearestEnemy).Projectile()
                    .On(Evt.Execute1)
                        .Do(SpawnLinearProjectile())
                    .Register();

            // ── 빅마우스 (240107002): 대쉬 돌진 + 경로 관통 데미지 + 기절 ──
            // 원본 3단계: Execute0=대쉬+데미지+기절, Execute1=오버슈트+포탈VFX, Execute2=원위치복귀
            // range 0 = 타겟까지 거리 동적 계산
            Skill(240107002, E.Channeling, T.NearestEnemy)
                .On(Evt.Execute1)  // 대쉬 돌진 + 관통 데미지 + 기절
                    .Do(Vfx(0, V.AtTargetWithDir))
                    .Do(Vfx(1, V.AtCasterWithDir))
                    .Do(Damage(power: Spec(1, 200f), filter: F.EnemiesInArea, area: S.Line))
                    .Do(CC(CrowdControlType.Stun, duration: Spec(2, P.Frames, 3f)))
                .On(Evt.Execute2)  // 오버슈트 + 돌아오는 포탈
                    .Do(Vfx(0, V.AtCasterWithDir))
                .On(Evt.Execute3)  // 원위치 복귀
                .Register();

            // ── TeleportStrike (이동 후 범위 공격 + 스턴) ──
            // 1202091=6챕터 버팔로, 240407302=샌드웜
            // 250108002/250108003=정글 버팔로/샌드웜
            foreach (var id in new[] { 1202091, 240407302, 250108002, 250108003 })
                Skill(id, E.Instant, T.NearestEnemy)
                    .On(Evt.Cast)
                        .Do(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                        .Do(AreaCC(CrowdControlType.Stun, S.Circle, 1))
                    .Register();

            // ── MultiHit (다단히트) ──
            // 1105031=6챕터 암살자, 230404005/230505005=4/5챕터 암살자
            foreach (var id in new[] { 1105031, 230404005, 230505005 })
                Skill(id, E.Instant, T.NearestEnemy).Apply(PresetMultiHit).Register();

            // ── MultiTargetHeal (다대상 힐) ──
            // 1106041=6챕터 서포터, 230404006/230505006/230606006=4/5/3챕터 서포터
            foreach (var id in new[] { 1106041, 230404006, 230505006, 230606006 })
                Skill(id, E.Instant, T.LowestHPAlly).Apply(PresetMultiTargetHeal).Register();

            // ── 보스 탱커 (250108001): 전방 10칸 순차 타격 + 넉백 ──
            Skill(250108001, E.Channeling, T.NearestEnemy)
                .On(Evt.Tick)
                    .Do(WithRepeat(SequentialLine(power: Spec(1, 200f), lineLength: 10,
                        intervalMs: BossTankLineIntervalMs, repeatCount: 10)))
                .Register();
        }
    }
}
