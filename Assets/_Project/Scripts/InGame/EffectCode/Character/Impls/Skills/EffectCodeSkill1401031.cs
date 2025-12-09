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
/// 오데트
// 범위 : 전방 X축 3칸
// 대미지 : 낫을 크게 휘둘러, 적에게 공격력 {0}%의 대미지를 준다.
//     특수 효과 : 피격된 적은 {1}동안 공격속도가 {2}% 감소한다.
/// </summary>
[UseEffectCodeIds(1401031)]
public partial class EffectCodeSkill1401031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;
    private ObfuscatorFloat _debuffTime;
    private ObfuscatorFloat _atkSpeedDownRate;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _atkSpeedDownRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _atkSpeedDownRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        ExecuteSkillRoutine(0.2f).Forget();

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTaskVoid ExecuteSkillRoutine(float WaitTime)
    {
        if (owner.Target == null)
            return;

        IsSkillActivated = false;
        
        List<InGameTile> inGameTiles = null;
        
        inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 1, 1);
        await ExecuteSkillStep(inGameTiles);
        await UniTask.Delay(TimeSpan.FromSeconds(WaitTime));

        inGameTiles.Clear();
        inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 2, 1);
        await ExecuteSkillStep(inGameTiles);
        await UniTask.Delay(TimeSpan.FromSeconds(WaitTime));

        inGameTiles.Clear();
        inGameTiles =InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 3, 1);
        await ExecuteSkillStep(inGameTiles);
        
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }
    
    
    private async UniTask ExecuteSkillStep(List<InGameTile> inGameTiles)
    {
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);

            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);

                var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter, codeId,
                    true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);

                tile.OccupiedCharacter.GetDamaged(damage, owner);

                double[] eccStats = new double[3];
                eccStats[0] = codeId;
                eccStats[1] = _debuffTime;
                eccStats[2] = _atkSpeedDownRate;
                    
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN, tile.OccupiedCharacter, eccStats, source);
            }
        }
                    
        UniTask.Yield();
    }
}
