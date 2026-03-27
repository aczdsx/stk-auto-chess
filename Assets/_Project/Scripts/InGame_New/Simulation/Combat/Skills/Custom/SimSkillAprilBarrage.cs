using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>에이프릴: 채널링 다단히트 — 전방 확산 범위, 거리별 배율 차등</summary>
    public static class AprilSkillLogic
    {
        public static void InitializeFromSpec(ref SimSkillInstance skill, List<SkillActive> specList, int tickRate)
        {
            skill.ExecutionType = SkillExecutionType.Channeling;
            // {0}=쿨타임, {1}=회수, {2}=근거리배율(%), {3}=중거리배율(%), {4}=원거리배율(%)
            skill.HitCount = SkillSpecHelper.GetInt(specList, 1, 10f);
            skill.Rate1 = SkillSpecHelper.GetInt(specList, 2, 100f);
            skill.Rate2 = SkillSpecHelper.GetInt(specList, 3, 75f);
            skill.Rate3 = SkillSpecHelper.GetInt(specList, 4, 50f);
        }

        public static void Execute(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, int targetCombatId, ref DeterministicRNG rng)
        {
            // 타겟 방향 결정 (타겟이 있으면 타겟 기준, 없으면 팀 기준 전방)
            int targetIdx = state.FindUnitIndex(targetCombatId);
            if (targetIdx >= 0)
            {
                ref var target = ref state.Units[targetIdx];
                int dc = target.GridCol - caster.GridCol;
                int dr = target.GridRow - caster.GridRow;
                // 주 방향 결정: row/col 중 변위가 큰 쪽을 주축으로
                if (System.Math.Abs(dr) >= System.Math.Abs(dc))
                {
                    skill.DirRow = dr >= 0 ? 1 : -1;
                    skill.DirCol = 0;
                }
                else
                {
                    skill.DirCol = dc >= 0 ? 1 : -1;
                    skill.DirRow = 0;
                }
            }
            else
            {
                skill.DirRow = caster.TeamIndex == 0 ? 1 : -1;
                skill.DirCol = 0;
            }

            // SkillHitFrames[0]까지 대기 후 첫 히트
            skill.StartDelay = skill.SkillHitFrames != null && skill.SkillHitFrames.Length > 0
                ? skill.SkillHitFrames[0] : 15;
            int channelFrames = skill.SkillClipFrames > skill.StartDelay
                ? skill.SkillClipFrames - skill.StartDelay : 90;
            int totalHits = skill.HitCount;
            skill.TickInterval = totalHits > 1 ? channelFrames / (totalHits - 1) : channelFrames;
            skill.RemainingTicks = totalHits;
            skill.TickTimer = 0;
            skill.Started = false;
            skill.ClipEndTimer = skill.SkillClipFrames > 0
                ? skill.SkillClipFrames : skill.StartDelay + channelFrames;
            skill.HitIndex = 0;
        }

        public static bool OnChannelTick(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster, ref DeterministicRNG rng)
        {
            skill.ClipEndTimer--;

            // SkillHitFrames[0] 타이밍까지 대기
            if (!skill.Started)
            {
                skill.StartDelay--;
                if (skill.StartDelay > 0) return true;

                skill.Started = true;

                // 스킬 VFX 발행 (vfx[0]) — 타겟 방향 전달하여 rotation 적용
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, skill.SkillId, 0,
                    dirCol: (sbyte)skill.DirCol, dirRow: (sbyte)skill.DirRow);

                // 첫 히트 즉시 실행
                DoBarrageHit(ref skill, state, ref caster);
                skill.RemainingTicks--;
                skill.HitIndex++;
                skill.TickTimer = skill.TickInterval;
                return true;
            }

            // 히트가 남아있으면 틱 간격 대기 후 실행
            if (skill.RemainingTicks > 0)
            {
                skill.TickTimer--;
                if (skill.TickTimer <= 0)
                {
                    skill.TickTimer = skill.TickInterval;
                    DoBarrageHit(ref skill, state, ref caster);
                    skill.RemainingTicks--;
                    skill.HitIndex++;
                }
            }

            // SKL 클립 끝까지 채널링 유지
            return skill.ClipEndTimer > 0;
        }

        private static void DoBarrageHit(ref SimSkillInstance skill, CombatMatchState state,
            ref CombatUnit caster)
        {
            int attack = caster.Attack;
            byte team = caster.TeamIndex;
            int col = caster.GridCol;
            int row = caster.GridRow;

            // 주축 방향: DirRow != 0이면 row축 전진 + col축 확산, 아니면 col축 전진 + row축 확산
            bool rowMain = skill.DirRow != 0;

            // 전방 4칸 확산 범위 순회
            for (int dist = 1; dist <= 4; dist++)
            {
                int fwdCol = rowMain ? col : col + skill.DirCol * dist;
                int fwdRow = rowMain ? row + skill.DirRow * dist : row;
                int halfWidth = dist - 1;

                // 타일 이펙트 이벤트 — 3히트마다 발행 (10히트 중 0,3,6,9번째)
                if (skill.HitIndex % 3 == 0 && BoardHelper.IsValidCombatPosition(fwdCol, fwdRow))
                {
                    state.EventQueue?.PushSkillAreaEffect(
                        caster.CombatId, (byte)fwdCol, (byte)fwdRow, halfWidth, isRow: rowMain);
                }

                // 거리별 배율 결정
                int rate;
                if (dist <= 2) rate = skill.Rate1;
                else if (dist == 3) rate = skill.Rate2;
                else rate = skill.Rate3;

                int raw = attack * rate / 100 / skill.HitCount;

                // 확산 너비: dist=1 → 1칸, dist=2 → 3칸, dist=3 → 5칸, dist=4 → 7칸
                for (int d = -halfWidth; d <= halfWidth; d++)
                {
                    int tCol = rowMain ? fwdCol + d : fwdCol;
                    int tRow = rowMain ? fwdRow : fwdRow + d;
                    if (!BoardHelper.IsValidCombatPosition(tCol, tRow)) continue;

                    int combatId = state.GetUnitAtGrid(tCol, tRow);
                    if (combatId == CombatUnit.InvalidId) continue;

                    int idx = state.FindUnitIndex(combatId);
                    if (idx < 0) continue;

                    ref var target = ref state.Units[idx];
                    if (!target.IsAlive) continue;
                    if (target.TeamIndex == team) continue;

                    int dmg = DamageSystem.CalculateDamage(raw, skill.DamageType, ref caster, ref target);
                    DamageSystem.ApplyDamage(state, ref target, dmg);
                    DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
                }
            }
        }
    }
}
