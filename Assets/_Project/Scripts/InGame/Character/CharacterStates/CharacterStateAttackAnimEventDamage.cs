using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class CharacterStateAttackAnimEventDamage : CharacterStateAttack
{
    private int _currentHitCount = 0;// 공격 애니메이션 총 히트 수
    public override void StateInit(object owner)
    {
        base.StateInit(owner);
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
        else if (eventKey == AnimationEventKey.Start){
            _currentHitCount = 0;
        }

        if (characCtrl.Target == null)
            return;

        if (!characCtrl.Target.IsAlive)
            return;

        if (eventKey is > AnimationEventKey.ExecuteStart and < AnimationEventKey.ExecuteEnd)
        {
            // damage 계산
            CharacterController.DamageInfo damageInfo = CalculateNormalAttackDamage();

            // CharacterStateSkill처럼 이벤트 계산

            var totalHitCount = eventKey - AnimationEventKey.Execute1Per1;

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
                    characCtrl.Target.GetCharacterView().CachedTr.position, _vfxProjectileSpeed);
                vfxProjectile.Initialize(false, movement);

                void OnReachedTargetHandler()
                {
                    OnAttackEndProcess();
                    var ctrlEcc = characCtrl?.GetEffectCodeContainer();
                    var effectCodes = ctrlEcc?.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnStateNormalAttackDamageEvent);
                    EffectCodeForLoopHelper.CallWithArgs(effectCodes,
                    EffectCodeCharacterLambda.CallOnStateNormalAttackDamageEventLambda,
                    damageInfo, _currentHitCount++, totalHitCount);


                    vfxProjectile.Remove();
                }

                movement.OnReachedTarget += OnReachedTargetHandler;
            }
            else
            {
                OnAttackEndProcess();

                var ctrlEcc = characCtrl?.GetEffectCodeContainer();
                var effectCodes = ctrlEcc?.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnStateNormalAttackDamageEvent);
                EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnStateNormalAttackDamageEventLambda,
                damageInfo, _currentHitCount++, totalHitCount);
            }
        }
    }
    


}
