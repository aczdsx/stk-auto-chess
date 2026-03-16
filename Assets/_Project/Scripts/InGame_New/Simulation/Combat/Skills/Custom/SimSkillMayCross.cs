using System.Collections.Generic;
using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// 메이 (215322201): 자기 중심 십자 범위 데미지 + 넉백 + 방어 버프.
    /// DelayedApply — Execute에서 시전 VFX → SkillHitFrames[0] 타이밍에 효과 적용.
    ///
    /// 1. 십자 범위(Plus 형태, 맨해튼 1) 적에게 데미지 + 넉백 1타일
    /// 2. 자기에게 Def 버프 (Param0 프레임, Param1%)
    /// </summary>
    public class SimSkillMayCross : SimSkillBase
    {
        private const int KnockbackDistance = 1;

        private int _buffDurationFrames;
        private int _defBuffPercent;
        private int _worldTickRate;

        public override SkillExecutionType ExecutionType => SkillExecutionType.DelayedApply;

        public override void InitializeFromSpec(SkillParams baseParams, List<SkillActive> specList, int tickRate)
        {
            base.Initialize(baseParams);
            // {0}=쿨타임, {1}=데미지배율(%)→PowerPercent, {2}=버프시간(초), {3}=방어력버프율(%)
            PowerPercent = SkillSpecHelper.GetInt(specList, 1, 200f);
            _buffDurationFrames = SkillSpecHelper.GetFrames(specList, 2, 4f, tickRate);
            _defBuffPercent = SkillSpecHelper.GetInt(specList, 3, 50f) * 100;
            _worldTickRate = tickRate;
        }

        public override int SelectTarget(CombatMatchState state, ref CombatUnit caster)
        {
            return caster.CombatId;
        }

        public override void Execute(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            state.EventQueue?.PushSkillPhaseVfx(caster.CombatId, SkillId, 0);
        }

        protected override void ApplySkillEffect(CombatMatchState state, ref CombatUnit caster,
            int targetCombatId, ref DeterministicRNG rng)
        {
            int casterIdx = state.FindUnitIndex(caster.CombatId);
            if (casterIdx < 0) return;

            int attack = caster.Attack;
            int power = PowerPercent;
            var dmgType = DamageType;
            byte team = caster.TeamIndex;
            int casterCol = caster.GridCol;
            int casterRow = caster.GridRow;

            // 1. 십자 범위(Plus 형태, 맨해튼 1) 적에게 데미지 + 넉백
            SkillAreaHelper.ForEachEnemyInPlus(state, team,
                casterCol, casterRow, 1,
                (ref CombatUnit t, int i) =>
                {
                    int raw = attack * power / 100;
                    int dmg = DamageSystem.CalculateDamage(raw, dmgType, ref state.Units[casterIdx], ref t);
                    DamageSystem.ApplyDamage(state, ref t, dmg);
                    DamageSystem.ChargeMana(ref t, t.ManaGainOnHit);

                    if (!t.IsAlive) return;

                    int dirCol = t.GridCol - casterCol;
                    int dirRow = t.GridRow - casterRow;
                    dirCol = dirCol > 0 ? 1 : dirCol < 0 ? -1 : 0;
                    dirRow = dirRow > 0 ? 1 : dirRow < 0 ? -1 : 0;
                    if (dirCol == 0 && dirRow == 0) dirCol = team == 0 ? 1 : -1;

                    SkillCCHelper.Knockback(state, ref t, dirCol, dirRow, KnockbackDistance, _worldTickRate);
                });

            // 2. 자기에게 Def 버프
            if (_defBuffPercent > 0 && _buffDurationFrames > 0)
            {
                int defBonus = caster.Def * _defBuffPercent / 100;
                if (defBonus > 0)
                    SkillBuffHelper.ApplyTimedBuff(state, casterIdx,
                        StatModType.Def, defBonus, _buffDurationFrames);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _buffDurationFrames = 0;
            _defBuffPercent = 0;
        }
    }
}
