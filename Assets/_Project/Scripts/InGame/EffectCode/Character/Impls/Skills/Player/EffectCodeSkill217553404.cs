using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
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
    // 스킬 스탯
    private ObfuscatorFloat _healRatePercent; // 치유력 비율 (1)
    private ObfuscatorFloat _damageRatePercent; // 피해 비율 (2)
    private ObfuscatorFloat _healReductionRatePercent; // 회복량 감소 비율 (3)
    private ObfuscatorFloat _debuffDuration; // 디버프 지속 시간 (4)

    // 스킬 상태
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    
    // 채널링 스킬 설정
    private const float SKILL_DURATION = 3.0f; // 스킬 지속 시간 (초)
    private const float TICK_INTERVAL = 0.5f; // 틱 처리 간격 (초)
    private const int AREA_RANGE = 2; // 영역 범위 (맨해튼 거리)

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _damageRatePercent = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _healReductionRatePercent = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _debuffDuration = codeInfo.GetCodeStatToFloat(4);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRatePercent = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _damageRatePercent = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _healReductionRatePercent = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _debuffDuration = codeInfo.GetCodeStatToFloat(4);
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
            ProcessArea(centerTile).Forget();
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

    private async UniTask ProcessArea(InGameTile centerTile)
    {
        int tickCount = Mathf.CeilToInt(SKILL_DURATION / TICK_INTERVAL);
        
        for (int tickIndex = 0; tickIndex < tickCount; tickIndex++)
        {
            if (owner == null || owner.CurrentTile == null)
                break;

            // 매 틱마다 최신 영역 타일 리스트를 가져와서 처리
            var currentAreaTiles = GetAreaTiles(centerTile);
            
            // 힐 적용 시점에 모든 타일이 빛나도록 이펙트 표시
            PlayTileEffectsForTick(tickIndex, currentAreaTiles);
            
            // 영역 내 캐릭터들 처리
            ProcessTilesInArea(currentAreaTiles);
            // 마지막 tick에서는 delay 없이 종료
            if (tickIndex < tickCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(TICK_INTERVAL));
            }
        }

    }

    private void PlayAreaEffect(InGameTile centerTile, List<InGameTile> areaTiles)
    {
        if (owner == null)
            return;

        // 중심 타일 VFX 생성
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], centerTile.View.CachedTr.position);
        
        // 영역 타일들에 타일 이펙트 표시
        foreach (var areaTile in areaTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, areaTile);
        }
    }

    private void ProcessTilesInArea(List<InGameTile> areaTiles)
    {
        if (owner == null)
            return;

        foreach (var tile in areaTiles)
        {
            if (tile.OccupiedCharacter == null)
                continue;

            var character = tile.OccupiedCharacter;
            
            // 아군 처리: 치유 + CC제거
            if (character.AllianceType == owner.AllianceType)
            {
                ProcessMyTeamInArea(character);
            }
            // 적군 처리: 피해 + 회복량 감소 디버프
            else if (tile.CheckValidTile(owner.AllianceType, false))
            {
                ProcessEnemyInArea(character);
            }
        }
    }

    private void PlayTileEffectsForTick(int tickIndex, List<InGameTile> areaTiles)
    {
        if (owner == null || tickIndex % 2 == 1)
            return;

        // 힐 적용 시점에 영역 내 모든 타일이 빛나도록 이펙트 표시
        foreach (var areaTile in areaTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, areaTile);
        }
    }

    private void ProcessMyTeamInArea(CharacterController ally)
    {
        // 치유
        double healAmount = owner.PostCalculateHealAmount(owner.AP * _healRatePercent, ally);
        ally.GetHealed(healAmount, owner, codeId, true);
        
        // CC 제거
        EffectCodeHelper.RemoveAllDebuff(ally);
        EffectCodeHelper.RemoveAllCrowdControl(ally);
    }

    private void ProcessEnemyInArea(CharacterController enemy)
    {
        // 피해
        var damage = owner.CalculateDamageAmount(owner.AD * _damageRatePercent, 0, enemy, codeId, true);
        enemy.GetDamaged(damage, owner);
        
        // 회복량 감소 디버프 적용
        Span<double> eccStats = stackalloc double[3];
        eccStats[0] = codeId;
        eccStats[1] = _debuffDuration;
        eccStats[2] = _healReductionRatePercent;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_HEAL_RATE_DOWN, enemy, eccStats, source);
    }


    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}
