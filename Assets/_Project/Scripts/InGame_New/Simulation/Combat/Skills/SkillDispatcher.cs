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
        public static void InitializeFromSpec(ref SimSkillInstance skill, SkillParams baseParams,
            List<SkillActive> specList, int tickRate)
        {
            skill.InitializeBase(baseParams);
            switch (skill.Type)
            {
                case SkillImplType.Generic: GenericSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillImplType.Rukida:  RukidaSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillImplType.April:   AprilSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillImplType.Enki:    EnkiSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
                case SkillImplType.Adria:   AdriaSkillLogic.InitializeFromSpec(ref skill, specList, tickRate); break;
            }
        }

        public static int SelectTarget(ref SimSkillInstance skill, CombatMatchState state, ref CombatUnit caster)
        {
            switch (skill.Type)
            {
                case SkillImplType.Generic: return GenericSkillLogic.SelectTarget(ref skill, state, ref caster);
                case SkillImplType.Rukida:  return caster.CombatId;
                case SkillImplType.April:   return TargetingSystem.FindNearestEnemy(state, ref caster);
                case SkillImplType.Enki:    return EnkiSkillLogic.SelectTarget(ref skill, state, ref caster);
                case SkillImplType.Adria:   return caster.CombatId;
                default:                return CombatUnit.InvalidId;
            }
        }

        public static void Execute(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            switch (skill.Type)
            {
                case SkillImplType.Generic: GenericSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.Rukida:  RukidaSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.April:   AprilSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.Enki:    EnkiSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
                case SkillImplType.Adria:   AdriaSkillLogic.Execute(ref skill, state, ref caster, targetCombatId, ref rng); break;
            }
        }

        public static bool OnChannelTick(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            switch (skill.Type)
            {
                case SkillImplType.Generic: return GenericSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                case SkillImplType.April:   return AprilSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                case SkillImplType.Enki:    return EnkiSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                case SkillImplType.Adria:   return AdriaSkillLogic.OnChannelTick(ref skill, state, ref caster, ref rng);
                default:                return false; // Rukida는 Instant → 채널링 없음
            }
        }

        /// <summary>DelayedApply 기본 구현 (base.OnChannelTick 대체)</summary>
        public static bool OnChannelTickDelayedApply(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (skill.ExecutionType != SkillExecutionType.DelayedApply) return false;

            if (skill.DelayTimer <= 0)
                skill.DelayTimer = skill.SkillHitFrames != null && skill.SkillHitFrames.Length > 0
                    ? skill.SkillHitFrames[0] : 10;

            skill.DelayTimer--;
            if (skill.DelayTimer > 0) return true;

            GenericSkillLogic.ApplySkillEffect(ref skill, state, ref caster, ref rng);
            return false;
        }
    }
}
