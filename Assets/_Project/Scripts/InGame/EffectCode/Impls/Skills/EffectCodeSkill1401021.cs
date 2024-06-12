using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 오데트
// 범위 : 전방 X축 3칸
// 대미지 : 낫을 크게 휘둘러, 적에게 공격력 {0}%의 대미지를 준다.
//     특수 효과 : 피격된 적은 {1}동안 공격력이 {2}% 감소한다.
/// </summary>
[UseEffectCodeIds(1401031)]
public class EffectCodeSkill1401031 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _cooltime;
    private ObfuscatorFloat _power;
    private ObfuscatorFloat _debuffTime;
    private ObfuscatorFloat _atkDownRate;

    private ObfuscatorFloat _elapsedTime;

    private bool _isReadyToActivate;
    private bool _isSkillActivated;

    private SpecSkill _specSkill;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _power = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _atkDownRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _elapsedTime = 0f;
        _isReadyToActivate = false;
        _isSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _cooltime = codeInfo.GetCodeStatToFloat(0);
        _power = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _atkDownRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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

        var inGameTiles = InGameObjectManager.Instance.InGameGrid.GetTilesByDirection(owner);
        foreach (var tile in inGameTiles)
        {
            InGameVfxManager.Instance.AddInGameTIleFx(owner.SpecCharacter.element_type, tile.View.CachedTr);
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr);

            if (tile.OccupiedCharacter != null)
            {
                var damage = owner.PrecalculateDamageAmount(owner.AD * _power, 0, tile.OccupiedCharacter, codeId, true);
                owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);

                //[TODO] 공격속도 디버프

            }
        }


        _isSkillActivated = false;
    }

    public override void OnSkillAnimationEnd()
    {
        base.OnSkillAnimationEnd();
        // _vfx.OnCollisionWithTile -= OnCollision2DEnter;
        _isSkillActivated = false;
    }
}
