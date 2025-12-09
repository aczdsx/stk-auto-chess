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
/// 4챕터 샌드웜
// 샌드웜이 바닥으로 {0}초 동안 들어간다, 이 때 타겟 불가능 상태가 된다. 
//  현재 DPS 가장 높은 적에게 큰 가시를 소환해 공격력 {1}%의 대미지를 준다. 
//  그 주변 범위에는 작은 가시를 소환해 {2}%의 대미지를 준다. 
/// </summary>
[UseEffectCodeIds(1202051)]
public partial class EffectCodeSkill1202051 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _time;
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _additionalDamageRate;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _time = codeInfo.GetCodeStatToFloat(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _time = codeInfo.GetCodeStatToFloat(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _additionalDamageRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        
        if (owner.Target == null)
            return;
        
        // 타겟 불가능 상태
        {
            Span<double> eccStats = stackalloc double[3];
            eccStats.Clear();
            eccStats[0] = codeId;
            eccStats[1] = _time;
            eccStats[2] = 1.0f;
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.TARGET_IMPOSSIBLE, owner, eccStats, source);
        }
        AfterAction().Forget();
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private async UniTask AfterAction()
    {
        float moveTime = 1.0f;
        float distance = 10.0f;
        // MoveDown(moveTime, distance).Forget();
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        // MoveUp(moveTime, distance).Forget();

        var characterControllers =
            InGameObjectManager.Instance.GetCharacterListSortedByADDescending(owner.AllianceType, false);
        if (characterControllers.Count > 0)
        {
            var inGameTiles =
                InGameObjectManager.Instance.InGameGrid.GetTileListByManhattanDistanceInRange(characterControllers[0].CurrentTile,
                    2);
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                characterControllers[0].CurrentTile.View.CachedTr.position);
            float calculatedDamageRate = _damageRate;
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type,
                characterControllers[0].CurrentTile);

            var damage = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * calculatedDamageRate,
                characterControllers[0], codeId, true);
            owner.PostCalculateDamageAmount(ref damage, characterControllers[0]);
            characterControllers[0].GetDamaged(damage, owner);

            // 주변 타겟
            inGameTiles.Remove(characterControllers[0].CurrentTile);
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.View.CachedTr.position);

                if (tile.CheckValidTile(owner.AllianceType, false))
                {
                    InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                        tile.OccupiedCharacter.SkillRootTransformFollowable);

                    float calculatedDamageRate2 = _additionalDamageRate;

                    var damage2 = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * calculatedDamageRate2,
                        tile.OccupiedCharacter, codeId, true);
                    owner.PostCalculateDamageAmount(ref damage2, tile.OccupiedCharacter);
                    tile.OccupiedCharacter.GetDamaged(damage2, owner);
                }
            }
        }

        IsSkillActivated = false;
    }
    
    private async UniTask MoveDown(float duration, float distance)
    {
        float startHeight = owner.ViewPosition3D.y;
        float endHeight = startHeight - distance;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newY = Mathf.Lerp(startHeight, endHeight, elapsedTime / duration);
            var pos = owner.ViewPosition3D;
            pos.y = newY;
            owner.ViewPosition3D = pos;
            await UniTask.Yield();
        }
    }

    private async UniTask MoveUp(float duration, float distance)
    {
        float startHeight = owner.ViewPosition3D.y;
        float endHeight = startHeight + distance;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newY = Mathf.Lerp(startHeight, endHeight, elapsedTime / duration);
            var pos = owner.ViewPosition3D;
            pos.y = newY;
            owner.ViewPosition3D = pos;
            await UniTask.Yield();
        }
    }
}
