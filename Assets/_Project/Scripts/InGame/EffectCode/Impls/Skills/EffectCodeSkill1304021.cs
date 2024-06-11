using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 멘샤
// 대상 : 가장 가까운 적
// 대미지 : 샷건을 발사해 필리아 공격력 {0}%의 대미지를 가한다.
//     특수 효과 : 스킬로 적을 사망 시켰을 시, 스킬 쿨타임이 즉시 초기화된다.
/// </summary>
[UseEffectCodeIds(1304021)]
public class EffectCodeSkill1304021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat cooltime;
    private ObfuscatorFloat powerRate;

    private ObfuscatorFloat elapsedTime;

    private bool isReadyToActivate;
    private bool isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        powerRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        elapsedTime = 0f;
        isReadyToActivate = false;
        isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        cooltime = codeInfo.GetCodeStatToFloat(0);
        powerRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTilesByRow(owner.CurrentTile.X);
        if (inGameTiles != null)
        {
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type, tile.View.CachedTr);
                if (tile.OccupiedCharacter != null)
                {
                    var _otherVfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                        tile.OccupiedCharacter.GetCharacterView().SkillRootTransform);

                    //[TODO] 해당 캐릭터에게 쉴드 생성
                }
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
