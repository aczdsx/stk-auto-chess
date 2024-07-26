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
/// 6챕터 샌드웜
// "타겟 : 가장 딜이 높은 적
// 스킬 시전 : 샌드웜이 바닥으로 {0}초 동안 들어간다, 이 때 타겟 불가능 상태가 된다. 
//     스킬 범위 : 딜이 높은 적이 속한 x 축 좌우 전체 
// 스킬 내용 : 공격력 {1}%의 대미지를 준다. 
//     특수 효과 : 해당 적의 공격속도를 {2}초 동안 {3}% 느리게 한다. "
/// </summary>
[UseEffectCodeIds(1202081)]
public class EffectCodeSkill1202081 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _time;
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _debuffTime;
    private ObfuscatorFloat _debuffRate;

    private bool _isReadyToActivate;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _time = codeInfo.GetCodeStatToFloat(1);
        _damageRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(3);
        _debuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
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
        _debuffTime = codeInfo.GetCodeStatToFloat(3);
        _debuffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
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
        MoveDown(moveTime, distance).Forget();
        await UniTask.Delay(TimeSpan.FromSeconds(_time));
        MoveUp(moveTime, distance).Forget();

        var characterControllers =
            InGameObjectManager.Instance.GetCharacterListSortedByADDescending(owner.AllianceType, false);
        
        if (characterControllers.Count > 0)
        {
            var inGameTiles =
                InGameObjectManager.Instance.InGameGrid.GetTileListByRow(owner.Target.CurrentTile, 2);
            
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                characterControllers[0].CurrentTile.View.CachedTr.position);
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                characterControllers[0].CurrentTile.View.CachedTr.position);

            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                    tile.View.CachedTr.position);
                
                tile.CheckValidTile(owner.AllianceType, false, () =>
                {
                    InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], tile.View.CachedTr.position);

                    var damage = owner.PrecalculateDamageAmount(owner.AD * 0, owner.AP * _damageRate,
                        tile.OccupiedCharacter, codeId, true);
                    owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                    tile.OccupiedCharacter.GetDamaged(damage, owner);

                    Span<double> eccStats = stackalloc double[3];
                    eccStats.Clear();
                    eccStats[0] = codeId;
                    eccStats[1] = _debuffTime;
                    eccStats[2] = _debuffRate;

                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN,
                        tile.OccupiedCharacter, eccStats, source);
                });
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