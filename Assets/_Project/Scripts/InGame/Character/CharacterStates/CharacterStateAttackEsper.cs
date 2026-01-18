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
                if (characCtrl == null || characCtrl.IsAlive == false)
                {
                    return;
                }

                Transform projectileTransform = characCtrl.GetCharacterView().CachedFront ? characCtrl.GetCharacterView().ProjectileFrontTransform : characCtrl.GetCharacterView().ProjectileBackTransform;

                var vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(projectile,
                    projectileTransform.position);

                var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();

                Vector3 direction =
                    (characCtrl.Target.CurrentTile.View.CachedTr.position -
                     characCtrl.CurrentTile.View.CachedTr.position).normalized;
                vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction);

                movement.SetData(vfxProjectile.CachedTr.position,
                    characCtrl.Target.GetCharacterView().CachedTr.position, 50);
                vfxProjectile.Initialize(false, movement);

                void OnReachedTargetHandler()
                {
                    OnAttackEndProcess();

                    if (characCtrl != null && characCtrl.Target != null)
                        characCtrl.Target.GetDamaged(_specialAttackInfo.DamageInfo, characCtrl);
                    vfxProjectile.Remove();
                }

                movement.OnReachedTarget += OnReachedTargetHandler;
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
        _passiveEsperEffectCode.ProgressExplosionDamage(characCtrl.Target);

        // if (_specialAttackInfo.IsSpecialAttack)
        // {
        // }
    }
}
