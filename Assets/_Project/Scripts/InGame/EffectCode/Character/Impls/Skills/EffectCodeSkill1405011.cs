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
/// 베인
// "타겟 : 가장 가까이에 위치한 적 
// 대미지 : 적에게 부메랑을 던져 공격력 {0}%의 대미지를 준다. 
//     부메랑을 맞은 적 근처에 다른 적이 있으면, 부메랑이 그 적에게 튕긴다. 
//     부메랑이 튕길 때마다 대미지가 {1}%씩 줄어든다. 
//     부메랑은 최대 {4}명까지 공격가능하다. 
//     추가 효과 : 피격된 적 1명당 {2}초 동안 공격속도가 {3}% 증가한다. 
//     개발 사이드 필요 내용 : 튕긴 적에게 다시 튕기지는 않는다. "
/// </summary>
[UseEffectCodeIds(1405011)]
public partial class EffectCodeSkill1405011 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;
    private ObfuscatorFloat _decreasedPowerRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _buffRate;
    private ObfuscatorFloat _targetMaximumCount;

    private bool _isReadyToActivate;

    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    private InGameVfx _vfx;

    private SpecSkill _specSkill;
    private int _targetCount;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _decreasedPowerRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(3);
        _buffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        _targetMaximumCount = codeInfo.GetCodeStatToFloat(5);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _decreasedPowerRate = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(3);
        _buffRate = codeInfo.GetCodeStatToFloat(4) * 0.01f;
        _targetMaximumCount = codeInfo.GetCodeStatToFloat(5);
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
        _hitCharacters.Clear();
        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        _vfx = InGameVfxManager.Instance
            .AddInGameVfx(_specSkill.skill_vfxs[0], owner.GetCharacterView().CachedTr.position)
            .GetComponent<InGameVfx>();

        InGameTile targetTile = null;
        if (owner.Target is {IsAlive: true})
        {
            targetTile = owner.Target.CurrentTile;
        }
        else
        {
            var targetTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
            targetTile = targetTileList.Find(l => l.CheckValidTile(owner.AllianceType, false));
            if (targetTile == null)
            {
                targetTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 2);
                targetTile = targetTileList.Find(l => l.CheckValidTile(owner.AllianceType, false));
                if (targetTile == null)
                {
                    return;
                }
            }
        }

        _targetCount = 0;
        _hitCharacters.Clear();
        if (targetTile != null)
            ActionSkill(targetTile);

        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }

    private void ActionSkill(InGameTile targetTile)
    {
        if (targetTile == null || _targetCount > _targetMaximumCount)
        {
            _vfx.Remove();
            ActionSkillBuff();
            return;
        }
        
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
        bool isHasTarget = false;
        bool isTargetFound = false;
        Vector3 direction = (targetTile.View.CachedTr.position - _vfx.CachedTr.position).normalized;
        _vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        movement.SetData(_vfx.CachedTr.position, targetTile.View.CachedTr.position, 30);
        _vfx.Initialize(false, movement);

        void OnReachedTargetHandler()
        {
            if (targetTile.OccupiedCharacter != null)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type, targetTile);
                InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1],
                    targetTile.OccupiedCharacter.GetCharacterView().CachedTr.position);

                float powerRate = _powerRate - _decreasedPowerRate * (float) _targetCount;
                var damage = owner.PrecalculateDamageAmount(owner.AD * powerRate, 0, targetTile.OccupiedCharacter,
                    codeId, true);
                owner.PostCalculateDamageAmount(ref damage, targetTile.OccupiedCharacter);
                targetTile.OccupiedCharacter.GetDamaged(damage, owner);

                _hitCharacters.Add(targetTile.OccupiedCharacter);
            }

            _targetCount++;
            movement = null;

            InGameTile pivotTile = targetTile;

            var targetTileList =
                InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(pivotTile, 1);
            targetTileList.RemoveAll(l => l.CheckValidTile(owner.AllianceType, false) && _hitCharacters.Exists(hl => l.OccupiedCharacter == hl));
            targetTile = targetTileList.Find(l => l.CheckValidTile(owner.AllianceType, false));

            if (targetTile == null)
            {
                targetTileList =
                    InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(pivotTile, 2);
                targetTileList.RemoveAll(l => l.CheckValidTile(owner.AllianceType, false) && _hitCharacters.Exists(hl => l.OccupiedCharacter == hl));
                targetTile = targetTileList.Find(l => l.CheckValidTile(owner.AllianceType, false));
            }

            ActionSkill(targetTile);
        }

        movement.OnReachedTarget += OnReachedTargetHandler;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private void ActionSkillBuff()
    {
        double[] eccStats = new double[3];
        eccStats[0] = codeId;
        eccStats[1] = _buffTime;
        eccStats[2] = _buffRate * (float) _hitCharacters.Count;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_ATK_SPEED_UP, owner, eccStats, source);

        _hitCharacters.Clear();
    }
}