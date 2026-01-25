using CookApps.AutoBattler;
using CookApps.AutoBattler.Prologue;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class LaplasCharacterStateMarieAttack : CharacterStateBase
{
    protected bool isAttackAnimRunning;
    public override StatePriority StatePriority => StatePriority.Attack;
    protected float _vfxProjectileSpeed = 30;

    public override void SetStateData(object projectileSpeed)
    {
        if(projectileSpeed is float speed)
        {
            _vfxProjectileSpeed = speed;
        }
    }

    public override void StateStart()
    {
        base.StateStart();
        isAttackAnimRunning = false;
    }

    public override CharacterStateRunningResult CharacterStateRunning(float dt)
    {
        if (isAttackAnimRunning)
        {
            return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
        }

        if (characCtrl != null)
        {
            if (characCtrl.NeedToBeCrowdControlState())
            {
                characCtrl.Target = null;
                characCtrl.AddNextState<CharacterStateCC>();
                return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
            }

            // 1. 잡는 적이 아직 살아있는지 체크
            CharacterController atkTarget = characCtrl.Target;
            if (atkTarget == null || !atkTarget.IsAlive ||
                !InGameObjectManager.Instance.IsInRange(characCtrl, characCtrl.Target))
            {
                isAttackAnimRunning = false;
                characCtrl.Target = null;
                characCtrl.AddNextState<CharacterStateIdle>();
                return CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
            }

            // Flip은 공격, 스킬(타겟 방향), 이동(다음 타일 방향)
            characCtrl.LookAtTarget();
            Vector2 diff = characCtrl.Target.Position - characCtrl.Position;
            characCtrl.FlipX = diff.x > 0;

            if (characCtrl.GetAttackCoolTime() <= 0f)
            {
                characCtrl.ResetAttackCoolTime();

                // 이펙트 코드에게 공격 횟수 전달
                var characEffectCodes = characCtrl.GetEffectCodeContainer()
                    .GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnAttack);
                EffectCodeForLoopHelper.Call(characEffectCodes, EffectCodeCharacterLambda.CallOnAttackLambda);

                RunAttackAnimation();
                isAttackAnimRunning = true;
            }
        }

        return isAttackAnimRunning
            ? CharacterStateRunningResult.CanCallAllWithoutMove
            : CharacterStateRunningResult.CanCallEffectCodeOnUpdateAndOnCooltime;
    }

    protected virtual void RunAttackAnimation()
    {
        AnimationClip clip = characCtrl.GetCharacterView().PlayAnimation(AnimationKey.ATK);
        // 공격 애니메이션 타임 스케일 계산 방법 : 기본 공격 시간 (atkTime : 1/atkSpeed)이 공격 애니메이션 시간의 1.5배보다 느리면
        // 공격 애니메이션 타임에 공속을 곱함. 아님 1f
        var attackSpeed = characCtrl.AttackSpeed;
        float animTime = clip.length * 1.5f;
        float atkTime = 1f / attackSpeed;
        if (animTime > atkTime)
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(animTime * attackSpeed);
        }
        else
        {
            characCtrl.GetCharacterView().SetAnimationSpeed(1f);
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
            characCtrl.GetCharacterView().PlayAnimation(AnimationKey.IDLE);
            return;
        }

        if (characCtrl.Target == null)
            return;
        
        if(characCtrl.Target.CharacterId != PrologueID.프롤로그마리에ID)
        {
            return;
        }

        if (!characCtrl.Target.IsAlive)
            return;

        if (AnimationEventKey.ExecuteStart < eventKey && eventKey < AnimationEventKey.ExecuteEnd)
        {
            int hitCount = eventKey - AnimationEventKey.ExecuteStart;

            // damage 계산
            CharacterController.DamageInfo damageInfo = CalculateNormalAttackDamage();
            if (hitCount > 1)
            {
                damageInfo.damageAmount /= hitCount;
            }

            InGameVfxNameType projectile = characCtrl.SpecCharacter.projectile_vfx_name_type;

            if (projectile != InGameVfxNameType.NONE)
            {
                var vfxProjectile = CreateProjectileVfx(projectile, _vfxProjectileSpeed, (vfx) =>
                {
                    OnAttackEndProcess();
                    if (characCtrl != null && characCtrl.Target != null)
                        characCtrl.Target.GetDamaged(damageInfo, characCtrl);
                    vfx.Remove();
                });
            }
            else
            {
                // TODO: Effect
                OnAttackEndProcess();
                characCtrl.Target.GetDamaged(damageInfo, characCtrl);
            }
        }
    }

    public virtual CharacterController.DamageInfo CalculateNormalAttackDamage()
    {
        CharacterController.DamageInfo damageInfo;
        if (characCtrl.SpecCharacter.atk_type == AtkType.AD)
        {
            damageInfo = characCtrl.CalculateDamageAmount(characCtrl.Target.HP * 0.51f, 0, characCtrl.Target, 0, false);
        }
        else
        {
            damageInfo = characCtrl.CalculateDamageAmount(0, characCtrl.Target.HP * 0.51f, characCtrl.Target, 0, false);
        }

        return damageInfo;

    }

    virtual protected void OnAttackEndProcess()
    {
        if (characCtrl == null || characCtrl.Target == null || !characCtrl.Target.IsAlive)
            return;
        var ctrlEcc = characCtrl?.GetEffectCodeContainer();
        var effectCodes = ctrlEcc?.GetCharacterEffectCodesByFlag(EffectCodeInheritFlag.UseOnAttackEnd);
        EffectCodeForLoopHelper.CallWithArgs(effectCodes, EffectCodeCharacterLambda.CallOnAttackEndLambda, characCtrl.Target);
    }

    /// <summary>
    /// 프로젝타일 VFX를 생성하고 타겟으로 이동시킵니다.
    /// </summary>
    /// <param name="projectile">프로젝타일 VFX 타입</param>
    /// <param name="speed">프로젝타일 이동 속도 (기본값: _vfxProjectileSpeed)</param>
    /// <param name="onReachedTarget">타겟 도달 시 호출할 핸들러</param>
    /// <returns>생성된 VFX 프로젝타일</returns>
    protected InGameVfx CreateProjectileVfx(InGameVfxNameType projectile, float? speed = null, System.Action<InGameVfx> onReachedTarget = null)
    {
        if (characCtrl == null || characCtrl.IsAlive == false || characCtrl.Target == null)
            return null;

        // 프로젝타일 Transform 가져오기
        Transform projectileTransform = characCtrl.GetCharacterView().CachedFront 
            ? characCtrl.GetCharacterView().ProjectileFrontTransform 
            : characCtrl.GetCharacterView().ProjectileBackTransform;

        // VFX 프로젝타일 생성
        var vfxProjectile = InGameVfxManager.Instance.AddInGameVfx(projectile, projectileTransform.position);

        // Movement 생성 및 설정
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();

        // 방향 계산 및 회전 설정
        Vector3 direction = (characCtrl.Target.CurrentTile.View.CachedTr.position -
                             characCtrl.CurrentTile.View.CachedTr.position).normalized;
        vfxProjectile.CachedTr.rotation = Quaternion.LookRotation(direction);

        // Movement 데이터 설정
        float projectileSpeed = speed ?? _vfxProjectileSpeed;
        movement.SetData(vfxProjectile.CachedTr.position,
            characCtrl.Target.SkillMiddleFXTransformFollowable.GetPosition(), projectileSpeed);
        vfxProjectile.Initialize(false, movement);

        // 타겟 도달 핸들러 등록
        if (onReachedTarget != null)
        {
            movement.OnReachedTarget += () => onReachedTarget(vfxProjectile);
        }

        return vfxProjectile;
    }


    public override void StateEnd(bool isForced)
    {
        characCtrl.GetCharacterView().SetAnimationSpeed(1f);
        base.StateEnd(isForced);
    }

}
