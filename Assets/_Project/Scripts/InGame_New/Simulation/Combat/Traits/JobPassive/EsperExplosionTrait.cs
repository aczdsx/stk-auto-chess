namespace CookApps.AutoChess
{
    /// <summary>
    /// ESPER 패시브: 확률적으로 타겟 주변 3×3 폭발.
    /// OnPostAttack에서 확률 판정 → 즉시 폭발 처리.
    /// 근접/원거리 모두 데미지 적용 후 호출되므로 타이밍 일관.
    /// </summary>
    public sealed class EsperExplosionTrait : CombatTraitBase
    {
        private readonly int _chancePercent;
        private readonly int _damagePercent;

        public EsperExplosionTrait(int chancePercent, int damagePercent)
        {
            _chancePercent = chancePercent;
            _damagePercent = damagePercent > 0 ? damagePercent : 100;
        }

        public override void OnPostAttack(CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target)
        {
            if (_chancePercent <= 0) return;
            if (!target.IsAlive) return;

            if (state.Rng.Chance(_chancePercent))
            {
                int dmgPct = _damagePercent > 255 ? 255 : _damagePercent;
                JobPassiveSystem.ProcessEsperExplosion(state, ref attacker, ref target, dmgPct);
            }
        }
    }
}
