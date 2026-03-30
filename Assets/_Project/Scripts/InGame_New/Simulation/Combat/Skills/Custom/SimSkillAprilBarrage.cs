using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>에이프릴: 채널링 다단히트 — 전방 확산 범위, 거리별 배율 차등</summary>
    public static class AprilSkillLogic
    {
        public static void InitializeFromSpec(ref SkillConfig config, List<SkillActive> specList, int tickRate)
        {
            config.ExecutionType = SkillExecutionType.Channeling;
            // {0}=쿨타임, {1}=회수, {2}=근거리배율(%), {3}=중거리배율(%), {4}=원거리배율(%)
            config.HitCount = SkillSpecHelper.GetInt(specList, 1, 10f);
            config.Rate1 = SkillSpecHelper.GetInt(specList, 2, 100f);
            config.Rate2 = SkillSpecHelper.GetInt(specList, 3, 75f);
            config.Rate3 = SkillSpecHelper.GetInt(specList, 4, 50f);
        }

        public static void Execute(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            ref var april = ref state.Custom.April;

            // 타겟 방향 결정 (타겟이 있으면 타겟 기준, 없으면 팀 기준 전방)
            int targetIdx = matchState.FindUnitIndex(targetCombatId);
            if (targetIdx >= 0)
            {
                ref var target = ref matchState.Units[targetIdx];
                int dc = target.GridCol - caster.GridCol;
                int dr = target.GridRow - caster.GridRow;
                // 주 방향 결정: row/col 중 변위가 큰 쪽을 주축으로
                if (System.Math.Abs(dr) >= System.Math.Abs(dc))
                {
                    april.DirRow = dr >= 0 ? 1 : -1;
                    april.DirCol = 0;
                }
                else
                {
                    april.DirCol = dc >= 0 ? 1 : -1;
                    april.DirRow = 0;
                }
            }
            else
            {
                april.DirRow = caster.TeamIndex == 0 ? 1 : -1;
                april.DirCol = 0;
            }

            // SkillHitFrames[0]까지 대기 후 첫 히트
            state.StartDelay = config.SkillHitFrames != null && config.SkillHitFrames.Length > 0
                ? config.SkillHitFrames[0] : 15;
            int channelFrames = config.SkillClipFrames > state.StartDelay
                ? config.SkillClipFrames - state.StartDelay : 90;
            int totalHits = config.HitCount;
            state.TickInterval = totalHits > 1 ? channelFrames / (totalHits - 1) : channelFrames;
            state.RemainingTicks = totalHits;
            state.TickTimer = 0;
            april.Started = 0;
            april.ClipEndTimer = config.SkillClipFrames > 0
                ? config.SkillClipFrames : state.StartDelay + channelFrames;
            april.HitIndex = 0;
        }

        public static bool OnChannelTick(ref SkillConfig config, ref SkillState state, CombatMatchState matchState,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            ref var april = ref state.Custom.April;
            april.ClipEndTimer--;

            // SkillHitFrames[0] 타이밍까지 대기
            if (april.Started == 0)
            {
                state.StartDelay--;
                if (state.StartDelay > 0) return true;

                april.Started = 1;

                // 스킬 VFX 발행 (vfx[0]) — 타겟 방향 전달하여 rotation 적용
                matchState.EventQueue?.PushSkillPhaseVfx(caster.CombatId, config.SkillId, 0,
                    dirCol: (sbyte)april.DirCol, dirRow: (sbyte)april.DirRow);

                // 첫 히트 즉시 실행
                DoBarrageHit(ref config, ref state, matchState, ref caster);
                state.RemainingTicks--;
                april.HitIndex++;
                state.TickTimer = state.TickInterval;
                return true;
            }

            // 히트가 남아있으면 틱 간격 대기 후 실행
            if (state.RemainingTicks > 0)
            {
                state.TickTimer--;
                if (state.TickTimer <= 0)
                {
                    state.TickTimer = state.TickInterval;
                    DoBarrageHit(ref config, ref state, matchState, ref caster);
                    state.RemainingTicks--;
                    april.HitIndex++;
                }
            }

            // SKL 클립 끝까지 채널링 유지
            return april.ClipEndTimer > 0;
        }

        private static void DoBarrageHit(ref SkillConfig config, ref SkillState state,
            CombatMatchState matchState, ref CombatUnit caster)
        {
            ref var april = ref state.Custom.April;
            int attack = caster.Attack;
            byte team = caster.TeamIndex;
            int col = caster.GridCol;
            int row = caster.GridRow;

            // 주축 방향: DirRow != 0이면 row축 전진 + col축 확산, 아니면 col축 전진 + row축 확산
            bool rowMain = april.DirRow != 0;

            // 전방 4칸 확산 범위 순회
            for (int dist = 1; dist <= 4; dist++)
            {
                int fwdCol = rowMain ? col : col + april.DirCol * dist;
                int fwdRow = rowMain ? row + april.DirRow * dist : row;
                int halfWidth = dist - 1;

                // 타일 이펙트 이벤트 — 3히트마다 발행 (10히트 중 0,3,6,9번째)
                if (april.HitIndex % 3 == 0 && BoardHelper.IsValidCombatPosition(fwdCol, fwdRow))
                {
                    matchState.EventQueue?.PushSkillAreaEffect(
                        caster.CombatId, (byte)fwdCol, (byte)fwdRow, halfWidth, isRow: rowMain);
                }

                // 거리별 배율 결정
                int rate;
                if (dist <= 2) rate = config.Rate1;
                else if (dist == 3) rate = config.Rate2;
                else rate = config.Rate3;

                int raw = attack * rate / 100 / config.HitCount;

                // 확산 너비: dist=1 → 1칸, dist=2 → 3칸, dist=3 → 5칸, dist=4 → 7칸
                for (int d = -halfWidth; d <= halfWidth; d++)
                {
                    int tCol = rowMain ? fwdCol + d : fwdCol;
                    int tRow = rowMain ? fwdRow : fwdRow + d;
                    if (!BoardHelper.IsValidCombatPosition(tCol, tRow)) continue;

                    int combatId = matchState.GetUnitAtGrid(tCol, tRow);
                    if (combatId == CombatUnit.InvalidId) continue;

                    int idx = matchState.FindUnitIndex(combatId);
                    if (idx < 0) continue;

                    ref var target = ref matchState.Units[idx];
                    if (!target.IsAlive) continue;
                    if (target.TeamIndex == team) continue;

                    int dmg = DamageSystem.CalculateDamage(raw, config.DamageType, ref caster, ref target);
                    DamageSystem.ApplyDamage(matchState, ref target, dmg);
                    DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
                }
            }
        }
    }
}
