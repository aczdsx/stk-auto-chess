using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;

/// <summary>
/// 3챕터 서포터
// 대상 : 아군 전체
// 효과 : 체력을 공격력 {0}%만큼 회복시킨다.
/// </summary>
[UseEffectCodeIds(230606006)]
public partial class EffectCodeSkill230606006 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        SkillIndex = 0;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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

        var characterControllers = InGameObjectManager.Instance.GetCharacterList(owner.AllianceType);

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByAllianceType(owner.AllianceType, 10);
        foreach (var character in characterControllers)
        {
            if(character == owner)
                continue;
            
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], character.CurrentTile.View.CachedTr.position);
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, character.CurrentTile);
        }

        foreach (var character in characterControllers)
        {
            if (character != null)
            {
                double damage = owner.PostCalculateHealAmount(owner.AP * _damageRate, character);
                character.GetHealed(damage, owner, codeId, true);
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
}
