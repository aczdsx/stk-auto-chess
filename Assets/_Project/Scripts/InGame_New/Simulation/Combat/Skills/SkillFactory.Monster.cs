using static CookApps.AutoChess.SkillFactory.SkillRecipeBuilder;
using E = CookApps.AutoChess.SkillExecutionType;
using T = CookApps.AutoChess.SkillTargetType;
using P = CookApps.AutoChess.ParamValueType;

namespace CookApps.AutoChess
{
    public static partial class SkillFactory
    {
        /// <summary>보스탱커: 타일 간 순차 타격 간격.</summary>
        private const short BossTankLineIntervalMs = 200;

        /// <summary>몬스터 커스텀 스킬 Recipe 등록</summary>
        private static void RegisterMonsterRecipes()
        {
            // ── 보스 탱커: 전방 10칸 순차 타격 + 넉백 ──
            Skill(250108001, E.Channeling, T.NearestEnemy)
                .Param(1, P.Int, 200f)
                .OnTick(SequentialLine(0, lineLength: 10,
                    intervalMs: BossTankLineIntervalMs, repeatCount: 10))
                .Register();
        }
    }
}
