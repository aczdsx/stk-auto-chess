using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 4챕터 일반 암살자
// 타겟 : 가장 가까이에 위치한 적 1명 
// 대미지 : 공격력 {0}%로 적을 {1}회 베어 대미지를 준다. 
/// </summary>
[UseEffectCodeIds(1105011)]
public class EffectCodeSkill1105011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;
    private ObfuscatorInt _attackCount;

    private bool isReadyToActivate;

    private SpecSkill _specSkill;

    private CharacterController _targetCharacter;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _attackCount = codeInfo.GetCodeStatToInt(2);
        isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _attackCount = codeInfo.GetCodeStatToInt(2);
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
        if (isReadyToActivate || IsSkillActivated)
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

        var isInRange = InGameObjectManager.Instance.IsInRange(owner, owner.Target);
        if (!isInRange)
        {
            if (owner.Target != null)
            {
                InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(owner.CurrentTile,
                    owner.Target.CurrentTile);
                owner.MoveTile(bestTile);
            }
            return;
        }

        isReadyToActivate = false;
        IsSkillActivated = true;
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

        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
            _targetCharacter.SkillRootTransformFollowable);
        var directionTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        if (directionTile.Count > 0)
        {
            Vector3 direction = (directionTile[0].View.CachedTr.position - vfx.CachedTr.position).normalized;
            vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            ExecuteSkillRoutine(_attackCount).Forget();

        }
        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTaskVoid ExecuteSkillRoutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                _targetCharacter.SkillRootTransformFollowable);
            var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
            owner.PostCalculateDamageAmount(ref damage, _targetCharacter);
            _targetCharacter.GetDamaged(damage, owner);
            
            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));
        }
    }
}
