using System;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;

/// <summary>
/// 미사
// 대상 : 스킬 발동 시점 누적 딜이 가장 높은 적 
// 효과 : 적을 {0}초 동안 관에 가둬 봉인한다. 봉인된 적도 공격 대상으로 취급되며, 봉인된 적은 봉인동안 
// 스킬 쿨타임이 흐르지 않는다. 
/// </summary>
[UseEffectCodeIds(217323201)]
public partial class EffectCodeSkill217323201 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _time;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;

    private bool isKilled;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _time = codeInfo.GetCodeStatToFloat(1);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _time = codeInfo.GetCodeStatToFloat(1);
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

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        // InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
        //     owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        if (owner.Target == null)
            return;

        var characterControllers = InGameObjectManager.Instance.GetCharacterListSortedByADDescending(owner.AllianceType, false);
        if (characterControllers.Count > 0)
        {
            var characterController = characterControllers[0];
            // InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, characterController.CurrentTile);

            {
                Span<double> eccStats = stackalloc double[1];
                eccStats.Clear();
                eccStats[0] = _time;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_MISA_RESTRAINT, characterController, eccStats, source);
            }

            {
                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = codeId;
                eccStats[1] = _time;
                eccStats[2] = 0;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_MISA, characterController, eccStats, source);
            }

            {
                Span<double> eccStats = stackalloc double[1];
                eccStats.Clear();
                eccStats[0] = _time;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, characterController, eccStats, source);
            }
        }

        IsSkillActivated = false;


    }


    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }



    public override void OnSkillAnimationEnd()
    {
        if (isKilled)
        {
            CoolTimeElapsedTime = CoolTimeDurationTime;
            _isReadyToActivate = true;
            isKilled = false;
        }
        else
        {
            CoolTimeElapsedTime = 0.0f;
        }
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
    }
}
