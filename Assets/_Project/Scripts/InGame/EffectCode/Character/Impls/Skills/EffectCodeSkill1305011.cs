using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 시이나
// 대상 : 가장 가까운 적
// 대미지 : 강하게 베어 공격력 {0}%의 대미지를 가한다.
//     특수 효과 : 피격된 적은 {1}초 동안 스킬 쿨타임 감소 속도를 {2}%느리게 한다.
/// </summary>
[UseEffectCodeIds(1305011)]
public class EffectCodeSkill1305011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;
    private ObfuscatorFloat _debuffRate;
    private ObfuscatorFloat _debuffTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    private SpecSkill _specSkill;

    private CharacterController _targetCharacter;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(1);
        _debuffRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        isReadyToActivate = false;
        isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!isSkillActivated)
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
        if (isReadyToActivate || isSkillActivated)
            return;
        CoolTimeElapsedTime += dt;
        if (CoolTimeElapsedTime >= CoolTimeDurationTime)
        {
            isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();
        // TODO: Target Check
        isReadyToActivate = false;
        isSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);

        _targetCharacter = owner.Target;
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (_targetCharacter == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            _targetCharacter.SkillRootTransformFollowable);

        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);
        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        Vector3 direction = (inGameTile.View.CachedTr.position - vfx.CachedTr.position).normalized;
        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

        var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, _targetCharacter);
        _targetCharacter.GetDamaged(damage, owner);

        Span<double> debuffStats = stackalloc double[3];
        debuffStats.Clear();
        debuffStats[0] = codeId;
        debuffStats[1] = _debuffTime;
        debuffStats[2] = _debuffRate;
        var effectCodeID = new EffectCodeInfo((long)CharacterEffectType.DEBUFF_COOL_DOWN_SPEED_PERCENT_DOWN, 0, debuffStats);
        _targetCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeID, owner);

        isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        CoolTimeElapsedTime = 0;
        isSkillActivated = false;
    }
}
