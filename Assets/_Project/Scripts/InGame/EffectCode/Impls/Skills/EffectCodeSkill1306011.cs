using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 유니
// 대상 : 공격력이 가장 높은 아군 2명
// 효과 : 공격력을 {0}초 동안 {1}% 증가시킨다.
/// </summary>
[UseEffectCodeIds(1306011)]
public class EffectCodeSkill1306011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat duration;
    private ObfuscatorFloat atkUpRate;

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
        atkUpRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;;
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        duration = codeInfo.GetCodeStatToFloat(1);
        atkUpRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;;
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

        // 나한테 붙은 vfx
        _ownVfx = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[0], InGameObjectManager.Instance.Playground);
        _ownVfx.CachedTr.position = owner.GetCharacterView().SkillRootTransform.position;

        // Target 2명 찾기 + 2명의 위치에 _otherVfx 생성
        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTilesByCount(owner.AllianceType, 2);
        if (inGameTiles != null)
        {
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type, tile.View.CachedTr);

                _otherVfx = InGameVfxManager.Instance.AddInGameVfx(specSkill.skill_vfxs[1],
                    tile.OccupiedCharacter.GetCharacterView().SkillRootTransform);

                //[TODO] 해당 캐릭터에게 버프 생성
                // _otherVfx.CachedTr.position =tile.OccupiedCharacter.GetCharacterView().SkillRootTransform.position;
            }
        }

        isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        isSkillActivated = false;
    }
}
