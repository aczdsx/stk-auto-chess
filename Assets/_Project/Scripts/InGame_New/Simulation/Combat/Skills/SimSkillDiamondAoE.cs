namespace CookApps.AutoChess
{
    /// <summary>
    /// 블린
    /// 다이아몬드 범위 AoE 데미지 (맨해튼 거리 기반)
    /// 채널링 스킬 — Execute 즉시 → SkillHitFrames[0] 타이밍에 효과 적용.
    /// - 가장 가까운 적 기준 Param0 범위 (기본 2 = 5×5 다이아몬드)
    /// - vfx[0]: 중심 타겟 위치 (1회)
    /// - vfx[1]: 범위 내 각 타일마다 개별 발사
    /// - 타일이펙트: 다이아몬드 범위 표시
    /// </summary>
    public class SimSkillDiamondAoE : SimSkillBase
    {
        private int _areaRange;

        // 채널링 상태
        private int _cachedTargetId;
        private int _phaseTimer;
        private bool _fired;

        public override bool IsChanneling => true;
        public override int GetCastFrames() => 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _areaRange = p.Param0 > 0 ? p.Param0 : 2;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _cachedTargetId = targetCombatId;
            _fired = false;
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 15;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_fired) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            _fired = true;
            ApplyDiamondAoE(state, ref caster);
            return false;
        }

        private void ApplyDiamondAoE(CombatMatchState state, ref CombatUnit caster)
        {
            int idx = state.FindUnitIndex(_cachedTargetId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            int centerCol = target.GridCol;
            int centerRow = target.GridRow;
            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            int casterIdx = state.FindUnitIndex(caster.CombatId);

            // vfx[0]: 중심 타겟 위치
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                col: (byte)centerCol, row: (byte)centerRow, useGridPos: true);

            // 타일이펙트: 다이아몬드 범위 (맨해튼 거리 기반)
            state.EventQueue?.PushSkillAreaEffect(
                caster.SourceEntityId, (byte)centerCol, (byte)centerRow, _areaRange);

            // vfx[1]: 범위 내 각 타일에 개별 발사
            for (int r = 0; r < BoardHelper.CombatHeight; r++)
            {
                for (int c = 0; c < BoardHelper.CombatWidth; c++)
                {
                    if (BoardHelper.ManhattanDistance(centerCol, centerRow, c, r) > _areaRange)
                        continue;
                    state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1,
                        col: (byte)c, row: (byte)r, useGridPos: true);
                }
            }

            // 데미지: 다이아몬드 범위 내 적
            SkillAreaHelper.ForEachEnemyInDiamond(state, team,
                centerCol, centerRow, _areaRange,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, t.ManaGainOnHit);
                });
        }

        public override void Reset()
        {
            _areaRange = 2;
            _fired = false;
        }
    }
}
