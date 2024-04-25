using System;
using UnityEngine;
using Random = System.Random;

namespace CookApps.BattleSystem
{
    public class InGameEffectProjectile : InGameEffectWithParticle
    {
//         protected CharacterController target;
//         protected Vector2 targetPos;
//         protected CharacterController attacker;
//         protected ProjectileMovementBase movement;
//         protected CharacterController.DamageInfo damageInfo;
//         protected float disableTimer = 0;
//         private VfxEffect vfxEffect;
//         protected float moveSpeed = 35;
//         private Action<CharacterController> onDamageAction;
//
//         public void AddChasingTarget(CharacterController target, CharacterController attacker,
//             CharacterController.DamageInfo damageInfo, ProjectileMovementType type,
//             Action<CharacterController> onHitActionCCCallBack = null, bool isAllHit = false, int allMutiCnt = 1,
//             Vector2? pos = null, bool isSkill = false, Action onHitAction = null)
//         {
//             disableTimer = 0;
//             isRemoved = false;
//             if (target == null && pos == null)
//             {
//                 target = attacker.target;
//             }
//
//             Init();
//             onDamageAction = onHitActionCCCallBack;
//             this.target = target;
//
//             if (pos != null)
//             {
//                 targetPos = (Vector2) pos;
//             }
//             else
//             {
//                 if (target != null)
//                 {
//                     targetPos = target.GetHitPos();
//                 }
//             }
//
//             if (targetPos == Vector2.zero)
//             {
//                 targetPos = UnityEngine.Random.insideUnitCircle * 10;
//             }
//
//             this.attacker = attacker;
//             this.damageInfo = damageInfo;
//
//             vfxEffect = gameObject.GetComponent<VfxEffect>();
//
//             var isAssembleAttack = false;
//
//             if (onHitAction == null)
//             {
//                 onHitAction = this.onHitAction;
//             }
//
//             if (vfxEffect != null)
//             {
//                 if (this.attacker.AllianceType != AllianceType.Enemy)
//                 {
//                     if (vfxEffect.GetProjectTileType() == VfxEffect.ProjectTileAttackType.Basic)
//                     {
//                         if (InGameMain.isAssemble == true)
//                         {
//                             isAssembleAttack = true;
//                             onHitAction = AssembleHitAction;
//                         }
//                     }
//                 }
//             }
//
//             if (vfxEffect != null)
//             {
//                 vfxEffect.Init(damageInfo, this.attacker, onHitAction: onHitAction, onCCHitAction: onHitActionCCCallBack,
//                     isAllHit: isAllHit, allHitMultiTarget: allMutiCnt, isAssembleAttack: isAssembleAttack);
//             }
//
//             LookAtTarget();
//
//             movement = ProjectileMovementPool.Create(type);
//             Vector3 endPos = isSkill == true ? (Vector3) attacker.GetSkillPosition() : (Vector3) attacker.GetAttackPosition();
//             Vector3 direction = Vector3.Normalize((Vector3) targetPos - endPos);
//
//             float atkSpeed = moveSpeed;
//
//             movement.SetData(this, target, targetPos, atkSpeed, direction, attacker);
//
//             var trail = GetComponentInChildren<TrailRenderer>();
//             if (trail != null)
//             {
//                 trail.Clear();
//             }
//         }
//
//         protected virtual void Init()
//         {
//         }
//
//         private void LookAtTarget()
//         {
//             Vector3 vec = transform.localScale;
//             vec.y = Mathf.Abs(vec.y);
//
//             if (attacker.FlipX == true)
//             {
//                 vec.y = -vec.y;
//             }
//
//             transform.localScale = vec;
//             float angle = Mathf.Atan2(targetPos.y - attacker.GetAttackPosition().y,
//                               targetPos.x - attacker.GetAttackPosition().x)
//                           * Mathf.Rad2Deg;
//
//             transform.rotation = Quaternion.Euler(0, 0, angle + 180);
//         }
//
//         public override void ManagedUpdate(float dt)
//         {
//             if (target == null)
//             {
//                 if (attacker != null)
//                 {
//                     if (attacker.target != null)
//                     {
//                         target = attacker.target;
//                     }
//                 }
//             }
//
//             movement?.ManagedUpdate(dt);
//             base.ManagedUpdate(dt);
//         }
//
//         public virtual void onHitAction()
//         {
//             //    if(target != null && attacker != null)
//             //    target.GetDamaged(damageInfo, attacker,0);
//
//             ResetEffect();
//         }
//
//         public void ChangeBulletSpeed(float speed)
//         {
//             moveSpeed = speed;
//         }
//
//         public virtual void AssembleHitAction()
//         {
//             if (isRemoved == false)
//             {
//                 ReturnProjectTile();
//                 ResetEffect();
//             }
//         }
//
//         public virtual void ReturnProjectTile()
//         {
//             isRemoved = true;
//         }
//
//         public void DamageAction(CharacterController target)
//         {
//             onDamageAction?.Invoke(target);
//         }
//
//         public virtual void ResetEffect()
//         {
//             disableTimer = 0;
//             InGameObjectManager.Instance.RemoveIngameEffect(this);
//             isRemoved = true;
//             if (movement != null)
//             {
//                 movement.Clear();
//                 ProjectileMovementPool.Release(movement);
//                 movement = null;
//             }
//
//             target = null;
//             attacker = null;
//         }
//
//         protected virtual void OnReachedTarget()
//         {
//             /*
//             IngameObjectManager.Instance.RemoveIngameEffect(this);
//
//             if(target != null && attacker != null)
//                 target.GetDamaged(damageInfo, attacker,0);
//
//             movement.Clear();
//             ProjectileMovementPool.Release(movement);
//             movement = null;
//             target = null;
//             attacker = null;*/
//         }
//
//         protected void CreateHitEffect(HitEffectType type)
//         {
//             var hitEffect = IngameEffectHitFactory.Get(type);
//             hitEffect.Initialize(targetPos /*, soringOrder*/, cachedFlipX);
//         }
    }
}
