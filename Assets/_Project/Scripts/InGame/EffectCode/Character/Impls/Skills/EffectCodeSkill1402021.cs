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
/// 아드리아
// 범위 : 자기 중심 
// 대미지 : 아드리아 공격력{0}% 대미지를 입히고, 물리 방어력에 비례해 피해가 증가한다. 
//     추가 효과 : 범위 내에 위치한 적은 {2}초 동안 스턴에 걸린다.
//     개발용 대미지 계산식 : 아드리아 공격력*{0}*(1+물리 방어력/{1})
/// </summary>
[UseEffectCodeIds(1402021)]
public class EffectCodeSkill1402021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _defValue;
    private ObfuscatorFloat _stunTime;

    private const float WaitTime = 0.5f;

    private bool _isReadyToActivate;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _defValue = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _stunTime = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _defValue = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _stunTime = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        ExecuteSkillRoutine().Forget();

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
    
    private async UniTaskVoid ExecuteSkillRoutine()
    {
        if (owner.Target == null)
            return;

        IsSkillActivated = false;
        
        List<InGameTile> inGameTiles = null;
        
        inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(owner.CurrentTile, 1);
        await ExecuteSkillStep(inGameTiles);
        await UniTask.Delay(TimeSpan.FromSeconds(WaitTime));

        inGameTiles.Clear();
        inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByDiagonal(owner.CurrentTile, 1);
        await ExecuteSkillStep(inGameTiles);
        await UniTask.Delay(TimeSpan.FromSeconds(WaitTime));

        inGameTiles.Clear();
        inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(owner.CurrentTile, 2);
        await ExecuteSkillStep(inGameTiles);
        await UniTask.Delay(TimeSpan.FromSeconds(WaitTime));
    }
    
    private async UniTask ExecuteSkillStep(List<InGameTile> inGameTiles)
    {
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
            tile.CheckValidTile(owner.AllianceType, false, () =>
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);
                
                var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.OccupiedCharacter.CurrentTile.View.CachedTr.position);

                var damage = owner.PrecalculateDamageAmount(owner.AD * _damageRate * (1 + owner.DEF / _defValue), 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);

                tile.OccupiedCharacter.GetDamaged(damage, owner);

                double[] eccStats = new double[1];
                eccStats[0] = _stunTime;
                    
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.STUN, tile.OccupiedCharacter, eccStats, source);
            });
        }
                    
        UniTask.Yield();
    }

}
