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
    private ObfuscatorFloat _cooltime;
    private ObfuscatorFloat _duration;
    private ObfuscatorFloat _shieldRate;

    private ObfuscatorFloat _elapsedTime;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _duration = codeInfo.GetCodeStatToFloat(1);
        _shieldRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _elapsedTime = 0f;
        _isReadyToActivate = false;
        _isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _duration = codeInfo.GetCodeStatToFloat(1);
        _shieldRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        if (!_isSkillActivated)
        {
            return;
        }

        // target check
        if (false)
        {
            owner.AddNextState<CharacterStateIdle>();
            _elapsedTime = _cooltime;
        }
    }

    public override void OnCooltime(float dt)
    {
        if (_isReadyToActivate || _isSkillActivated)
            return;
        _elapsedTime += dt;
        if (_elapsedTime >= _cooltime)
        {
            _isReadyToActivate = true;
        }
        // owner.GetHpBarView().OnCoolTimeUpdated();
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
        _isSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTilesByRow(owner.CurrentTile);
        if (inGameTiles != null)
        {
            foreach (var tile in inGameTiles)
            {
                InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type, tile.View.CachedTr);
                if (tile.OccupiedCharacter != null)
                {
                    if (tile.OccupiedCharacter.AllianceType == owner.AllianceType)
                    {
                        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0],
                            tile.OccupiedCharacter.GetCharacterView().SkillRootTransform);

                        var shieldAmount = owner.PrecalculateDamageAmount(owner.AD * _shieldRate, 0, tile.OccupiedCharacter,
                            codeId, true);
                        var effectCodeInfo = new EffectCodeInfo(EffectCodeBuffShield.CodeId, 0, 2, _duration,
                            shieldAmount.damageAmount);
                        tile.OccupiedCharacter.GetEffectCodeContainer().AddOrMergeEffectCode(effectCodeInfo, owner);
                    }
                }
            }
        }

        _isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        _elapsedTime = 0;
        _isSkillActivated = false;
    }
}
