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
/// 프롤로그_유니
/// </summary>
[UseEffectCodeIds(295250010)]
public partial class EffectCodeSkill295250010 : EffectCodeSkill215252102
{
    
}


/// <summary>
/// 프롤로그_필리아
/// </summary>
[UseEffectCodeIds(295530011)]
public partial class EffectCodeSkill295530011 : EffectCodeSkill215532401
{
    
}



/// <summary>
/// 프롤로그_아트레시아
/// </summary>
[UseEffectCodeIds(297510012)]
public partial class EffectCodeSkill297510012 : EffectCodeSkill217513401
{
    
}


[UseEffectCodeIds(297510112)]
public partial class EffectCodeSkill297510112 : EffectCodeSkill217513401
{
    
}



/// <summary>
/// 프롤로그_클레이
/// </summary>
[UseEffectCodeIds(297550013)]
public partial class EffectCodeSkill297550013 : EffectCodeCharacterBase
{
    // 스킬 상태
    private bool _isReadyToActivate;
    private SkillActive _specSkill;

    // 채널링 스킬 설정
    private const float SKILL_DURATION = 3.0f; // 스킬 지속 시간 (초)
    private const int AREA_RANGE = 2; // 영역 범위 (맨해튼 거리)

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
                if (owner == null)
            return;

        var centerTile = owner.CurrentTile;
        var areaTiles = GetAreaTiles(centerTile);
        
        if (areaTiles.Count > 0)
        {
            PlayAreaEffect(centerTile, areaTiles);
        }
    }
    
    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private List<InGameTile> GetAreaTiles(InGameTile centerTile)
    {
        return InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(centerTile, AREA_RANGE);
    }

    private void PlayAreaEffect(InGameTile centerTile, List<InGameTile> areaTiles)
    {
        if (owner == null)
            return;

        Unity.Mathematics.int2 prevTileIdx = new (centerTile.Int2Index.x, centerTile.Int2Index.y -1);
        var prevTile = InGameObjectManager.Instance.InGameGrid.GetTile(prevTileIdx);
        // 중심 타일 VFX 생성
        foreach (var ally in InGameObjectManager.Instance.GetCharacterList(owner.AllianceType))
        {
            InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_prologue_shield_01, ally.SkillRootTransformFollowable);
        }
    }
    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
    
}



/// <summary>
/// 프롤로그_오데트
/// </summary>
[UseEffectCodeIds(297610014)]
public partial class EffectCodeSkill297610014 : EffectCodeSkill217613501
{
    
}


/// <summary>
/// 프롤로그_마리에
/// </summary>
[UseEffectCodeIds(297560015)]
public partial class EffectCodeSkill297560015 : EffectCodeSkill217563405
{
    
}
