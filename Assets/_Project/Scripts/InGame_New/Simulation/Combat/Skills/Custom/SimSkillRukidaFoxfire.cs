using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 루키다 액티브 스킬 (217263103).
    /// 여우불 추가 + 현재 여우불 수 × 공속비율 공속 버프.
    /// 여우불은 StatusEffect(SkillMarker, Value=SkillMarkerType)로 관리, 개별 타이머로 자동 만료.
    /// </summary>
    public class SimSkillRukidaFoxfire : SimSkillBase
    {
        private const int MarkerValue = (int)SkillMarkerType.RukidaFoxfire;

        private int _foxFireIncrease;
        private int _buffDurationFrames;
        private int _atkSpeedRatePercent;
        private int _foxFireDurationFrames;

        private const int MaxFoxFires = 9;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=여우불 증가량, {2}=공속버프 지속(초), {3}=공속증가율(%)
            _foxFireIncrease = SkillSpecHelper.GetInt(specList, 1, 2f);
            _buffDurationFrames = SkillSpecHelper.GetFrames(specList, 2, 3f, tickRate);
            _atkSpeedRatePercent = SkillSpecHelper.GetInt(specList, 3, 10f);
            _foxFireDurationFrames = 150;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
            => caster.CombatId;

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx < 0) return;

            int currentCount = StatusEffectSystem.CountMarkers(state, casterIdx, MarkerValue);

            // 여우불 추가 (최대 9개, 초과 시 가장 오래된 것 제거 후 추가)
            for (int i = 0; i < _foxFireIncrease; i++)
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
                    StatusEffectType.SkillMarker, MarkerValue, _foxFireDurationFrames);
            }

            // 공속 버프 = 현재 여우불 수 × 공속비율
            int foxCount = StatusEffectSystem.CountMarkers(state, casterIdx, MarkerValue);
            int totalAtkSpeedBonus = _atkSpeedRatePercent * foxCount;
            if (totalAtkSpeedBonus > 0)
            {
                SkillBuffHelper.ApplyTimedBuff(state, casterIdx,
                    StatModType.AttackSpeed, totalAtkSpeedBonus, _buffDurationFrames);
            }

            // VFX 이벤트: 본인에게 스폰 (여우불 수는 dirCol로 전달)
            state.EventQueue?.PushSkillPhaseVfx(
                caster.CombatId, SkillId, 0, dirCol: (sbyte)foxCount);
        }
    }
}
