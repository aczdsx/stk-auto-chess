using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 루키다 액티브 스킬 (217263103).
    /// 여우불 추가 + 현재 여우불 수 × 공속비율 공속 버프.
    /// 여우불은 StatusEffect(SkillMarker, Value=SkillMarkerType)로 관리, 개별 타이머로 자동 만료.
    /// </summary>
    public static class RukidaSkillLogic
    {
        private const int MarkerValue = (int)SkillMarkerType.RukidaFoxfire;
        private const int MaxFoxFires = 9;

        public static void InitializeFromSpec(ref SimSkillInstance skill, List<SkillActive> specList, int tickRate)
        {
            // {0}=쿨타임, {1}=여우불 증가량, {2}=공속버프 지속(초), {3}=공속증가율(%)
            skill.FoxFireIncrease = SkillSpecHelper.GetInt(specList, 1, 2f);
            skill.BuffDurationFrames = SkillSpecHelper.GetFrames(specList, 2, 3f, tickRate);
            skill.AtkSpeedRatePercent = SkillSpecHelper.GetInt(specList, 3, 10f);
        }

        public static void Execute(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx < 0) return;

            int currentCount = StatusEffectSystem.CountMarkers(state, casterIdx, MarkerValue);

            // 여우불 추가 (최대 9개, 초과 시 가장 오래된 것 제거 후 추가)
            for (int i = 0; i < skill.FoxFireIncrease; i++)
            {
                if (currentCount >= MaxFoxFires)
                {
                    StatusEffectSystem.RemoveOldestMarker(state, casterIdx, MarkerValue);
                }
                else
                {
                    currentCount++;
                }

                StatusEffectSystem.AddEffect(state, casterIdx,
                    StatusEffectType.SkillMarker, MarkerValue, skill.BuffDurationFrames);
            }

            // 공속 버프 = 현재 여우불 수 × 공속비율
            int foxCount = StatusEffectSystem.CountMarkers(state, casterIdx, MarkerValue);
            int totalAtkSpeedBonus = skill.AtkSpeedRatePercent * foxCount;
            if (totalAtkSpeedBonus > 0)
            {
                SkillBuffHelper.ApplyTimedBuff(state, casterIdx,
                    StatModType.AttackSpeed, totalAtkSpeedBonus, skill.BuffDurationFrames,
                    sourceSkillId: skill.SkillId);
            }

            // VFX 이벤트: 본인에게 스폰 (여우불 수는 dirCol로 전달)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, skill.SkillId, 0, dirCol: (sbyte)foxCount);
        }
    }
}
