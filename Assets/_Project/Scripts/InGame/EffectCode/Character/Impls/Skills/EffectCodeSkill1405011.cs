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
public class EffectCodeSkill1405011 : EffectCodeCharacterBase
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

        // vfx 생성
        _vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.CurrentTile.View.CachedTr.position);
        var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        if (inGameTile != null)
        {
            Vector3 direction = (inGameTile[0].View.CachedTr.position - _vfx.CachedTr.position).normalized;
            _vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

            movement.SetData(_vfx.CachedTr.position, inGameTile[0].View.CachedTr.position, 15);
            _vfx.Initialize(false, movement);
            _vfx.OnCollisionWithTile += OnCollision2DEnter;
        }
        
        ActiveSkill().Forget();

        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }

    private async UniTask ActiveSkill()
    {
        CharacterController targetController = InGameObjectManager.Instance.GetNearestTargetByManhattanDistance(owner);
        while (true)
        {
            if (_hitCharacters.Count >= _targetMaximumCount)
                break;
            
            await MoveVfxToTarget(_vfx, targetController.CurrentTile.View.Position, 10.0f);
            var tiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
            var targetTile = tiles.Find(l =>
                l.OccupiedCharacter != null && l.OccupiedCharacter.AllianceType != AllianceType.Wall &&
                l.OccupiedCharacter.AllianceType != owner.AllianceType && !_hitCharacters.Contains(l.OccupiedCharacter));
            
            if (targetTile == null)
                break;
            targetController = targetTile.OccupiedCharacter;
        }
        
        double[] eccStats = new double[3];
        eccStats[0] = codeId;
        eccStats[1] = _buffTime;
        eccStats[2] = _buffRate * (float)_hitCharacters.Count;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_ATK_SPEED_UP, owner, eccStats, source);
        
        _hitCharacters.Clear();
    }
    
    private async UniTask MoveVfxToTarget(InGameVfx vfx, Vector3 targetPosition, float speed)
    {
        while (Vector3.Distance(vfx.CachedTr.position, targetPosition) > 0.01f)
        {
            vfx.CachedTr.position = Vector3.MoveTowards(vfx.CachedTr.position, targetPosition, speed * Time.deltaTime);
            await UniTask.Yield();
        }

        vfx.CachedTr.position = targetPosition;
    }

    private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.element_type,tile.View.CachedTr.position);
        tileFx.CachedTr.position = tile.View.CachedTr.position;

        if (tile.OccupiedCharacter == null)
            return;

        if (tile.OccupiedCharacter.AllianceType == AllianceType.Wall)
            return;

        if (_hitCharacters.Contains(tile.OccupiedCharacter))
            return;

        if(owner.AllianceType == tile.OccupiedCharacter.AllianceType)
            return;

        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.CurrentTile.View.CachedTr.position);

        double damageRate = owner.AD * (_powerRate - _decreasedPowerRate * (float)_hitCharacters.Count);
        var damage = owner.PrecalculateDamageAmount(damageRate, 0, tile.OccupiedCharacter, codeId, true);
        owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
        tile.OccupiedCharacter.GetDamaged(damage, owner);

        _hitCharacters.Add(tile.OccupiedCharacter);
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }
}
