namespace CookApps.AutoChess
{
    /// <summary>텔레포트 타격 (시전 시간 동안 무적, 착지 시 AoE + CC)</summary>
    public class SimSkillTeleportStrike : SimSkillBase
    {
        private int _aoeRange;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _aoeRange = p.Param0 > 0 ? p.Param0 : 1;
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

            int centerCol = target.GridCol;
            int centerRow = target.GridRow;
            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            var ccType = CCType;
            int ccFrames = CCDurationFrames;

            SkillAreaHelper.ForEachEnemyInRadius(state, team,
                centerCol, centerRow, _aoeRange,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, DamageSystem.ManaGainOnHit);

                    if (ccType != CrowdControlType.None && ccFrames > 0)
                    {
                        SkillCCHelper.ApplyCC(ref t, ccType, ccFrames);
                    }
                });
        }

        public override void Reset()
        {
            _aoeRange = 1;
        }
    }
}
