using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 1챕터 일반 탱커
// 대상 : 가장 가까운 적
// 대미지 : 강한 일격을 가해 공격력 {0}%의 대미지를 준다.
// 특수 효과 : 뒤로 1칸 넉백 시킨다.
/// </summary>
[UseEffectCodeIds(230202002)]
public partial class EffectCodeSkill230202002 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool isReadyToActivate;

    private SkillActive _specSkill;

    private CharacterController _targetCharacter;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        isReadyToActivate = false;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (_targetCharacter == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            _targetCharacter.SkillRootTransformFollowable);

        var directionTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        if (directionTile.Count > 0)
        {
            var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                owner.SkillRootTransformFollowable);
            
            Vector3 direction = (directionTile[0].View.CachedTr.position - vfx.CachedTr.position).normalized;
            vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
            // var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
            // owner.PostCalculateDamageAmount(ref damage, _targetCharacter);
            _targetCharacter.GetDamaged(damage, owner);

            var inGameTile =
                InGameObjectManager.Instance.InGameGrid.GetTileForKnockBack(owner.CurrentTile, _targetCharacter.CurrentTile,
                    1);

            Span<double> eccStats = stackalloc double[3];
            eccStats.Clear();
            eccStats[0] = 0.3f;
            eccStats[1] = 0.3f;
            eccStats[2] = inGameTile.View.ID;
        
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.KNOCKBACK, _targetCharacter, eccStats, source);
        }

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
