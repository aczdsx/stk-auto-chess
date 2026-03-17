using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>에이프릴: 채널링 다단히트 — 전방 확산 범위, 거리별 배율 차등</summary>
    public class SimSkillAprilBarrage : SimSkillBase
    {
        private int _rate1; // 근거리 배율 (1~2행)
        private int _rate2; // 중거리 배율 (3행)
        private int _rate3; // 원거리 배율 (4+행)

        private int _remainingHits;
        private int _tickInterval;
        private int _tickTimer;
        private int _dirCol; // 타겟 방향 (col)
        private int _dirRow; // 타겟 방향 (row)
        private bool _started; // SkillHitFrames[0] 대기 완료 여부
        private int _startDelay;
        private int _clipEndTimer; // SKL 클립 끝까지 채널링 유지용
        private int _hitIndex; // 타일이펙트 주기 제어용

        public override SkillExecutionType ExecutionType => SkillExecutionType.Channeling;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=회수, {2}=근거리배율(%), {3}=중거리배율(%), {4}=원거리배율(%)
            HitCount = SkillSpecHelper.GetInt(specList, 1, 10f);
            _rate1 = SkillSpecHelper.GetInt(specList, 2, 100f);
            _rate2 = SkillSpecHelper.GetInt(specList, 3, 75f);
            _rate3 = SkillSpecHelper.GetInt(specList, 4, 50f);
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
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
                    _dirRow = dr >= 0 ? 1 : -1;
                    _dirCol = 0;
                }
                else
                {
                    _dirCol = dc >= 0 ? 1 : -1;
                    _dirRow = 0;
                }
            }
            else
            {
                _dirRow = caster.TeamIndex == 0 ? 1 : -1;
                _dirCol = 0;
            }

            // SkillHitFrames[0]까지 대기 후 첫 히트
            _startDelay = SkillHitFrames != null && SkillHitFrames.Length > 0 ? SkillHitFrames[0] : 15;
            int channelFrames = SkillClipFrames > _startDelay ? SkillClipFrames - _startDelay : 90;
            int totalHits = HitCount;
            _tickInterval = totalHits > 1 ? channelFrames / (totalHits - 1) : channelFrames;
            _remainingHits = totalHits;
            _tickTimer = 0;
            _started = false;
            _clipEndTimer = SkillClipFrames > 0 ? SkillClipFrames : _startDelay + channelFrames;
            _hitIndex = 0;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;

            // SkillHitFrames[0] 타이밍까지 대기
            if (!_started)
            {
                _startDelay--;
                if (_startDelay > 0) return true;

                _started = true;

                // 스킬 VFX 발행 (vfx[0]) — 타겟 방향 전달하여 rotation 적용
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                    dirCol: (sbyte)_dirCol, dirRow: (sbyte)_dirRow);

                // 첫 히트 즉시 실행
                DoBarrageHit(state, ref caster);
                _remainingHits--;
                _hitIndex++;
                _tickTimer = _tickInterval;
                return true;
            }

            // 히트가 남아있으면 틱 간격 대기 후 실행
            if (_remainingHits > 0)
            {
                _tickTimer--;
                if (_tickTimer <= 0)
                {
                    _tickTimer = _tickInterval;
                    DoBarrageHit(state, ref caster);
                    _remainingHits--;
                    _hitIndex++;
                }
            }

            // SKL 클립 끝까지 채널링 유지
            return _clipEndTimer > 0;
        }

        private void DoBarrageHit(CombatMatchState state, ref CombatUnit caster)
        {
            int attack = caster.Attack;
            byte team = caster.TeamIndex;
            int col = caster.GridCol;
            int row = caster.GridRow;

            // 주축 방향: _dirRow != 0이면 row축 전진 + col축 확산, 아니면 col축 전진 + row축 확산
            bool rowMain = _dirRow != 0;

            // 전방 4칸 확산 범위 순회
            for (int dist = 1; dist <= 4; dist++)
            {
                int fwdCol = rowMain ? col : col + _dirCol * dist;
                int fwdRow = rowMain ? row + _dirRow * dist : row;
                int halfWidth = dist - 1;

                // 타일 이펙트 이벤트 — 3히트마다 발행 (10히트 중 0,3,6,9번째)
                if (_hitIndex % 3 == 0 && BoardHelper.IsValidCombatPosition(fwdCol, fwdRow))
                {
                    state.EventQueue?.PushSkillAreaEffect(
                        caster.SourceEntityId, (byte)fwdCol, (byte)fwdRow, halfWidth, isRow: rowMain);
                }

                // 거리별 배율 결정
                int rate;
                if (dist <= 2) rate = _rate1;
                else if (dist == 3) rate = _rate2;
                else rate = _rate3;

                int raw = attack * rate / 100 / HitCount;

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

                    int dmg = DamageSystem.CalculateDamage(raw, DamageType, ref caster, ref target);
                    DamageSystem.ApplyDamage(state, ref target, dmg);
                    DamageSystem.ChargeMana(ref target, target.ManaGainOnHit);
                }
            }
        }

        public override void Reset()
        {
            _remainingHits = 0;
            _tickTimer = 0;
            _tickInterval = 0;
            _dirCol = 0;
            _dirRow = 0;
            _started = false;
            _startDelay = 0;
            _clipEndTimer = 0;
            _hitIndex = 0;
        }
    }
}
