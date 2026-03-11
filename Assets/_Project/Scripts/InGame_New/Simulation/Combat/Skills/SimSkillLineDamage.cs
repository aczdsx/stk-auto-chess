namespace CookApps.AutoChess
{
    /// <summary>직선 관통 데미지 스킬 (Linear 투사체로 한 칸씩 이동하며 피격)</summary>
    public class SimSkillLineDamage : SimSkillBase
    {
        private int _length;
        private int _moveInterval;
        private int _width;

        // 채널링용 상태
        private int _phaseTimer;
        private int _targetCombatId;
        private bool _fired; // 투사체 발사 완료 여부

        public override bool HasProjectile => true;
        public override bool IsChanneling => true;

        // Execute 즉시 호출 → OnChannelTick에서 SkillHitFrames 타이밍에 투사체 생성
        public override int GetCastFrames() => 0;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _length = p.Param0 > 0 ? p.Param0 : 4;
            _moveInterval = p.Param1 > 0 ? p.Param1 : 3; // N프레임마다 1칸 이동
            _width = p.Param2 > 0 ? p.Param2 : 1;         // 투사체 폭 (1 = 1칸, 3 = 3칸)
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return TargetingSystem.FindNearestEnemy(state, ref caster);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            // 준비만: 타겟 저장 + 타이머 설정 (오데트 패턴)
            _targetCombatId = targetCombatId;
            _fired = false;
            _phaseTimer = SkillHitFrames != null && SkillHitFrames.Length > 0
                ? SkillHitFrames[0]
                : 15; // fallback 0.5초
        }

        public override bool OnChannelTick(CombatMatchState state, ref CombatUnit caster, ref DeterministicRNG rng)
        {
            if (_fired) return false;

            _phaseTimer--;
            if (_phaseTimer > 0) return true;

            // SkillHitFrames[0] 타이밍 도달 → 투사체 발사
            _fired = true;
            FireProjectile(state, ref caster);
            return false; // 채널링 종료
        }

        private void FireProjectile(CombatMatchState state, ref CombatUnit caster)
        {
            int idx = state.FindUnitIndex(_targetCombatId);

            // 시전자 → 타겟 방향 계산
            int dirCol, dirRow;
            if (idx >= 0)
            {
                ref var target = ref state.Units[idx];
                int dc = target.GridCol - caster.GridCol;
                int dr = target.GridRow - caster.GridRow;
                dirCol = dc > 0 ? 1 : (dc < 0 ? -1 : 0);
                dirRow = dr > 0 ? 1 : (dr < 0 ? -1 : 0);
            }
            else
            {
                dirCol = 0;
                dirRow = 0;
            }

            // 방향이 없으면 (같은 위치 또는 타겟 사망) 팀에 따라 기본 방향 설정
            if (dirCol == 0 && dirRow == 0)
            {
                dirRow = caster.TeamIndex == 0 ? 1 : -1;
            }

            int raw = caster.Attack * PowerPercent / 100;
            bool isCrit = false;

            ProjectileSystem.CreateLinearProjectile(
                state, caster.CombatId,
                caster.GridCol, caster.GridRow,
                (sbyte)dirCol, (sbyte)dirRow,
                raw, isCrit, DamageType,
                _moveInterval, _length, _width, caster.SkillSpecId);
        }

        public override void Reset()
        {
            _length = 4;
            _moveInterval = 3;
            _width = 1;
            _phaseTimer = 0;
            _targetCombatId = 0;
            _fired = false;
        }
    }
}
