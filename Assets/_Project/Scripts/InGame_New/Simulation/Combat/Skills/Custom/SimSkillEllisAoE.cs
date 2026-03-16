namespace CookApps.AutoChess
{
    /// <summary>
    /// 엘리스 (215642501): 다이아몬드 AoE (맨해튼 거리 1) 마법 데미지.
    /// 2단계: SkillHitFrames[0]에 얼음 vfx[0] 스폰 → 0.8초 후 데미지.
    /// 스펙: {0}=쿨타임, {1}=데미지배율(%) → PowerPercent
    /// Params: Param0=diamond range (기본 1)
    /// </summary>
    public class SimSkillEllisAoE : SimSkillBase
    {
        private const float DamageDelaySec = 0.8f;
        private const int Range = 1; // 맨해튼 거리

        private int _damageDelayFrames;
        private int _cachedTargetId;
        private int _phaseTimer;
        private int _clipEndTimer;
        private byte _phase; // 0=vfx대기, 1=데미지대기, 2=완료

        // vfx 스폰 시 기록한 중심 좌표 (타겟이 이동해도 데미지는 원래 위치)
        private int _centerCol;
        private int _centerRow;

        public override bool IsChanneling => true;
        public override int GetCastFrames() => 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _damageDelayFrames = (int)(DamageDelaySec * p.WorldTickRate + 0.5f);
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            _cachedTargetId = targetCombatId;
            _phase = 0;
            _clipEndTimer = SkillClipFrames > 0 ? SkillClipFrames : 90;

            // Phase 0: SkillHitFrames[0]까지 대기 (얼음 vfx 스폰 타이밍)
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0] : 15;
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            _clipEndTimer--;
            _phaseTimer--;

            if (_phaseTimer > 0)
                return true;

            switch (_phase)
            {
                case 0: // 얼음 vfx 스폰 + 범위 표시
                    SpawnVfx(state, ref caster);
                    // Phase 1: 0.8초 후 데미지
                    _phaseTimer = _damageDelayFrames;
                    _phase = 1;
                    return true;

                case 1: // 데미지 적용
                    ApplyDamage(state, ref caster);
                    _phase = 2;
                    return _clipEndTimer > 0;

                default: // 클립 종료 대기
                    return _clipEndTimer > 0;
            }
        }

        private void SpawnVfx(CombatMatchState state, ref CombatUnit caster)
        {
            int idx = state.FindUnitIndex(_cachedTargetId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            _centerCol = target.GridCol;
            _centerRow = target.GridRow;

            // 타일 이펙트 (범위 표시)
            state.EventQueue?.PushSkillAreaEffect(
                caster.SourceEntityId, (byte)_centerCol, (byte)_centerRow, Range);

            // vfx[0]: 범위 내 각 타일에 개별 스폰
            for (int r = 0; r < BoardHelper.CombatHeight; r++)
                for (int c = 0; c < BoardHelper.CombatWidth; c++)
                {
                    if (BoardHelper.ManhattanDistance(_centerCol, _centerRow, c, r) > Range)
                        continue;
                    state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                        col: (byte)c, row: (byte)r, useGridPos: true);
                }
        }

        private void ApplyDamage(CombatMatchState state, ref CombatUnit caster)
        {
            int attack = caster.Attack;
            int power = PowerPercent;
            var dmgType = DamageType;
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx < 0) return;

            SkillAreaHelper.ForEachEnemyInDiamond(state, caster.TeamIndex,
                _centerCol, _centerRow, Range,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, dmgType, ref state.Units[casterIdx], ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, t.ManaGainOnHit);
                });
        }

        public override void Reset()
        {
            _cachedTargetId = CombatUnit.InvalidId;
            _phaseTimer = 0;
            _clipEndTimer = 0;
            _phase = 0;
            _centerCol = 0;
            _centerRow = 0;
        }
    }
}
