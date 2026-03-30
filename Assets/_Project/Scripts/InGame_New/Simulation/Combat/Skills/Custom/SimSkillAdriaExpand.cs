using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 아드리아 (217523403): 3단계 확장 패턴 AoE + 방어력 비례 데미지 + 스턴.
    /// Phase 0: +(범위1), Phase 1: X(범위1), Phase 2: +(범위2)
    /// 각 Phase는 SkillHitFrames 타이밍에 발동, 이미 맞은 적은 중복 히트 안 함.
    /// 스펙: {0}=쿨타임, {1}=데미지배율(%), {2}=방어력계수, {3}=스턴시간(초)
    /// </summary>
    public static class AdriaSkillLogic
    {
        private const int PhaseCount = 3;
        private const int FallbackDelay = 8; // SkillHitFrames 없을 때 페이즈 간격

        public static void InitializeFromSpec(ref SkillConfig config, List<SkillActive> specList, int tickRate)
        {
            config.ExecutionType = SkillExecutionType.Channeling;
            // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent, {2}=방어력계수, {3}=스턴시간(초)
            config.PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            config.DefScaleValue = SkillSpecHelper.GetInt(specList, 2, 100f);
            config.StunDurationFrames = SkillSpecHelper.GetFrames(specList, 3, 2f, tickRate);
        }

        public static void Execute(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            ref var adria = ref state.Custom.Adria;
            adria.CurrentPhase = 0;
            adria.Done = 0;
            adria.HitMask = 0;

            // 첫 Phase 타이밍 대기 (SkillHitFrames[0])
            state.TickTimer = config.SkillHitFrames != null && config.SkillHitFrames.Length > 0
                ? config.SkillHitFrames[0]
                : FallbackDelay;
        }

        public static bool OnChannelTick(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            ref var adria = ref state.Custom.Adria;
            if (adria.Done != 0) return false;

            // PhaseTimer는 Adria에 없으므로 Enki union의 PhaseTimer 사용 (같은 offset)
            // 주의: Explicit Layout union이므로 Enki.PhaseTimer와 Adria는 같은 메모리
            // 더 안전한 방법: SkillState 공통 필드 사용
            state.TickTimer--;
            if (state.TickTimer > 0) return true;

            // 현재 Phase 실행
            DoPhase(ref config, ref state, matchState, ref caster, adria.CurrentPhase);

            adria.CurrentPhase++;
            if (adria.CurrentPhase >= PhaseCount)
            {
                adria.Done = 1;
                return false;
            }

            // 다음 Phase 타이밍 설정
            if (config.SkillHitFrames != null && adria.CurrentPhase < config.SkillHitFrames.Length)
            {
                int prevFrame = config.SkillHitFrames[adria.CurrentPhase - 1];
                int nextFrame = config.SkillHitFrames[adria.CurrentPhase];
                state.TickTimer = nextFrame > prevFrame ? nextFrame - prevFrame : FallbackDelay;
            }
            else
            {
                state.TickTimer = FallbackDelay;
            }

            return true;
        }

        private static void DoPhase(ref SkillConfig config, ref SkillState state,
            CombatMatchState matchState, ref CombatUnit caster, int phase)
        {
            ref var adria = ref state.Custom.Adria;
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            int def = caster.Def;
            var dmgType = config.DamageType;
            byte team = caster.TeamIndex;
            int power = config.PowerPercent;
            int defScale = config.DefScaleValue;
            int stunFrames = config.StunDurationFrames;
            int casterIdx = matchState.FindUnitIndex(caster.CombatId);

            // Phase별 패턴으로 적 순회
            for (int i = 0; i < matchState.UnitCount; i++)
            {
                ref var unit = ref matchState.Units[i];
                if (!unit.IsAlive) continue;
                if (unit.TeamIndex == team) continue;

                if (!IsInPattern(col, row, unit.GridCol, unit.GridRow, phase))
                    continue;

                // 중복 히트 방지
                int bitIndex = i % 64;
                long bit = 1L << bitIndex;
                if ((adria.HitMask & bit) != 0) continue;
                adria.HitMask |= bit;

                // 데미지: attack * damageRate% * (1 + def / defValue)
                int raw = attack * power / 100 * (defScale + def) / defScale;
                int dmg = DamageSystem.CalculateDamage(raw, dmgType, ref matchState.Units[casterIdx], ref unit);
                DamageSystem.ApplyDamage(matchState, ref unit, dmg);
                DamageSystem.ChargeMana(ref unit, unit.ManaGainOnHit);

                // 스턴
                if (stunFrames > 0)
                    SkillCCHelper.ApplyCC(matchState, ref unit, CrowdControlType.Stun, stunFrames);
            }

            // 패턴 내 모든 타일에 vfx[0] 스폰
            EmitPatternVfx(matchState, ref caster, col, row, phase, config.SkillId);
        }

        /// <summary>패턴에 해당하는 모든 그리드 좌표에 vfx[0] 발행</summary>
        private static void EmitPatternVfx(CombatMatchState state, ref CombatUnit caster,
            int cx, int cy, int phase, int skillId)
        {
            int range = GetPhaseRadius(phase);
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    if (!IsInPattern(cx, cy, cx + dx, cy + dy, phase)) continue;

                    int tx = cx + dx;
                    int ty = cy + dy;
                    if (!BoardHelper.IsValidCombatPosition(tx, ty)) continue;

                    state.EventQueue?.PushSkillPhaseVfx(
                        caster.CombatId, skillId, 0,
                        col: (byte)tx, row: (byte)ty, useGridPos: true);
                }
            }
        }

        /// <summary>Phase별 패턴 판정. Phase 0: +(1), Phase 1: X(1), Phase 2: +(2)</summary>
        private static bool IsInPattern(int cx, int cy, int tx, int ty, int phase)
        {
            int dx = tx - cx;
            int dy = ty - cy;
            int absDx = dx < 0 ? -dx : dx;
            int absDy = dy < 0 ? -dy : dy;

            switch (phase)
            {
                case 0: // + 패턴 (맨해튼 거리 1, 축 정렬만)
                    return (absDx + absDy <= 1) && (absDx + absDy > 0);

                case 1: // X 패턴 (대각선 거리 1)
                    return absDx == 1 && absDy == 1;

                case 2: // + 패턴 (맨해튼 거리 2, 축 정렬만 — 이미 히트된 적 제외는 hitMask에서)
                    return (absDx + absDy <= 2) && (absDx + absDy > 0)
                           && (absDx == 0 || absDy == 0);

                default:
                    return false;
            }
        }

        private static int GetPhaseRadius(int phase)
        {
            switch (phase)
            {
                case 0: return 1;
                case 1: return 1;
                case 2: return 2;
                default: return 1;
            }
        }
    }
}
