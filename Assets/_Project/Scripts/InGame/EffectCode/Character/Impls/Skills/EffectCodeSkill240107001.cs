using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 베놈 
/// 대상: 가장 가까운 적
/// {0}쿨타임
/// 어금니로 상대를 물어 뜯어 {1} % 피해를 입히고 
/// { 2}초간 {3}%의 피해를 매초 입힙니다.
/// </summary>
[UseEffectCodeIds(240107001)]
public partial class EffectCodeSkill240107001 : EffectCodeCharacterBase
{
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private float _damageRate;
    private float _debuffTime;
    private float _debuffDamageRate;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;

        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _debuffDamageRate = codeInfo.GetCodeStatToFloat(3) *0.01f;

        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);

        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;

        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _debuffDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        owner.Target = InGameObjectManager.Instance.GetNearestTargetByManhattanDistance(owner);
        if (owner.Target == null)
        {
            return;
        }

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);

    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.Target.SkillRootTransformFollowable.GetPosition());

        var damageValue = owner.SpecCharacter.atk_type is AtkType.AD ? owner.AD : owner.AP;

        var damage = owner.CalculateDamageAmount(damageValue * _damageRate, 0, owner.Target, codeId, true);
        owner.Target.GetDamaged(damage, owner);

        Span<double> eccStats = stackalloc double[3];
        eccStats.Clear();
        eccStats[0] = codeId;
        eccStats[1] = _debuffTime;// duration   
        eccStats[2] = _debuffDamageRate * damageValue; // value
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_POISON, owner.Target, eccStats, source);

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}//240107001
