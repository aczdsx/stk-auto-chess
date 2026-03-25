using UnityEngine;

namespace CookApps.AutoChess
{
    /// <summary>
    /// GUARDIAN 패시브: 쿨타임마다 일반공격 N회 무시 베리어.
    /// OnTick에서 타이머 누적, 쿨타임 도달 시 쉴드 충전.
    /// ModifyIncomingDamage에서 일반공격이면 데미지 0으로 만들고 충전 차감.
    /// </summary>
    public sealed class GuardianEndureTrait : CombatTraitBase
    {
        private readonly int _cooldownFrames;
        private readonly int _maxCharges;
        private int _timer;
        private int _shieldCharges;

        public GuardianEndureTrait(int cooldownFrames, int maxCharges = 3)
        {
            _cooldownFrames = cooldownFrames > 0 ? cooldownFrames : 1;
            _maxCharges = maxCharges > 0 ? maxCharges : 3;
        }

        public override void OnCombatStart(CombatMatchState state, ref CombatUnit owner)
        {
            _shieldCharges = _maxCharges;
            _timer = 0;
            state.EventQueue?.PushStatusEffectAdded(owner.CombatId, CombatVfxType.BasicAttackShield, _shieldCharges);
        }

        public override void OnTick(CombatMatchState state, ref CombatUnit owner, int tickRate)
        {
            _timer++;
            if (_timer >= _cooldownFrames)
            {
                _shieldCharges = _maxCharges;
                _timer = 0;
                state.EventQueue?.PushStatusEffectAdded(owner.CombatId, CombatVfxType.BasicAttackShield, _shieldCharges);
            }
        }

        public override int ModifyIncomingDamage(CombatMatchState state, ref CombatUnit attacker,
            ref CombatUnit target, int damage, DamageType damageType, bool isBasicAttack = false)
        {
            if (!isBasicAttack) return damage;
            if (_shieldCharges <= 0) return damage;

            _shieldCharges--;

            if (_shieldCharges <= 0)
                state.EventQueue?.PushStatusEffectRemoved(target.CombatId, CombatVfxType.BasicAttackShield);
            else
                state.EventQueue?.PushStatusEffectAdded(target.CombatId, CombatVfxType.BasicAttackShield, _shieldCharges);

            return 0;
        }

        public override void Reset()
        {
            _timer = 0;
            _shieldCharges = 0;
        }
    }
}
