using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;
using E = CookApps.AutoChess.SkillExecutionType;
using T = CookApps.AutoChess.SkillTargetType;
using F = CookApps.AutoChess.SkillTargetFilter;
using S = CookApps.AutoChess.SkillAreaShape;
using P = CookApps.AutoChess.ParamValueType;

namespace CookApps.AutoChess
{
    public static partial class SkillFactory
    {
        /// <summary>보스탱커: 타일 간 순차 타격 간격.</summary>
        private const short BossTankLineIntervalMs = 200;

        // ── Preset 함수 (파라미터 없는 고정 패턴) ──

        static SkillRecipeBuilder PresetDamageStun(SkillRecipeBuilder b)
            => b.OnCast(Damage()).OnCast(CC(CrowdControlType.Stun));

        static SkillRecipeBuilder PresetSingleDamage(SkillRecipeBuilder b)
            => b.OnCast(Damage());

        static SkillRecipeBuilder PresetConeDamage(SkillRecipeBuilder b)
            => b.OnCast(Damage(filter: F.EnemiesInArea, area: S.Line, range: 2));

        static SkillRecipeBuilder PresetMultiHit(SkillRecipeBuilder b)
            => b.OnCast(MultiHit());

        static SkillRecipeBuilder PresetMultiTargetHeal(SkillRecipeBuilder b)
            => b.OnCast(Heal(filter: F.LowestHpAllies, range: 3));

        /// <summary>몬스터 스킬 Recipe 등록</summary>
        private static void RegisterMonsterRecipes()
        {
            // ── DamageCC (데미지 + 스턴) ──
            // 1102061=5챕터 탱커, 230404002/230505002/230606002=4/5/2챕터 탱커
            // 240107001=베놈, 240407301=사막 전갈, 250208101=1챕터 보스
            foreach (var id in new[] { 1102061, 230404002, 230505002, 230606002,
                                        240107001, 240407301, 250208101 })
                Skill(id, E.Instant, T.NearestEnemy).Apply(PresetDamageStun).Register();

            // ── ConeDamage (전방 직선 데미지) ──
            // 230101002=0챕터 가디언, 230404001/230505001/230606001=4/5/3챕터 가디언
            // 280109001=공허의 토마
            foreach (var id in new[] { 230101002, 230404001, 230505001, 230606001, 280109001 })
                Skill(id, E.Instant, T.NearestEnemy).Apply(PresetConeDamage).Register();

            // ── DiamondAoE (맨허튼 거리 범위, range=1) ──
            // 230202003=1챕터 마법사, 230606003=2챕터 마법사
            foreach (var id in new[] { 230202003, 230606003 })
                Skill(id, E.DelayedApply, T.NearestEnemy)
                    .AtHit(Vfx(0, SkillVfxPlacement.AtGridPos))
                    .AtHit(AreaVfx(SkillVfxPlacement.AreaEffect, 1))
                    .AtHit(AreaVfx(SkillVfxPlacement.PerTileInDiamond, 1, vfxIndex: 1))
                    .AtHit(Damage(filter: F.EnemiesInArea, area: S.Diamond, range: 1))
                    .Register();

            // ── PatternDamage (범위 데미지 + 스턴) ──
            // 1103041=6챕터 마법사, 1203021=5챕터 독 두꺼비, 230505003=5챕터 마법사
            // 250608501=2챕터 보스, 280109002=라플라스마녀
            foreach (var id in new[] { 1103041, 1203021, 230505003, 250608501, 280109002 })
                Skill(id, E.Instant, T.BestAoETarget)
                    .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .OnCast(AreaCC(CrowdControlType.Stun, S.Circle, 1))
                    .Register();

            // ── LineDamage (직선 관통 투사체) ──
            // 1104081=6챕터 저격수, 230404004/230505004/230606004=4/5/3챕터 저격수
            // 240107002=빅마우스
            foreach (var id in new[] { 1104081, 230404004, 230505004, 230606004, 240107002 })
                Skill(id, E.DelayedApply, T.NearestEnemy).Projectile()
                    .AtHit(SpawnLinearProjectile())
                    .Register();

            // ── TeleportStrike (이동 후 범위 공격 + 스턴) ──
            // 1202091=6챕터 버팔로, 240407302=샌드웜
            // 250108002/250108003=정글 버팔로/샌드웜
            foreach (var id in new[] { 1202091, 240407302, 250108002, 250108003 })
                Skill(id, E.Instant, T.NearestEnemy)
                    .OnCast(Damage(filter: F.EnemiesInArea, area: S.Circle, range: 1))
                    .OnCast(AreaCC(CrowdControlType.Stun, S.Circle, 1))
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
                .Param(1, P.Int, 200f)
                .OnTick(SequentialLine(0, lineLength: 10,
                    intervalMs: BossTankLineIntervalMs, repeatCount: 10))
                .Register();
        }
    }
}
