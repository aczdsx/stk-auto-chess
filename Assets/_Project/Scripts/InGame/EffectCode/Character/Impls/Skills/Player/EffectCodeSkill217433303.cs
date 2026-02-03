using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 하티
// 대상 : 거리가 가장 먼 적 1명
// 대미지 : 공격력 {0}%의 대미지를 가한다.
// 특수 효과 : 피격된 적은 두 칸 거리만큼 넉백된다.
/// </summary>
[UseEffectCodeIds(217433303)]
public partial class EffectCodeSkill217433303 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;

    private bool isReadyToActivate;

    private SkillActive _specSkill;

    private CharacterController _targetCharacter = null;

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

        var targetCharacter = InGameObjectManager.Instance.GetFarthestTargetByManhattanDistance(owner);
        if (targetCharacter != null)
        {
            _targetCharacter = targetCharacter;
        }
        else
        {
            return;
        }


        isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        owner.GetCharacterView().LookAt(owner.CurrentTile, _targetCharacter.CurrentTile);

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillRootTransformFollowable);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], _targetCharacter.SkillRootTransformFollowable);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (_targetCharacter == null || !_targetCharacter.IsAlive)
            return;

        if (owner == null)
            return;

        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
            _targetCharacter.SkillRootTransformFollowable);

        var hitEffect = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[2],
            owner.SkillRootTransformFollowable);
        var direction = (_targetCharacter.CurrentTile.View.CachedTr.position - owner.CurrentTile.View.CachedTr.position).normalized;
        hitEffect.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0); ;

        var damage = owner.CalculateDamageAmount(owner.AD * _powerRate, 0, _targetCharacter, codeId, true);

        _targetCharacter.GetDamaged(damage, owner);

        var inGameTile =
            InGameObjectManager.Instance.InGameGrid.GetTileForKnockBack(owner.CurrentTile, _targetCharacter.CurrentTile,
                2);

        Span<double> eccStats = stackalloc double[3];
        eccStats.Clear();
        eccStats[0] = 0.3f;
        eccStats[1] = 0.3f;
        eccStats[2] = inGameTile.View.ID;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_KNOCKBACK, _targetCharacter, eccStats, source);

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
        _targetCharacter = null;
    }



    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }

}
