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

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _foxFireIncrease = p.Param0 > 0 ? p.Param0 : 2;
            _buffDurationFrames = p.Param1 > 0 ? p.Param1 : 90;
            _atkSpeedRatePercent = p.Param2 > 0 ? p.Param2 : 10;
            _foxFireDurationFrames = p.Param3 > 0 ? p.Param3 : 150;
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
