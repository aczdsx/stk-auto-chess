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
                var vfxProjectile = CreateProjectileVfx(projectile, 50f, (vfx) =>
                {
                    base.OnAttackEndProcess();
                    vfx.Remove();
                });
            }
            else
            {
                // TODO: Effect
                base.OnAttackEndProcess();
            }
        }

    }



}

