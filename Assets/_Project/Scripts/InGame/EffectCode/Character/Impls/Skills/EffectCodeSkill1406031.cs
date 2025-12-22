using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 아란
// "타겟 : 현재 체력이 가장 낮은 아군 1명
// 효과 : 아란 공격력 {0}%만큼 체력을 회복시키고, 물리, 마법 방어력을 {1}만큼 추가한다. "
/// </summary>
[UseEffectCodeIds(1406031)]
public partial class EffectCodeSkill1406031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _buffRate;
    private bool _isReadyToActivate;
    private SkillActive _specSkill;

    private InGameVfxObj _vfxObj;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _buffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        SkillIndex = 0;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _buffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        if (_vfxObj != null)
        {
            _vfxObj = InGameVfxManager.Instance
                .AddInGameVfx(_specSkill.skill_vfxs[0], owner.GetCharacterView().CachedTr.position)
                .GetComponent<InGameVfxObj>();
        }

        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();

        var targetCharacterList = InGameObjectManager.Instance.GetCharacterListSortedByHpRate(owner.AllianceType, true);
        if (targetCharacterList.Count > 0)
        {
            var characterWithLowestHp = targetCharacterList[0];

            Vector3 direction = (characterWithLowestHp.CurrentTile.View.CachedTr.position - _vfxObj.CachedTr.position).normalized;
            _vfxObj.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            movement.SetData(_vfxObj.CachedTr.position, characterWithLowestHp.CurrentTile.View.CachedTr.position, 20);
            _vfxObj.Initialize(false, movement);

            void OnReachedTargetHandler()
            {
                if (characterWithLowestHp != null)
                    _vfxObj.SetFollowable(characterWithLowestHp.SkillRootTransformFollowable);
            }

            movement.OnReachedTarget += OnReachedTargetHandler;
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

}
