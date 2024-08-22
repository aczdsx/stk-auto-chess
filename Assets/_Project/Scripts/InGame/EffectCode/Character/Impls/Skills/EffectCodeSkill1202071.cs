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
/// 6챕터 탱커 보스 (2챕터 시뮬)
// "범위 : 자신의 전방 1칸, 3칸, 5칸 부채꼴 범위 
// 대미지 : 
// -1칸, 3칸 : 공격력 {0}%*(1+물리 방어력/{1}의 대미지를 가한다.
// -5칸 : 공격력 {2}%*(1+물리 방어력/{1}의 대미지를 가한다. 
//     특수 효과 : 피격된 적을 넉백시킨다."
/// </summary>
/// 
[UseEffectCodeIds(1202071)]
public class EffectCodeSkill1202071 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _additionalDefValue;
    private ObfuscatorFloat _damageRate2;

    private bool _isReadyToActivate;
    private SpecSkill _specSkill;
    
    private float _elapsedTime;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDefValue = codeInfo.GetCodeStatToFloat(2);
        _damageRate2 = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _additionalDefValue = codeInfo.GetCodeStatToFloat(2);
        _damageRate2 = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        
        SkillProcess(0.2f).Forget();

        IsSkillActivated = false;
    }
    
    public async UniTaskVoid SkillProcess(float time)
    {
        var inGameTiles1 = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        var inGameTiles2 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 2, 1);
        var inGameTiles3 = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 3, 2);

        ProcessTiles(inGameTiles1, owner, _damageRate);
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        ProcessTiles(inGameTiles2, owner, _damageRate);
        await UniTask.Delay(TimeSpan.FromSeconds(time));
        ProcessTiles(inGameTiles3, owner, _damageRate2);

        IsSkillActivated = false;
    }
    
    private void OnSkillEnd()
    {
        IsSkillActivated = false;
        owner.AddNextState<CharacterStateIdle>();
        CoolTimeElapsedTime = CoolTimeDurationTime;
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
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);
                
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);

                var damage = owner.PrecalculateDamageAmount(owner.AD * powerRate, 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
                
                var inGameTile =
                    InGameObjectManager.Instance.InGameGrid.GetTileForKnockBack(owner.CurrentTile, tile.OccupiedCharacter.CurrentTile,
                        1);

                long effectCodeID = (long)EffectCodeNameType.KNOCKBACK;
                var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, 0.3f, 0.3f, inGameTile.View.ID);
                tile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, owner);
            }
        }
    }
}
