using System;
using System.Linq;
using System.Threading.Tasks;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;


/// <summary>
/// 빅마우스 
/// 대상: 대상: 가장 가까운 직선의 적 -> 이동하여 공격
// 효과: 직선 3칸에 {1}% 만큼 피해를 입히고 {2}초간 기절 상태로 만든다.
/// </summary>
[UseEffectCodeIds(240107002)]
public partial class EffectCodeSkill240107002 : EffectCodeCharacterBase
{
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private float _damageRate;
    private float _debuffTime;

    // 이동 관련 변수
    private InGameTile _targetMoveTile;
    private bool _isMoving;
    private Vector3 _moveStartPosition;
    private Vector3 _moveTargetPosition;
    private float _moveSpeed;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _isReadyToActivate = false;
        IsSkillActivated = false;
        _isMoving = false;
        _isMoving = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
    }

    public override void OnUpdate(float dt)
    {
        if (!IsSkillActivated)
        {
            return;
        }

        // 이동 중이면 MoveTowards로 위치 업데이트
        if (_isMoving && owner != null)
        {
            owner.Position3D = Vector3.MoveTowards(owner.Position3D, _moveTargetPosition, _moveSpeed * dt);
            
            // 목표 위치에 도달했는지 확인
            if (Vector3.Distance(owner.Position3D, _moveTargetPosition) < 0.01f)
            {
                owner.Position3D = _moveTargetPosition;
                _isMoving = false;
            }
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
        CharacterController targetCharacter = FindNearestEnemyInLine();
        if (targetCharacter == null)
        {
            IsSkillActivated = false;
            return;
        }
        else
        {
            var isInRange = InGameObjectManager.Instance.IsInRange(owner, targetCharacter);
            if (!isInRange)
            {
                if (owner.Target != null)
                {
                    InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(owner.CurrentTile,
                        owner.Target.CurrentTile);
                    owner.MoveTile(bestTile);
                    IsSkillActivated = false;
                }
                return;
            }
        }
        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;


        // 돌진 이동 및 공격 실행
        DashAndAttack(owner.Target);
    }

    /// <summary>
    /// 캐릭터가 보고 있는 방향의 직선으로 가장 가까운 적을 찾습니다.
    /// </summary>
    private CharacterController FindNearestEnemyInLine()
    {
        // 캐릭터가 보고 있는 방향의 직선 타일들을 가져옴 (최대 10칸까지 확인)
        var directionTiles = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 10);

        foreach (var tile in directionTiles)
        {
            if (tile == null || tile == owner.CurrentTile)
                continue;

            // 적이 있는 타일을 찾으면 반환
            if (tile.CheckValidTile(owner.AllianceType, false) && tile.OccupiedCharacter != null)
            {
                var character = tile.OccupiedCharacter;
                if (character.IsAlive)
                {
                    return character;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 적에게 돌진하여 이동하고 직선 3칸 범위에 데미지와 기절을 적용합니다.
    /// </summary>
    private void DashAndAttack(CharacterController targetCharacter)
    {
        if (targetCharacter == null || targetCharacter.CurrentTile == null)
        {
            IsSkillActivated = false;
            return;
        }

        // 적의 위치로 돌진 이동
        InGameTile targetTile = null;

        // 적의 앞 타일로 이동 (적과 같은 타일이면 적의 뒤 타일로)
        var directionTiles = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 3);
        if (directionTiles != null && directionTiles.Count > 0)
        {
            // 적이 있는 타일 바로 앞으로 이동
            var targetTileCandidate = directionTiles[directionTiles.Count - 1];
            if (targetTileCandidate != null)
            {
                targetTile = targetTileCandidate;
            }
        }

        // 이동 가능한 타일이 있으면 서서히 이동
        if (targetTile != null)
        {
            MoveToTileSmoothly(targetTile);
        }

        // 직선 3칸 범위 타일 가져오기
        var attackTiles = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 3);

        // 타일 이펙트 표시
        foreach (var tile in attackTiles)
        {
            if (tile != null)
            {
                InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            }
        }

        // 공격 및 기절 적용
        foreach (var tile in attackTiles)
        {
            if (tile == null)
                continue;

            // InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);

            if (tile.CheckValidTile(owner.AllianceType, false) && tile.OccupiedCharacter != null)
            {
                var target = tile.OccupiedCharacter;
                if (!target.IsAlive)
                    continue;

                // 데미지 적용
                var damageValue = owner.SpecCharacter.atk_type is AtkType.AD ? owner.AD : owner.AP;
                var damage = owner.CalculateDamageAmount(damageValue * _damageRate, 0, target, codeId, true);
                target.GetDamaged(damage, owner);

                // 기절 적용
                Span<double> eccStats = stackalloc double[1];
                eccStats.Clear();
                eccStats[0] = _debuffTime;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, target, eccStats, source);
            }
        }

        IsSkillActivated = false;
    }

    /// <summary>
    /// 캐릭터를 지정된 타일로 스킬 듀레이션 동안 서서히 이동시킵니다.
    /// </summary>
    private void MoveToTileSmoothly(InGameTile targetTile)
    {
        if (targetTile == null || owner == null)
            return;

        // 타일 변경은 즉시 수행
        owner.ChangeOccupiedTile(targetTile);

        _targetMoveTile = targetTile;

        // 이동 시작 위치와 목표 위치 저장
        _moveStartPosition = owner.Position3D;
        _moveTargetPosition = targetTile.View.Position;

        // 스킬 애니메이션 시간 (1.0초) 사용
        float duration = 1.0f;

        // 거리에 따라 duration 조정 (넉백 코드 참고)
        float distance = Vector3.Distance(_moveStartPosition, _moveTargetPosition);
        duration += distance * 0.1f;

        // 이동 속도 계산 (거리 / 시간)
        _moveSpeed = distance / duration;

        // 이동 시작
        _isMoving = true;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}//240107002
