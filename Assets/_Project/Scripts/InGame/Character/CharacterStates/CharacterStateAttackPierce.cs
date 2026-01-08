using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;


/// <summary>
/// 일반 attackstate와 달리 damage 계산 후 animation callback처리.
/// 평타 공격 시 {0}% 확률로 상대방의 물방 마방 관통
/// 단지 스페셜 평타라면 투사체 자체가 달라야하기때문에 정의함.
/// </summary>
public class CharacterStateAttackPierce : CharacterStateAttack
{
    private SpecialAttackDamageInfo _specialAttackInfo = new SpecialAttackDamageInfo();
    
    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        var outRunningResult = base.CharacterStateRunning(dt);
        if (outRunningResult == CharacterStateRunningResult.CanCallAllWithoutMove)
        {
            PreCalculateDamageInfo();
        }

        return outRunningResult;
    }

    private void PreCalculateDamageInfo()
    {
        // 모든 이펙트 코드에서 DamageTestFlags 수집
        var skipTests = CharacterController.DamageTestFlags.None;
        bool isPierceAttack = false;
        bool isEnhancedAttack = false;
        
        var effectCodes = characCtrl.GetEffectCodeContainer()
            .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseModifyDamageTestFlags);
        
        for (int i = 0; i < effectCodes.Count; i++)
        {
            if (effectCodes[i] is EffectCodeCharacterBase characterEffectCode)
            {
                var flags = characterEffectCode.GetDamageTestFlags();
                skipTests |= flags;
                
                // Pierce 공격 여부 확인 (SkipResistPierce가 설정되어 있으면 Pierce 공격으로 간주)
                if ((flags & CharacterController.DamageTestFlags.SkipResistPierce) != 0)
                {
                    isPierceAttack = true;
                }
                
                // 강화탄 여부 확인 (SkipAvoidTest가 설정되어 있으면 강화탄으로 간주)
                if ((flags & CharacterController.DamageTestFlags.SkipAvoidTest) != 0)
                {
                    isEnhancedAttack = true;
                }
            }
        }
        
        _specialAttackInfo.IsSpecialAttack = isPierceAttack || isEnhancedAttack;

        if (characCtrl.SpecCharacter.atk_type == AtkType.AD)
        {
            _specialAttackInfo.DamageInfo = characCtrl.CalculateDamageAmount(characCtrl.AD, 0, characCtrl.Target,
            0, false, skipTests);
        }
        else
        {
            _specialAttackInfo.DamageInfo = characCtrl.CalculateDamageAmount(0, characCtrl.AP, characCtrl.Target,
            0, false, skipTests);
        }
        
        // 강화탄일 경우 크리티컬 강제 적용 및 크리티컬 데미지 배율 적용
        if (isEnhancedAttack)
        {
            characCtrl.ApplyCriticalDamage(ref _specialAttackInfo.DamageInfo);
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


            if (hitCount > 1)
            {
                _specialAttackInfo.DamageInfo.damageAmount /= hitCount;
            }

            InGameVfxNameType projectile = InGameVfxNameType.NONE;
            if (_specialAttackInfo.IsSpecialAttack)
            {
                // projectile = characCtrl.SpecCharacter.projectile_vfx_name_type;
                projectile = characCtrl.SpecCharacter.atk_type is AtkType.AD ?
                InGameVfxNameType.fx_common_job_sharpshooter_01 : InGameVfxNameType.fx_common_job_sharpshooter_02;
            }
            else
            {
                                // projectile = characCtrl.SpecCharacter.projectile_vfx_name_type;

                projectile = characCtrl.SpecCharacter.atk_type is AtkType.AD ?
                InGameVfxNameType.fx_common_job_sharpshooter_01 : InGameVfxNameType.fx_common_job_sharpshooter_02;
            }
            

            if (projectile != InGameVfxNameType.NONE)
            {
                if (characCtrl == null || characCtrl.IsAlive == false)
                {
                    return;
                }

                Transform projectileTransform = characCtrl.GetCharacterView().CachedFront ? characCtrl.GetCharacterView().ProjectileFrontTransform 
                : characCtrl.GetCharacterView().ProjectileBackTransform;

                var vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(projectile,
                    projectileTransform.position);

                var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();

                Vector3 direction =
                    (characCtrl.Target.CurrentTile.View.CachedTr.position -
                     characCtrl.CurrentTile.View.CachedTr.position).normalized;
                vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction);

                movement.SetData(vfxProjectile.CachedTr.position,
                    // characCtrl.Target.GetCharacterView().CachedTr.position, 50);
                    characCtrl.Target.GetCharacterView().CachedTr.position, 5f);
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
    }
}
