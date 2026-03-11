namespace CookApps.AutoChess
{
    /// <summary>패턴 기반 AoE 데미지 + 선택적 CC</summary>
    public class SimSkillPatternDamage : SimSkillBase
    {
        private int _range;

        public override void Initialize(SkillParams p)
        {
            base.Initialize(p);
            _range = p.Param1 > 0 ? p.Param1 : 1;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return SkillAreaHelper.FindBestAoETarget(state, ref caster, _range);
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int idx = state.FindUnitIndex(targetCombatId);
            if (idx < 0) return;
            ref var center = ref state.Units[idx];

            int attack = caster.Attack;
            int power = PowerPercent;
            var type = DamageType;
            byte team = caster.TeamIndex;
            var ccType = CCType;
            int ccFrames = CCDurationFrames;
            int casterIdx = state.FindUnitIndex(caster.CombatId);

            SkillAreaHelper.ForEachEnemyInRadius(state, team,
                center.GridCol, center.GridRow, _range,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, type, ref state.Units[casterIdx], ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, DamageSystem.ManaGainOnHit);

                    if (ccType != CrowdControlType.None && ccFrames > 0)
                    {
                        SkillCCHelper.ApplyCC(state, ref t, ccType, ccFrames);
                    }
                });
        }

        public override void Reset()
        {
            _range = 1;
        }
    }
}