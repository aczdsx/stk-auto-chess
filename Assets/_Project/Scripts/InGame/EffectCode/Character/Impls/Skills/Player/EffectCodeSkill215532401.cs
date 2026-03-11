using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using UnityEngine;

/// <summary>
/// 필리아
// 대상 : 가장 가까운 적
// 대미지 : 하티 처럼 한명 한 발 쎄게 데미지를 준다.
/// </summary>
[UseEffectCodeIds(215532401)]
public partial class EffectCodeSkill215532401 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;

    private bool isKilled;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            CoolTimeElapsedTime = CoolTimeDurationTime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || IsSkillActivated)
            return;
        CoolTimeElapsedTime += dt;
        if (CoolTimeElapsedTime >= CoolTimeDurationTime)
        {
            _isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return _isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();

        owner.Target = InGameObjectManager.Instance.GetCharacterListSortedByDistanceDescending(owner, false)[0];

        // var isInRange = InGameObjectManager.Instance.IsInRange(owner, owner.Target);
        // if (!isInRange)
        // {
        //     if (owner.Target != null)
        //     {
        //         InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(owner.CurrentTile,
        //             owner.Target.CurrentTile);
        //         owner.MoveTile(bestTile);
        //     }
        //     return;
        // }

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.GetCharacterView().LookAt(owner.CurrentTile, owner.Target.CurrentTile);

        owner.AddNextState<CharacterStateSkill>(this);

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.SkillMiddleFXTransformFollowable.GetPosition());
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (owner.Target == null)
            return;

        Transform projectileTransform = owner.GetCharacterView().CachedFront ? owner.GetCharacterView().ProjectileFrontTransform : owner.GetCharacterView().ProjectileBackTransform;
        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 1);
        //shooot effect
        var shootEffect = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], projectileTransform.position);

        if (inGameTile.Count > 0)
        {
            var direction = (inGameTile[0].View.CachedTr.position - owner.CurrentTile.View.CachedTr.position).normalized;
            shootEffect.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90f, 0);
        }
        //hit effect
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[2], owner.Target.SkillMiddleFXTransformFollowable);


        var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, owner.Target, codeId, true);
        owner.Target.GetDamaged(damage, owner, true, isNonHitFx: true);

        IsSkillActivated = false;

    }


    virtual protected void OnReachedTargetProcess()
    {
        if (owner == null || owner.Target == null || !owner.Target.IsAlive)
            return;
        var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, owner.Target, codeId, true);
        var type = owner.Target.GetDamaged(damage, owner);

        if (type == DamageReturnType.Killed)
        {
            isKilled = true;
        }
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }



    public override void OnSkillAnimationEnd()
    {
        if (isKilled)
        {
            CoolTimeElapsedTime = CoolTimeDurationTime;
            _isReadyToActivate = true;
            isKilled = false;
        }
        else
        {
            CoolTimeElapsedTime = 0.0f;
        }
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
    }
}
