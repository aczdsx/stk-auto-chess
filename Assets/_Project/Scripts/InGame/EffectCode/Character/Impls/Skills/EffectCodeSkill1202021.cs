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
/// 1챕터 보스 탱커
// 범위 : 자신 중심 십자가 전범위
// 대미지 : 공격력 {0}%의 대미지를 가한다.
//     특수 효과 : 피격된 적을 {1}초 동안 스턴시킨다.
/// </summary>
[UseEffectCodeIds(1202021)]
public class EffectCodeSkill1202021 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;
    private ObfuscatorFloat _stunTime;

    private bool _isReadyToActivate;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _stunTime = codeInfo.GetCodeStatToFloat(2);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _stunTime = codeInfo.GetCodeStatToFloat(2);
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
        // TODO: Target Check
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeX(owner.CurrentTile);
        inGameTiles.RemoveAll(l => l.OccupiedCharacter == owner);

        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, tile.View.CachedTr.position);
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);
        }

        OnSkillExecuteAsync(0.2f, inGameTiles).Forget();

        IsSkillActivated = false;
    }

    public async UniTask OnSkillExecuteAsync(float second, List<InGameTile> inGameTiles)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(second)); // n초 대기

        InGameCommanderManager.Instance.InGameCamera.ShakeCamera(0.3f, 0.25f);
        foreach (var tile in inGameTiles)
        {
            if (tile.OccupiedCharacter != null)
            {
                if (tile.OccupiedCharacter.AllianceType != owner.AllianceType)
                {
                    var damage = owner.PrecalculateDamageAmount(owner.AD * _powerRate, 0, tile.OccupiedCharacter,
                        codeId, true);
                    owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                    tile.OccupiedCharacter.GetDamaged(damage, owner);

                    StunCharacter(tile);
                }
            }
        }

        IsSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private void StunCharacter(InGameTile tile)
    {
        Span<double> eccStats = stackalloc double[1];
        eccStats.Clear();
        eccStats[0] = _stunTime;

        long effectCodeID = (long)EffectCodeNameType.STUN;
        var effectCodeInfo = new EffectCodeInfo(effectCodeID, 0, eccStats);
        tile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, owner);
    }
}
