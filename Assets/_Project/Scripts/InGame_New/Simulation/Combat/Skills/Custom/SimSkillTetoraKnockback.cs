namespace CookApps.AutoChess
{
    /// <summary>
    /// 테토라: 단일 데미지 + 넉백
    /// 채널링 스킬 — Execute 즉시 → SkillHitFrames[0] 타이밍에 효과 적용.
    /// - vfx[0]: 대검 휘두르기 (caster→target 방향 rotation + flip)
    /// - 충돌 시: 착지 지점 AoE 데미지 + 스턴 + vfx[1]
    /// - 미충돌 시: 추가 효과 없음 (타격딜만)
    /// </summary>
    public class SimSkillTetoraKnockback : SimSkillBase
    {
        private int _knockbackDistance;
        private int _stunAoERange;
        private int _worldTickRate;

        // 채널링 상태
        private int _cachedTargetId;
        private int _phaseTimer;
        private bool _fired;

        public override bool IsChanneling => true;
        public override int GetCastFrames() => 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _knockbackDistance = p.Param0 > 0 ? p.Param0 : 4;
            _stunAoERange = p.Param1 > 0 ? p.Param1 : 1;
            _worldTickRate = p.WorldTickRate;
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
                : 15; // fallback 0.5초@30fps
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_fired) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            // SkillHitFrames[0] 타이밍 도달 → 효과 적용
            _fired = true;
            ApplyKnockback(state, ref caster);
            return false;
        }

        private void ApplyKnockback(CombatMatchState state, ref CombatUnit caster)
        {
            int idx = state.FindUnitIndex(_cachedTargetId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            // caster → target 방향 계산
            int dirCol = target.GridCol - caster.GridCol;
            int dirRow = target.GridRow - caster.GridRow;
            if (System.Math.Abs(dirCol) >= System.Math.Abs(dirRow))
                dirRow = 0;
            else
                dirCol = 0;
            dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
            dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
            if (dirCol == 0 && dirRow == 0)
                dirCol = caster.TeamIndex == 0 ? 1 : -1;

            // vfx[0]: 대검 휘두르기 (caster 위치, target 방향 rotation)
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0,
                dirCol: (sbyte)dirCol, dirRow: (sbyte)dirRow);

            // 데미지
            SkillDamageHelper.DealDamage(state, ref caster, _cachedTargetId, PowerPercent, DamageType);

            // 사망 체크 (DealDamage 이후 인덱스 재검색)
            idx = state.FindUnitIndex(_cachedTargetId);
            if (idx < 0 || !state.Units[idx].IsAlive) return;
            target = ref state.Units[idx];

            // 넉백 (충돌 시 스턴은 Knockback 내부에서 자동 적용)
            int actualMoved = SkillCCHelper.Knockback(state, ref target, dirCol, dirRow, _knockbackDistance, _worldTickRate);
            bool hitWall = actualMoved < _knockbackDistance && actualMoved > 0;

            // 충돌 시: 착지 지점 AoE 데미지 + vfx[1] (스턴은 Knockback에서 처리됨)
            if (hitWall && SecondaryPowerPercent > 0)
            {
                int col = target.GridCol;
                int row = target.GridRow;
                byte team = caster.TeamIndex;
                int attack = caster.Attack;
                int power = SecondaryPowerPercent;
                var type = DamageType;

                // vfx[1]: 착지 지점 AoE VFX
                state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 1,
                    col: (byte)col, row: (byte)row, useGridPos: true);

                int casterIdx = state.FindUnitIndex(caster.CombatId);
                SkillAreaHelper.ForEachEnemyInRadius(state, team, col, row, _stunAoERange,
                    (ref CombatUnit t, int _) =>
                    {
                        int raw = attack * power / 100;
                        int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref t);
                        DamageSystem.ApplyDamage(state, ref t, dmg);
                    });
            }
        }

        public override void Reset()
        {
            _knockbackDistance = 4;
            _stunAoERange = 1;
            _fired = false;
        }
    }
}
