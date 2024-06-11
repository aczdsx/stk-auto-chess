using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 멘샤
// 범위 : 멘샤와 동일한 열
// 효과 : 아군에게 {0}초 동안 멘샤 공격력 {1}%의 실드를 부여한다.
/// </summary>
[UseEffectCodeIds(1302011)]
public class EffectCodeSkill1302011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat duration;
    private ObfuscatorFloat shieldRate;

    private ObfuscatorFloat elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    private InGameVfx _ownVfx;
    private InGameVfx _otherVfx;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        duration = codeInfo.GetCodeStatToFloat(1);
        shieldRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        duration = codeInfo.GetCodeStatToFloat(1);
        shieldRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!isSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            elapsedTime = cooltime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (isReadyToActivate || isSkillActivated)
            return;
        elapsedTime += dt;
        if (elapsedTime >= cooltime)
        {
            isReadyToActivate = true;
        }
    }

    public override bool IsReadyToActivate()
    {
        return isReadyToActivate;
    }

    public override void Activate()
    {
        base.Activate();
        // TODO: Target Check
        isReadyToActivate = false;
        isSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        var specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();

        // 범위 : 멘샤와 동일한 열
        // 효과 : 아군에게 {0}초 동안 멘샤 공격력 {1}%의 실드를 부여한다.
        // var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTilesByRow(owner.CurrentTile.X);
        // if (inGameTiles != null)
        // {
        //     foreach (var tile in inGameTiles)
        //     {
        //         InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type, tile.View.CachedTr);
        //         if (tile.OccupiedCharacter != null)
        //         {
        //             _otherVfx = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[0],
        //                 tile.OccupiedCharacter.GetCharacterView().SkillRootTransform);
        //
        //             //[TODO] 해당 캐릭터에게 쉴드 생성
        //         }
        //     }
        // }

        isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        isSkillActivated = false;
    }
}
