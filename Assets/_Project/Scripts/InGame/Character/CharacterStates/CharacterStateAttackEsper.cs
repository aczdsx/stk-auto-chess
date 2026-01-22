using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;


public class SpecialAttackDamageInfo
{
    public CharacterController.DamageInfo DamageInfo;
    public bool IsSpecialAttack;
}

/// <summary>
/// 일반 attackstate와 달리 damage 계산 후 animation callback처리.
/// 평타 공격 시 {0}% 확률로 폭발 3x3
/// 단지 스페셜 평타라면 투사체 자체가 달라야하기때문에 정의함.
/// </summary>
public class CharacterStateAttackEsper : CharacterStateAttack
{
    private EffectCodeJobPassiveEsper _passiveEsperEffectCode = null;
    private SpecialAttackDamageInfo _specialAttackInfo = new SpecialAttackDamageInfo();
    public override void StateInit(object owner)
    {
        base.StateInit(owner);
        _passiveEsperEffectCode = characCtrl.GetEffectCodeContainer().GetEffectCode((int)EffectCodeNameType.JOBS_ESPER) as EffectCodeJobPassiveEsper;
    }
    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (_passiveEsperEffectCode == null)
        {
            _passiveEsperEffectCode = characCtrl.GetEffectCodeContainer().GetEffectCode((int)EffectCodeNameType.JOBS_ESPER) as EffectCodeJobPassiveEsper;
        }

        var outRunningResult = base.CharacterStateRunning(dt);
        if (outRunningResult == CharacterStateRunningResult.CanCallAllWithoutMove)
        {
            PreCalculateDamageInfo();
        }

        return outRunningResult;
    }

    private void PreCalculateDamageInfo()
    {
        _specialAttackInfo.IsSpecialAttack = _passiveEsperEffectCode.IsExplosionDamage();

        if (characCtrl.SpecCharacter.atk_type == AtkType.AD)
        {
            _specialAttackInfo.DamageInfo = characCtrl.CalculateDamageAmount(characCtrl.AD, 0, characCtrl.Target, 0, false);
        }
        else
        {
            _specialAttackInfo.DamageInfo = characCtrl.CalculateDamageAmount(0, characCtrl.AP, characCtrl.Target, 0, false);
        }
    }

    public override void AnimationEventCallback(AnimationKey animName, AnimationEventKey eventKey)
    {
        if (animName != AnimationKey.ATK)
        {
            return;
        }

        if (eventKey == AnimationEventKey.End)
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(1f);
            isAttackAnimRunning = false;
            return;
        }

        if (characCtrl.Target == null)
            return;

        if (!characCtrl.Target.IsAlive)
            return;

        if (AnimationEventKey.ExecuteStart < eventKey && eventKey < AnimationEventKey.ExecuteEnd)
        {
            int hitCount = eventKey - AnimationEventKey.ExecuteStart;

            // damage 계산

            if (hitCount > 1)
            {
                _specialAttackInfo.DamageInfo.damageAmount /= hitCount;
            }

            InGameVfxNameType projectile = characCtrl.SpecCharacter.projectile_vfx_name_type;

            if (projectile != InGameVfxNameType.NONE)
            {
                var vfxProjectile = CreateProjectileVfx(projectile, 50f, (vfx) =>
                {
                    OnAttackEndProcess();
                    if (characCtrl != null && characCtrl.Target != null)
                        characCtrl.Target.GetDamaged(_specialAttackInfo.DamageInfo, characCtrl);
                    vfx.Remove();
                });
            }
            else
            {
                OnAttackEndProcess();
                characCtrl.Target.GetDamaged(_specialAttackInfo.DamageInfo, characCtrl);
            }
        }
    }

    protected override void OnAttackEndProcess()
    {
        base.OnAttackEndProcess();
        if (characCtrl == null || characCtrl.Target == null || !characCtrl.Target.IsAlive)
            return;

        if (_specialAttackInfo.IsSpecialAttack)
        {
            _passiveEsperEffectCode.ProgressExplosionDamage(characCtrl.Target);
        }
    }
}
