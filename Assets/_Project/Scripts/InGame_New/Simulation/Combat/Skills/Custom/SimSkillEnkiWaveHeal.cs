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

        public static void InitializeFromSpec(ref SkillConfig config, List<SkillActive> specList, int tickRate)
        {
            config.ExecutionType = SkillExecutionType.Channeling;
            // {0}=쿨타임, {1}=힐배율(%)→PowerPercent, {2}=HoT지속(초), {3}=HoT위력(%)
            config.PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            config.HotDuration = SkillSpecHelper.GetFrames(specList, 2, 6f, tickRate);
            config.HotInterval = 30;
            config.SecondaryPowerPercent = SkillSpecHelper.GetInt(specList, 3, 50f);
        }

        public static int SelectTarget(ref SkillConfig config, CombatMatchState state, ref CombatUnit caster)
        {
            // 적이 존재할 때만 시전, 자기 자신을 반환하여 facing 유지
            int nearestEnemy = TargetingSystem.FindNearestEnemy(state, ref caster);
            return nearestEnemy != CombatUnit.InvalidId ? caster.CombatId : CombatUnit.InvalidId;
        }

        public static void Execute(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            ref var enki = ref state.Custom.Enki;

            // 준비만: 상태 저장 + 타이머 설정 (LineDamage 패턴)
            enki.WaveDirRow = caster.TeamIndex == 0 ? 1 : -1;
            enki.StartRow = enki.WaveDirRow > 0 ? 0 : BoardHelper.CombatHeight - 1;
            enki.CenterCol = caster.GridCol;
            enki.HalfWidth = WaveWidth / 2;
            enki.CachedCasterCombatId = caster.CombatId;
            enki.CachedAttack = caster.Attack;
            enki.Fired = 0;
            enki.Channeling = 1;
            enki.PhaseTimer = config.SkillHitFrames != null && config.SkillHitFrames.Length > 0
                ? config.SkillHitFrames[0]
                : DefaultMoveInterval;
        }

        public static bool OnChannelTick(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            ref var enki = ref state.Custom.Enki;
            if (enki.Channeling == 0) return false;

            // 발사 전: SkillHitFrames[0] 대기
            if (enki.Fired == 0)
            {
                enki.PhaseTimer--;
                if (enki.PhaseTimer > 0) return true;

                enki.Fired = 1;
                SpawnWaveProjectiles(ref config, ref state, matchState);
                enki.ChannelFramesRemaining = (BoardHelper.CombatHeight - 1) * DefaultMoveInterval;
                return true;
            }

            // 발사 후: 투사체가 보드 끝까지 이동할 때까지 채널링 유지
            enki.ChannelFramesRemaining--;
            if (enki.ChannelFramesRemaining <= 0)
            {
                enki.Channeling = 0;
                return false;
            }

            return true;
        }

        private static void SpawnWaveProjectiles(ref SkillConfig config, ref SkillState state, CombatMatchState matchState)
        {
            ref var enki = ref state.Custom.Enki;
            int healAmount = enki.CachedAttack * config.PowerPercent / 100;
            int hotPerTick = enki.CachedAttack * config.SecondaryPowerPercent / 100;

            int minCol = enki.CenterCol - enki.HalfWidth;
            int maxCol = enki.CenterCol + enki.HalfWidth;

            for (int c = minCol; c <= maxCol; c++)
            {
                if (!BoardHelper.IsValidCombatPosition(c, enki.StartRow)) continue;

                ProjectileSystem.CreateLinearHealProjectile(
                    matchState, enki.CachedCasterCombatId,
                    (byte)c, (byte)enki.StartRow,
                    0, (sbyte)enki.WaveDirRow,
                    healAmount, DamageType.Physical,
                    DefaultMoveInterval, BoardHelper.CombatHeight, config.SkillId,
                    skillVfxIndex: 1,
                    hotPerTick: hotPerTick, hotDuration: config.HotDuration, hotInterval: config.HotInterval,
                    areaEffectHalfWidth: c == enki.CenterCol ? (byte)enki.HalfWidth : (byte)0);
            }
        }
    }
}
