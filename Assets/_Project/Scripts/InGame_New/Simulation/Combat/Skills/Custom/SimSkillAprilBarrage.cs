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
        private int _dirRow;

        public override bool IsChanneling => _remainingHits > 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _rate1 = p.Param0 > 0 ? p.Param0 : PowerPercent;
            _rate2 = p.Param1 > 0 ? p.Param1 : PowerPercent;
            _rate3 = p.Param2 > 0 ? p.Param2 : PowerPercent;
        }

        public override int GetCastFrames() => CastFrames > 0 ? CastFrames : 90;

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 전방 방향 결정 (팀 기준)
            _dirRow = caster.TeamIndex == 0 ? 1 : -1;

            int totalHits = HitCount;
            int totalFrames = GetCastFrames();
            _tickInterval = totalHits > 1 ? totalFrames / totalHits : totalFrames;
            _remainingHits = totalHits - 1; // 첫 히트는 지금 실행
            _tickTimer = _tickInterval;

            // 첫 히트 실행
            DoBarrageHit(state, ref caster);
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_remainingHits <= 0) return false;

            _tickTimer--;
            if (_tickTimer > 0) return true;

            // 히트 실행
            _tickTimer = _tickInterval;
            DoBarrageHit(state, ref caster);
            _remainingHits--;

            return _remainingHits > 0;
        }

        private void DoBarrageHit(CombatMatchState state, ref CombatUnit caster)
        {
            int attack = caster.Attack;
            byte team = caster.TeamIndex;
            int col = caster.GridCol;
            int row = caster.GridRow;

            // 전방 4행 확산 범위 순회
            for (int dist = 1; dist <= 4; dist++)
            {
                int targetRow = row + _dirRow * dist;
                int halfWidth = dist - 1;

                // 타일 이펙트 이벤트 (행 단위)
                if (BoardHelper.IsValidCombatPosition(col, targetRow))
                {
                    state.EventQueue?.PushSkillAreaEffect(
                        caster.SourceEntityId, (byte)col, (byte)targetRow, halfWidth, isRow: true);
                }

                // 거리별 배율 결정
                int rate;
                if (dist <= 2) rate = _rate1;
                else if (dist == 3) rate = _rate2;
                else rate = _rate3;

                int raw = attack * rate / 100 / HitCount;

                // 확산 너비: dist=1 → 1칸, dist=2 → 3칸, dist=3 → 5칸, dist=4 → 7칸
                for (int dc = -halfWidth; dc <= halfWidth; dc++)
                {
                    int targetCol = col + dc;
                    if (!BoardHelper.IsValidCombatPosition(targetCol, targetRow)) continue;

                    int combatId = state.GetUnitAtGrid(targetCol, targetRow);
                    if (combatId == CombatUnit.InvalidId) continue;

                    int idx = state.FindUnitIndex(combatId);
                    if (idx < 0) continue;

                    ref var target = ref state.Units[idx];
                    if (!target.IsAlive) continue;
                    if (target.TeamIndex == team) continue;

                    int dmg = DamageSystem.CalculateDamage(raw, DamageType, ref caster, ref target);
                    DamageSystem.ApplyDamage(state, ref target, dmg);
                    DamageSystem.ChargeMana(ref target, DamageSystem.ManaGainOnHit);
                }
            }
        }

        public override void Reset()
        {
            _remainingHits = 0;
            _tickTimer = 0;
            _tickInterval = 0;
            _dirRow = 0;
        }
    }
}
