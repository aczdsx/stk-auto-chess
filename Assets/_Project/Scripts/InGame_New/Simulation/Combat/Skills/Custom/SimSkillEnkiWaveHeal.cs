using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>엔키: 파도 채널링 — 뒤에서 앞으로 보드 전체를 이동하며 아군 힐 + HoT.
    /// 힐/타일이펙트/VFX 모두 ProjectileSystem(HealAlly)에서 동기 처리.</summary>
    public static class EnkiSkillLogic
    {
        private const int DefaultMoveInterval = 24; // 24프레임마다 1행 이동 (~0.4초/행, speed≈5)
        private const int WaveWidth = 5;           // 파도 폭 5칸

        public static void InitializeFromSpec(ref SimSkillInstance skill, List<SkillActive> specList, int tickRate)
        {
            skill.ExecutionType = SkillExecutionType.Channeling;
            // {0}=쿨타임, {1}=힐배율(%)→PowerPercent, {2}=HoT지속(초), {3}=HoT위력(%)
            skill.PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            skill.HotDuration = SkillSpecHelper.GetFrames(specList, 2, 6f, tickRate);
            skill.HotInterval = 30;
            skill.SecondaryPowerPercent = SkillSpecHelper.GetInt(specList, 3, 50f);
        }

        public static int SelectTarget(ref SimSkillInstance skill, CombatMatchState state, ref CombatUnit caster)
        {
            // 적이 존재할 때만 시전, 자기 자신을 반환하여 facing 유지
            int nearestEnemy = TargetingSystem.FindNearestEnemy(state, ref caster);
            return nearestEnemy != CombatUnit.InvalidId ? caster.CombatId : CombatUnit.InvalidId;
        }

        public static void Execute(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            // 준비만: 상태 저장 + 타이머 설정 (LineDamage 패턴)
            skill.WaveDirRow = caster.TeamIndex == 0 ? 1 : -1;
            skill.StartRow = skill.WaveDirRow > 0 ? 0 : BoardHelper.CombatHeight - 1;
            skill.CenterCol = caster.GridCol;
            skill.HalfWidth = WaveWidth / 2;
            skill.CachedCasterCombatId = caster.CombatId;
            skill.CachedAttack = caster.Attack;
            skill.Fired = false;
            skill.Channeling = true;
            skill.PhaseTimer = skill.SkillHitFrames != null && skill.SkillHitFrames.Length > 0
                ? skill.SkillHitFrames[0]
                : DefaultMoveInterval;
        }

        public static bool OnChannelTick(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (!skill.Channeling) return false;

            // 발사 전: SkillHitFrames[0] 대기
            if (!skill.Fired)
            {
                skill.PhaseTimer--;
                if (skill.PhaseTimer > 0) return true;

                skill.Fired = true;
                SpawnWaveProjectiles(ref skill, state);
                skill.ChannelFramesRemaining = (BoardHelper.CombatHeight - 1) * DefaultMoveInterval;
                return true;
            }

            // 발사 후: 투사체가 보드 끝까지 이동할 때까지 채널링 유지
            skill.ChannelFramesRemaining--;
            if (skill.ChannelFramesRemaining <= 0)
            {
                skill.Channeling = false;
                return false;
            }

            return true;
        }

        private static void SpawnWaveProjectiles(ref SimSkillInstance skill, CombatMatchState state)
        {
            int healAmount = skill.CachedAttack * skill.PowerPercent / 100;
            int hotPerTick = skill.CachedAttack * skill.SecondaryPowerPercent / 100;

            int minCol = skill.CenterCol - skill.HalfWidth;
            int maxCol = skill.CenterCol + skill.HalfWidth;

            for (int c = minCol; c <= maxCol; c++)
            {
                if (!BoardHelper.IsValidCombatPosition(c, skill.StartRow)) continue;

                ProjectileSystem.CreateLinearHealProjectile(
                    state, skill.CachedCasterCombatId,
                    (byte)c, (byte)skill.StartRow,
                    0, (sbyte)skill.WaveDirRow,
                    healAmount, DamageType.Physical,
                    DefaultMoveInterval, BoardHelper.CombatHeight, skill.SkillId,
                    skillVfxIndex: 1,
                    hotPerTick: hotPerTick, hotDuration: skill.HotDuration, hotInterval: skill.HotInterval,
                    areaEffectHalfWidth: c == skill.CenterCol ? (byte)skill.HalfWidth : (byte)0);
            }
        }
    }
}
