using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 2챕터 저격수
/// </summary>
[UseEffectCodeIds(1204011)]
public class EffectCodeSkill1204011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool _isReadyToActivate;

    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    private WeakReference<InGameVfx> _vfx;

    private SpecSkill _specSkill;

    private ElementType _elementType;

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
        // TODO: Target Check
        _isReadyToActivate = false;
        IsSkillActivated = true;
        _hitCharacters.Clear();
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
        _elementType = owner.SpecCharacter.element_type;
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
        Vector3 direction = (owner.Target.CurrentTile.View.CachedTr.position - vfx.CachedTr.position).normalized;
        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

        vfx.Initialize(false);
        vfx.OnCollisionWithTile += OnCollision2DEnter;

        IsSkillActivated = false;
    }

    private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        var tileFx = InGameVfxManager.Instance.AddInGameTileFx(_elementType, tile.View.CachedTr.position);
        tileFx.CachedTr.position = tile.View.CachedTr.position;

        if (tile.OccupiedCharacter == null)
            return;

        if (tile.OccupiedCharacter.AllianceType == AllianceType.None)
            return;

        if (_hitCharacters.Contains(tile.OccupiedCharacter))
            return;

        if(owner.AllianceType == tile.OccupiedCharacter.AllianceType)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            tile.OccupiedCharacter.SkillRootTransformFollowable);

        if (owner != null)
        {
            var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter, codeId, true);
            owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
            tile.OccupiedCharacter.GetDamaged(damage, owner);

            _hitCharacters.Add(tile.OccupiedCharacter);
        }
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
