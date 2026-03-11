namespace CookApps.AutoChess
{
    /// <summary>오데트: 2단계 채널링 — 1단계 L자형 범위공격+순간이동, 2단계 3×3 범위공격, 양쪽 공속감소 디버프</summary>
    public class SimSkillOdetteStrike : SimSkillBase
    {
        private int _debuffDurationFrames; // Param0: 공속감소 디버프 지속 프레임
        private int _atkSpeedDownValue;    // Param1: 공속 감소량

        private int _phase;      // 0=대기, 1=1단계 대기, 2=2단계 대기
        private int _phaseTimer;
        private int _dirCol;     // 타겟 방향 col 성분 (-1, 0, +1)
        private int _dirRow;     // 타겟 방향 row 성분 (-1, 0, +1)
        private int _targetCombatId; // 타겟 ID (페이즈간 유지)

        public override bool IsChanneling => true;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _debuffDurationFrames = p.Param0 > 0 ? p.Param0 : 90;  // 기본 3초
            _atkSpeedDownValue = p.Param1 > 0 ? p.Param1 : 30;     // 기본 30
        }

        // 채널링 스킬: Execute 즉시 호출 → 채널링 중 SkillHitFrames 타이밍으로 히트
        public override int GetCastFrames() => 0;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _targetCombatId = targetCombatId;

            // 타겟 위치 기준으로 방향 계산
            int targetIdx = state.FindUnitIndex(targetCombatId);
            if (targetIdx >= 0)
            {
                ref var target = ref state.Units[targetIdx];
                int dc = target.GridCol - caster.GridCol;
                int dr = target.GridRow - caster.GridRow;
                _dirCol = dc > 0 ? 1 : dc < 0 ? -1 : 0;
                _dirRow = dr > 0 ? 1 : dr < 0 ? -1 : 0;
            }
            else
            {
                // 타겟을 못 찾으면 팀 기본 방향
                _dirCol = 0;
                _dirRow = caster.TeamIndex == 0 ? 1 : -1;
            }

            _phase = 1;
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 15; // fallback
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_phase <= 0) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            if (_phase == 1)
            {
                DoPhase1(state, ref caster);
                _phase = 2;
                _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 1
                    ? SkillHitFrames[1] - SkillHitFrames[0]
                    : 19; // fallback
                return true;
            }

            if (_phase == 2)
            {
                DoPhase2(state, ref caster);
                _phase = 0;
                return false;
            }

            return false;
        }

        /// <summary>1단계: ㄷ자형 2×3 범위 공격 + 공속감소 (타겟 방향 기준)</summary>
        private void DoPhase1(CombatMatchState state, ref CombatUnit caster)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            byte team = caster.TeamIndex;

            // ㄷ자형 범위 2×3: 타겟 방향 기준
            // row 방향이 주축이면: 본인 행 col±1 + 전방 1행 col±1
            // col 방향이 주축이면(dirRow==0): 본인 col row±1 + 전방 1열 row±1
            bool rowDominant = _dirRow != 0;

            for (int i = 0; i < state.UnitCount; i++)
            {
                ref var unit = ref state.Units[i];
                if (!unit.IsAlive || unit.TeamIndex == team) continue;

                int uc = unit.GridCol;
                int ur = unit.GridRow;

                bool inRange;
                if (rowDominant)
                {
                    // 가로 3칸 × 세로 2칸 (본인행 + 전방1행)
                    if (uc < col - 1 || uc > col + 1) continue;
                    int rd = (ur - row) * _dirRow;
                    inRange = rd >= 0 && rd <= 1;
                }
                else
                {
                    // 세로 3칸 × 가로 2칸 (본인열 + 전방1열)
                    if (ur < row - 1 || ur > row + 1) continue;
                    int cd = (uc - col) * _dirCol;
                    inRange = cd >= 0 && cd <= 1;
                }
                if (!inRange) continue;

                int raw = attack * PowerPercent / 100;
                int dmg = DamageSystem.CalculateDamage(raw, DamageType, ref unit);
                DamageSystem.ApplyDamage(state, ref unit, dmg);
                DamageSystem.ChargeMana(ref unit, DamageSystem.ManaGainOnHit);

                // 공속감소 디버프
                SkillBuffHelper.ApplyTimedDebuff(state, i,
                    StatModType.AttackSpeed, -_atkSpeedDownValue, _debuffDurationFrames);
            }

            // Phase1 VFX: skill_vfxs[0] (낫 공격) — 타겟 방향 전달
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                dirCol: (sbyte)_dirCol, dirRow: (sbyte)_dirRow);

            // ㄷ자형 범위 타일 이펙트
            state.EventQueue?.PushSkillRectAreaEffect(caster.CombatId, (byte)col, (byte)row,
                (sbyte)_dirCol, (sbyte)_dirRow);

        }

        /// <summary>2단계: 전방 순간이동 후 현재 위치 기준 3×3(체비셰프 반경 1) 범위 공격 + 공속감소</summary>
        private void DoPhase2(CombatMatchState state, ref CombatUnit caster)
        {
            // 전방 2칸 순간이동 (빈 타일일 때만)
            TryTeleportForward(state, ref caster, 2);

            int col = caster.GridCol;
            int row = caster.GridRow;
            int attack = caster.Attack;
            byte team = caster.TeamIndex;

            // 범위 이펙트 이벤트 (3×3 네모)
            state.EventQueue?.PushSkillAreaEffect(caster.CombatId, (byte)col, (byte)row, 1, isBox: true);

            // Phase2 VFX: skill_vfxs[1] (폭발)
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1);

            // 3×3 범위 (체비셰프 거리 1)
            SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, 1,
                (ref CombatUnit enemy, int idx) =>
                {
                    int raw = attack * PowerPercent / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, DamageType, ref enemy);
                    DamageSystem.ApplyDamage(state, ref enemy, dmg);
                    DamageSystem.ChargeMana(ref enemy, DamageSystem.ManaGainOnHit);

                    // 공속감소 디버프
                    SkillBuffHelper.ApplyTimedDebuff(state, idx,
                        StatModType.AttackSpeed, -_atkSpeedDownValue, _debuffDurationFrames);
                });
        }

        /// <summary>타겟 방향으로 N칸 순간이동 (빈 타일일 때만)</summary>
        private void TryTeleportForward(CombatMatchState state, ref CombatUnit caster, int distance)
        {
            int col = caster.GridCol;
            int row = caster.GridRow;
            int destCol = col + _dirCol * distance;
            int destRow = row + _dirRow * distance;

            // 목표 위치가 유효하고 비어있으면 이동
            if (!BoardHelper.IsValidCombatPosition(destCol, destRow) ||
                state.GetUnitAtGrid(destCol, destRow) != CombatUnit.InvalidId)
            {
                return;
            }

            // 그리드 업데이트
            state.ClearGridMulti(caster.GridCol, caster.GridRow, 1, 1);
            caster.GridCol = (byte)destCol;
            caster.GridRow = (byte)destRow;
            state.SetGridMulti(destCol, destRow, 1, 1, caster.CombatId);

            // 뷰 동기화 이벤트
            state.EventQueue?.PushUnitMoved(caster.SourceEntityId, (byte)destCol, (byte)destRow);
        }

        public override void Reset()
        {
            _phase = 0;
            _phaseTimer = 0;
            _dirCol = 0;
            _dirRow = 0;
            _targetCombatId = 0;
        }
    }
}
