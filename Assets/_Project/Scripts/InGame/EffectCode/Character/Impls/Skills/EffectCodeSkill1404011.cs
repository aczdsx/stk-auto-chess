using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 에이프릴
//"범위 : 전방 1칸, 3칸, 5칸, 7칸 
//효과 : 넓은 범위에 다수의 총기를 꺼내 {0}초 동안 난사하며, 범위별로 다른 대미지를 부여한다. 
//    대미지 : 
//      -전방 1칸, 3칸은 초당 공격력 {1}%의 대미지
//    -전방 5칸은 초당 공격력 {2}%의 대미지 
//    -전방 7칸은 초당 공겨력 {3}%의 대미지 "
/// </summary>
/// 
[UseEffectCodeIds(1404011)]
public class EffectCodeSkill1404011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate1;
    private ObfuscatorFloat _powerRate2;
    private ObfuscatorFloat _powerRate3;
    private ObfuscatorFloat _durationTime;
    private ObfuscatorFloat _debuffRate;

    private bool _isReadyToActivate;
    private SpecSkill _specSkill;
    
    private float _elapsedTime;
    private float _totalElapsedTime;
    private InGameVfx _vfx;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _durationTime = codeInfo.GetCodeStatToFloat(1);
        _powerRate1 = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _powerRate2 = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _powerRate3 = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _durationTime = codeInfo.GetCodeStatToFloat(1);
        _powerRate1 = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _powerRate2 = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _powerRate3 = codeInfo.GetCodeStatToFloat(4) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }
        
        _elapsedTime += dt;

        if (_elapsedTime >= 1f)
        {
            _elapsedTime -= 1f;
            OnSkillExecute(0, 0);
        }

        _totalElapsedTime += dt;
        if (_totalElapsedTime >= _durationTime)
        {
            OnSkillEnd();
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;
        
        _vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
        Vector3 direction = (owner.CurrentTile.View.CachedTr.position - _vfx.CachedTr.position).normalized;
        _vfx.CachedTr.rotation = Quaternion.LookRotation(direction);
        
        var inGameTiles1 = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        var inGameTiles2 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 2, 1);
        var inGameTiles3 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 3, 2);
        var inGameTiles4 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 4, 3);

        ProcessTiles(inGameTiles1, owner, _powerRate1);
        ProcessTiles(inGameTiles2, owner, _powerRate1);
        ProcessTiles(inGameTiles3, owner, _powerRate2);
        ProcessTiles(inGameTiles4, owner, _powerRate3);

        IsSkillActivated = false;
    }
    
    private void OnSkillEnd()
    {
        IsSkillActivated = false;
        owner.AddNextState<CharacterStateIdle>();
        CoolTimeElapsedTime = CoolTimeDurationTime;
        _vfx.Remove();
        base.OnSkillAnimationEnd();
    }

    public override void OnSkillAnimationEnd()
    {
        //[TODO] 이거 불리지 않도록 end를 제거하거나 스킬을 길게 만들던 해서 다른 방법으로 처리해야 함.
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private void ProcessTiles(List<InGameTile> tiles, CharacterController owner, float powerRate)
    {
        foreach (var tile in tiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile);
            tile.CheckValidTile(owner.AllianceType, false, () =>
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);

                var damage = owner.PrecalculateDamageAmount(owner.AD * powerRate, 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
            });
        }
    }
}
