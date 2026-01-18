using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;

public class CharacterStateAttackHealer : CharacterStateAttack
{
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
                    base.OnAttackEndProcess();
                    vfxProjectile.Remove();
                }

                movement.OnReachedTarget += OnReachedTargetHandler;
            }
            else
            {
                // TODO: Effect
                base.OnAttackEndProcess();
            }
        }

    }



}

