namespace CookApps.AutoChess
{
    /// <summary>
    /// SHARPSHOOTER 패시브: 확률적으로 방어 완전 관통.
    /// OnPreAttack에서 AtkPierce/ResPierce를 100으로 설정 + ProjectileVfxOverride.
    /// OnPostAttack에서 복원.
    /// View에서는 ProjectileVfxOverride로 투사체 VFX 프리팹을 교체.
    /// </summary>
    public sealed class SharpshooterPierceTrait : CombatTraitBase
    {
        private readonly int _chancePercent;
        private int _savedAtkPierce;
        private int _savedResPierce;
        private bool _pierceActive;

        public SharpshooterPierceTrait(int chancePercent)
        {
            _chancePercent = chancePercent;
        }

        public override void OnPreAttack(CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target)
        {
            _pierceActive = false;
            if (_chancePercent <= 0) return;

            if (state.Rng.Chance(_chancePercent))
            {
                _savedAtkPierce = attacker.AtkPierce;
                _savedResPierce = attacker.ResPierce;
                attacker.AtkPierce = 100;
                attacker.ResPierce = 100;
                _pierceActive = true;

                // 투사체 VFX 오버라이드 (View에서 소비)
                // TODO: AD/AP 구분은 DamageType 기반으로 확장 가능
                attacker.ProjectileVfxOverride = ProjectileVfxId.SharpshooterAD;
            }
        }

        public override void OnPostAttack(CombatMatchState state, ref CombatUnit attacker, ref CombatUnit target)
        {
            if (_pierceActive)
            {
                attacker.AtkPierce = _savedAtkPierce;
                attacker.ResPierce = _savedResPierce;
                _pierceActive = false;
                // ProjectileVfxOverride는 BasicAttack에서 이미 소비됨
            }
        }

        public override void Reset()
        {
            _pierceActive = false;
        }
    }
}
