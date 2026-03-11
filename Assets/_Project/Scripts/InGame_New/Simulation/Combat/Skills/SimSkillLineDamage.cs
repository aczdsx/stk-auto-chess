namespace CookApps.AutoChess
{
    /// <summary>직선 관통 데미지 스킬 (Linear 투사체로 한 칸씩 이동하며 피격)</summary>
    public class SimSkillLineDamage : SimSkillBase
    {
        private int _length;
        private int _moveInterval;
        private int _width;

        public override bool HasProjectile => true;

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
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var target = ref state.Units[idx];

            // 시전자 → 타겟 방향 계산 (바라보는 방향으로 발사)
            int dc = target.GridCol - caster.GridCol;
            int dr = target.GridRow - caster.GridRow;
            int dirCol = dc > 0 ? 1 : (dc < 0 ? -1 : 0);
            int dirRow = dr > 0 ? 1 : (dr < 0 ? -1 : 0);

            // 방향이 없으면 (같은 위치) 팀에 따라 기본 방향 설정
            if (dirCol == 0 && dirRow == 0)
            {
                dirRow = caster.TeamIndex == 0 ? 1 : -1;
            }

            int raw = caster.Attack * PowerPercent / 100;
            bool isCrit = false; // TODO: 크리티컬 판정 필요 시 추가

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
        }
    }
}
