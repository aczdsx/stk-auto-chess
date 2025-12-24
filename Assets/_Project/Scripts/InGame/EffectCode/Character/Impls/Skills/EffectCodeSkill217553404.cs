using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 클레이
/// 범위: 자신 기준 다이아 모양 5*5 맨허튼ㄴ 거리 2
/// 형태: 채널링
/// 효과: 3초간 영역을 생성하며, 
/// 영역에 있는 아군은 클레이의 {1}% 치유력 만큼 회복 및 CC제거 효과를 제공받는다. 
/// 영역에 있는 적군은 {2}% 만큼 피해와 회복량 {3}%감소 디버프를 {4}초간 받는다.
/// </summary>
/// 
[UseEffectCodeIds(217553404)]
public partial class EffectCodeSkill217553404 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;//1
    private ObfuscatorFloat _tileDamage;//2
    private ObfuscatorFloat _healRateDecreaseRate;//3
    private ObfuscatorFloat _debuffTime;//4

    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    
    private float _elapsedTime;
    private float _totalElapsedTime;
    private InGameVfx _vfx;
    private int _count;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _tileDamage = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _healRateDecreaseRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(4);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _tileDamage = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _healRateDecreaseRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(4);
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner == null)
            return;

        float duration = 3.0f;
        _count = 0;
        
        var inGameTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(owner.CurrentTile, 2);
        if (inGameTileList.Count > 0)
        {
            ProcessArea(duration, inGameTileList).Forget();
            PlayEffect(owner.CurrentTile);
        }
    }
    
    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTask ProcessArea(float duration, List<InGameTile> areaTiles)
    {
        // 1초마다 영역 내 캐릭터들을 처리
        int tickCount = (int)duration;
        for (int i = 0; i < tickCount; i++)
        {
            if (owner == null)
                break;

            ProcessTiles(areaTiles, owner);
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
        }
    }

    private void PlayEffect(InGameTile tile)
    {
        if (owner == null)
            return;

        // 영역 VFX 생성
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);
        
        // 영역 타일들에 타일 이펙트 표시
        var inGameTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(tile, 2);
        foreach (var areaTile in inGameTileList)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, areaTile);
        }
    }

    private void ProcessTiles(List<InGameTile> tiles, CharacterController owner)
    {
        foreach (var tile in tiles)
        {
            if (tile.OccupiedCharacter == null)
                continue;

            var character = tile.OccupiedCharacter;
            
            // 아군 처리: 치유 + CC제거
            if (character.AllianceType == owner.AllianceType)
            {
                // 치유
                double healAmount = owner.PostCalculateHealAmount(owner.AP * _healRate, character);
                character.GetHealed(healAmount, owner, codeId, true);
                
                // CC 제거: CrowdControl 타입의 이펙트코드 모두 제거
                RemoveAllCrowdControls(character);
            }
            // 적군 처리: 피해 + 회복량 감소 디버프
            else if (tile.CheckValidTile(owner.AllianceType, false))
            {
                // 피해
                var damage = owner.CalculateDamageAmount(owner.AD * _tileDamage, 0, character, codeId, true);
                character.GetDamaged(damage, owner);
                
                // 회복량 감소 디버프 적용
                Span<double> eccStats = stackalloc double[3];
                eccStats.Clear();
                eccStats[0] = codeId;
                eccStats[1] = _debuffTime;
                eccStats[2] = _healRateDecreaseRate;
                
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, character, eccStats, source);
            }
        }
    }

    private void RemoveAllCrowdControls(CharacterController character)
    {
        // 모든 CC 타입을 개별적으로 제거
        // RemoveCrowdControl을 호출하면 해당 CC 이펙트코드의 OnPreRemoved가 호출되어 자동으로 제거됨
        character.RemoveCrowdControl(CrowdControlType.Airborne);
        character.RemoveCrowdControl(CrowdControlType.KnockBack);
        character.RemoveCrowdControl(CrowdControlType.Entangle);
        character.RemoveCrowdControl(CrowdControlType.Stun);
        character.RemoveCrowdControl(CrowdControlType.Slowing);
        character.RemoveCrowdControl(CrowdControlType.Provocation);
        character.RemoveCrowdControl(CrowdControlType.Freezing);
        character.RemoveCrowdControl(CrowdControlType.Silence);
        character.RemoveCrowdControl(CrowdControlType.MisaRestraint);
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}
