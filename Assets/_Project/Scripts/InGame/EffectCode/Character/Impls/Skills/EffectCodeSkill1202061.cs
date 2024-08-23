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
/// 5챕터 정글 버팔로
// 공중으로 {0}초 동안 튀어 오른다, 이 때 타겟 불가능 상태가 된다. 
// 현재 체력 비율이 가장 낮은 적 앞에 착지하면서, 자신 주변 3*3 범위에 공격력 {1}%의 대미지를 주고 
// {2}초 동안 스턴 상태를 부여한다. 
/// </summary>
[UseEffectCodeIds(1202061)]
public class EffectCodeSkill1202061 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _time;
    private ObfuscatorFloat _damageRate;
    private ObfuscatorFloat _stunTime;

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
        _stunTime = codeInfo.GetCodeStatToFloat(3);
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
        _stunTime = codeInfo.GetCodeStatToFloat(3);
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
        MoveUp(moveTime, distance).Forget();
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
        await UniTask.Delay(TimeSpan.FromSeconds(_time));
        MoveDown(moveTime, distance).Forget();
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1],owner.CurrentTile.View.CachedTr.position);
        
        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.Target.CurrentTile, 1);
        var characterControllers = InGameObjectManager.Instance.GetCharacterListSortedByHpRate(owner.AllianceType, false);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,
                characterControllers[0].CurrentTile);

            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);
                
                var damage = owner.PrecalculateDamageAmount(owner.AD * _damageRate, 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);
                
                StunCharacter(tile.OccupiedCharacter);
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
    
    private void StunCharacter(CharacterController character)
    {
        Span<double> eccStats = stackalloc double[1];
        eccStats.Clear();
        eccStats[0] = _stunTime;
        
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.STUN, character, eccStats, source);
    }
}
