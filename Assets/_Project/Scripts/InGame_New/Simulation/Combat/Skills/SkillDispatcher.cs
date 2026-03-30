using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 스킬 static dispatch 허브.
    /// SkillType enum 기반 switch로 각 스킬 로직의 static 메서드 호출.
    /// </summary>
    public static class SkillDispatcher
    {
        public static void InitializeFromSpec(ref SkillConfig config, ref SkillState state,
            SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            config.InitializeBase(baseParams);
            state.DelayTimer = -1;
            switch (config.Type)
            {
                case SkillImplType.Generic: GenericSkillLogic.InitializeFromSpec(ref config, ref state, specList, tickRate); break;
                case SkillImplType.Rukida:  RukidaSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
                case SkillImplType.April:   AprilSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
                case SkillImplType.Enki:    EnkiSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
                case SkillImplType.Adria:   AdriaSkillLogic.InitializeFromSpec(ref config, specList, tickRate); break;
            }
        }

        public static int SelectTarget(ref SkillConfig config, CombatMatchState state, ref CombatUnit caster)
        {
            switch (config.Type)
            {
                case SkillImplType.Generic: return GenericSkillLogic.SelectTarget(ref config, state, ref caster);
                case SkillImplType.Rukida:  return caster.CombatId;
                case SkillImplType.April:   return TargetingSystem.FindNearestEnemy(state, ref caster);
                case SkillImplType.Enki:    return EnkiSkillLogic.SelectTarget(ref config, state, ref caster);
                case SkillImplType.Adria:   return caster.CombatId;
                default:                return CombatUnit.InvalidId;
            }
        }

        public static void Execute(ref SkillConfig config, ref SkillState state,
            CombatMatchState matchState, ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            switch (config.Type)
            {
                case SkillImplType.Generic: GenericSkillLogic.Execute(ref config, ref state, matchState, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.Rukida:  RukidaSkillLogic.Execute(ref config, matchState, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.April:   AprilSkillLogic.Execute(ref config, ref state, matchState, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.Enki:    EnkiSkillLogic.Execute(ref config, ref state, matchState, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.Adria:   AdriaSkillLogic.Execute(ref config, ref state, matchState, ref caster, targetCombatId, ref rng); break;
            }
        }

        public static bool OnChannelTick(ref SkillConfig config, ref SkillState state,
            CombatMatchState matchState, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            switch (config.Type)
            {
                case SkillImplType.Generic: return GenericSkillLogic.OnChannelTick(ref config, ref state, matchState, ref caster, ref rng);
                case SkillImplType.April:   return AprilSkillLogic.OnChannelTick(ref config, ref state, matchState, ref caster, ref rng);
                case SkillImplType.Enki:    return EnkiSkillLogic.OnChannelTick(ref config, ref state, matchState, ref caster, ref rng);
                case SkillImplType.Adria:   return AdriaSkillLogic.OnChannelTick(ref config, ref state, matchState, ref caster, ref rng);
                default:                return false;
            }
        }

        /// <summary>DelayedApply 기본 구현 (base.OnChannelTick 대체)</summary>
        public static bool OnChannelTickDelayedApply(ref SkillConfig config, ref SkillState state,
            CombatMatchState matchState, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (config.ExecutionType != SkillExecutionType.DelayedApply) return false;

            if (state.DelayTimer <= 0)
                state.DelayTimer = config.SkillHitFrames != null && config.SkillHitFrames.Length > 0
                    ? config.SkillHitFrames[0] : 10;

            state.DelayTimer--;
            if (state.DelayTimer > 0) return true;

            GenericSkillLogic.ApplySkillEffect(ref config, ref state, matchState, ref caster, ref rng);
            return false;
        }
    }
}
