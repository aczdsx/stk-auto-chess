using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 하티
// 대상 : 공격력이 가장 높은 적 1명
// 대미지 : 공격력 {0}%의 대미지를 가한다.
// 특수 효과 : 피격된 적은 두 칸 거리만큼 넉백된다.
/// </summary>
[UseEffectCodeIds(1404021)]
public class EffectCodeSkill1404021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

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
        isReadyToActivate = false;
        isSkillActivated = false;

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

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], _targetCharacter.SkillRootTransformFollowable);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (_targetCharacter == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            _targetCharacter.SkillRootTransformFollowable);

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[2],
            _targetCharacter.SkillRootTransformFollowable);

        var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, _targetCharacter);
        _targetCharacter.GetDamaged(damage, owner);

        var inGameTile =
            InGameObjectManager.Instance.InGameGrid.GetTileForKnockBack(owner.CurrentTile, _targetCharacter.CurrentTile,
                2);

        long effectCodeID = (long)EffectCodeNameType.BOUND;
        Span<double> eccStats = stackalloc double[3];
        eccStats.Clear();
        eccStats[0] = 0.5f;
        eccStats[1] = 0.3f;
        eccStats[2] = inGameTile.View.ID;

        var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
        _targetCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, owner);

        isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        isSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
