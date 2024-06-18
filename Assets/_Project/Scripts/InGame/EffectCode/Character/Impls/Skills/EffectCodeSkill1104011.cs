using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 0챕터 일반 저격수
// "대상 : 가장 가까운 적
// 대미지 : 공격력 {0}%의 대미지를 입힌다. "
/// </summary>
[UseEffectCodeIds(1104011)]
public class EffectCodeSkill1104011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _cooltime;
    private ObfuscatorFloat _powerRate;
    private ObfuscatorFloat _elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    private SpecSkill _specSkill;

    private CharacterController _targetCharacter;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
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
            _elapsedTime = _cooltime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (isReadyToActivate || isSkillActivated)
            return;
        _elapsedTime += dt;
        if (_elapsedTime >= _cooltime)
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
        //[TODO] target이 죽었다면? 쿨타임 다시 돌리고 다시 쓸 수 있게 끔
        if (_targetCharacter == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            _targetCharacter.SkillRootTransformFollowable);

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
            _targetCharacter.SkillRootTransformFollowable);

        var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, _targetCharacter);
        _targetCharacter.GetDamaged(damage, owner);

        isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        _elapsedTime = 0;
        isSkillActivated = false;
    }
}
