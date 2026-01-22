using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CharacterController = CookApps.BattleSystem.CharacterController;
using UnityEngine;

/// <summary>
/// 0챕터 일반 저격수
// "대상 : 가장 가까운 적
// 대미지 : 공격력 {0}%의 대미지를 입힌다. "
/// </summary>
[UseEffectCodeIds(230101005)]
public partial class EffectCodeSkill230101005 : EffectCodeCharacterBase
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


        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.SkillMiddleFXTransformFollowable.GetPosition());
        Vector3 direction = (_targetCharacter.SkillMiddleFXTransformFollowable.GetPosition() - vfx.CachedTr.position).normalized;

        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, -90f, 0);
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
        movement.SetData(owner.CurrentTile.View.CachedTr.position, _targetCharacter.SkillMiddleFXTransformFollowable.GetPosition(), 30f);
        movement.OnReachedTarget += () =>
        {
            if (_targetCharacter != null && _targetCharacter.IsAlive
            && owner != null && owner.IsAlive)
            {
                var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);
                _targetCharacter.GetDamaged(damage, owner);

                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], _targetCharacter.SkillMiddleFXTransformFollowable);//hit
            }
            vfx.Remove();

        };

        vfx.Initialize(false, movement);

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
