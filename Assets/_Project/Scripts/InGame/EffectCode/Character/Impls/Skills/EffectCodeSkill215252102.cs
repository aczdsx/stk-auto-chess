using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 유니
// 재사용 시간: {0} 초
// 체력이 가장 낮은 아군 3인
// 대상에게 유니 치유력의 {1}% 만큼 회복 시키고, {2}개의 디버프 제거 효과를 부여합니다.
/// </summary>
[UseEffectCodeIds(215252102)]
public partial class EffectCodeSkill215252102 : EffectCodeCharacterBase
{

    private float _healRate;
    private int _ccRigidCount;

    private bool _isReadyToActivate;
    private SkillActive _specSkill;

    private int _targetCount = 3;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _ccRigidCount = codeInfo.GetCodeStatToInt(2);

        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList((int)codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _ccRigidCount = codeInfo.GetCodeStatToInt(2);
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }

        // target check
        {
        if (false)
            owner.AddNextState<CharacterStateIdle>();
            CoolTimeElapsedTime = CoolTimeDurationTime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || IsSkillActivated)
        {
            return;
        }

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
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
        {
            return;
        }

        // 나한테 붙은 vfx
        // InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.SkillRootTransformFollowable);

        var targetCharacters = InGameObjectManager.Instance.GetCharacterListSortedByHPRateDescending(owner.AllianceType, true);
        if (targetCharacters.Count > 0)
        {
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillMiddleFXTransformFollowable);

            for (int i = 0; i < _targetCount; i++)
            {
                if (i >= targetCharacters.Count)
                    break;

                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type,
                    targetCharacters[i].CurrentTile);

                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], targetCharacters[i].SkillMiddleFXTransformFollowable);


                double healAmount = owner.PostCalculateHealAmount(owner.AP * _healRate, targetCharacters[i], isSkill: true);
                targetCharacters[i].GetHealed(healAmount, owner, codeId, true);


                RemoveDebuffs(targetCharacters[i]);
            }
        }

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

    private void RemoveDebuffs(CharacterController targetCharacter)
    {
        var debuffs = targetCharacter.GetEffectCodeContainer().GetEffectCodesByType(EffectCodeType.Debuff);
        if (debuffs.Count > 0)
        {
            for (int i = 0; i < _ccRigidCount; i++)
            {
                targetCharacter.GetEffectCodeContainer().RemoveEffectCode(debuffs[i].CodeId);
            }
        }
    }



}

