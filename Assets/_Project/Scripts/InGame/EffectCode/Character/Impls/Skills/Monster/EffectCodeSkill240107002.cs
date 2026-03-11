using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using LitMotion;
using Unity.Mathematics;
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
    private InGameTile _originalTile;
    private MotionHandle _moveHandle;
    private static readonly float MOVE_DURATION = 0.5f; // 빠른 이동
    private static readonly float WAIT_DURATION = 1.5f; // 대기 시간
    private static readonly float WALK_IN_DURATION = 0.3f; // 걸어들어가는 연출 시간
    private static readonly float LAND_DURATION = 0.1f; // 점프 종료 연출 시간

    private InGameVfx _colliderVfx;
    private InGameVfx _jumpForwardPortalVfx; // 점프해서 달려갈때 포털 VFX
    private InGameVfx _jumpBackPortalVfx; // 점프해서 돌아오는 포털 VFX
    private Vector3 _jumpDirection;
    private List<CharacterController> _stunnedCharacters = new List<CharacterController>();
    private bool _isJumping;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);

        Span<double> buffStats = stackalloc double[3];

        buffStats.Clear();
        buffStats[0] = codeId;
        buffStats[1] = 999f;//duration
        buffStats[2] = 1;//value?

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_IMMUNE, owner, buffStats, source);


        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();

        _colliderVfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.SkillRootTransformFollowable);
        _colliderVfx.Initialize(false);
        // [InGame_New: removed] _colliderVfx.OnCollisionWithTile += OnCollision2DEnter;
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
        if (_isJumping)
            return;
        base.Activate();
        _colliderVfx.CachedGo.SetActive(true);
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

        _stunnedCharacters.Clear();
        owner.AddNextState<CharacterStateSkill>(this);
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(SynergyType.LIGHTNING,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        if (executeIndex == 0)
        {
            _colliderVfx.gameObject.SetActive(true);
            DashAndAttack(owner.Target);
        }
        else if (executeIndex == 1)
        {
            // 두 번째 호출: 점프 최종 위치로 이동
            MoveToFinalPosition();
            _colliderVfx.gameObject.SetActive(false);

            var vfXPosition = _originalTile.View.CachedTr.position;
            vfXPosition.y = owner.SkillTopFXTransformFollowable.GetPosition().y;
            // 돌아오는 포털 VFX 생성
            _jumpBackPortalVfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], vfXPosition - _jumpDirection * 1.8F);

            var direction = (_originalTile.View.CachedTr.position - _targetMoveTile.View.CachedTr.position).normalized;
            _jumpBackPortalVfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);
        }
        else if (executeIndex == 2)
        {
            // 돌아오는 포털 VFX 생성
            JumpEnd();
        }
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
    /// 적에게 돌진하여 이동합니다. (executeIndex 0에서 호출)
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

        // 적의 앞 타일로 이동
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

        // 이동 가능한 타일이 있으면 점프 시작
        if (targetTile != null)
        {
            StartJump(targetTile);
        }
    }

    /// <summary>
    /// 점프 시작 (executeIndex 0에서 호출)
    /// </summary>
    private void StartJump(InGameTile targetTile)
    {
        if (targetTile == null || owner == null)
            return;

        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_mon_skill_7002_01);

        _targetMoveTile = targetTile;
        _originalTile = owner.CurrentTile; // 원래 타일 저장

        // 이동 시작 위치와 목표 위치
        Vector3 startPosition = owner.Position3D;
        Vector3 targetPosition = targetTile.View.Position;
        _jumpDirection = (targetPosition - startPosition).normalized;

        // 기존 핸들이 있으면 중지
        _moveHandle.TryCancel();

        // 점프 시작
        _isJumping = true;

        var vfXPosition = targetPosition;
        vfXPosition.y = owner.SkillTopFXTransformFollowable.GetPosition().y;
        // 앞으로 들어가는 포털 VFX 생성
        _jumpForwardPortalVfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], vfXPosition);

        var direction = (targetPosition - startPosition).normalized;
        _jumpForwardPortalVfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90, 0);

        // 빠르게 목표 타일로 이동
        _moveHandle = LMotion.Create(
            startPosition,
            targetPosition,
            MOVE_DURATION)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() =>
            {
                // 목표 타일에 도달했을 때 타일 변경
                if (owner != null && _targetMoveTile != null)
                {
                    owner.ChangeOccupiedTile(_targetMoveTile);
                    owner.Position3D = _targetMoveTile.View.Position;
                    owner.GetCharacterView().CachedTr.localPosition = owner.Position3D;

                    _colliderVfx.CachedGo.SetActive(false);
                }
            })
            .Bind(value =>
            {
                if (owner != null)
                {
                    owner.Position3D = value;
                    owner.GetCharacterView().CachedTr.localPosition = value;
                }
            });
    }

    /// <summary>
    /// 점프 최종 위치로 이동 (executeIndex 1에서 호출)
    /// </summary>
    private void MoveToFinalPosition()
    {
        if (_targetMoveTile == null || owner == null)
            return;

        Vector3 currentPosition = owner.Position3D;
        Vector3 finalPosition = _targetMoveTile.View.Position +
            (_targetMoveTile.View.Position - _originalTile.View.Position).normalized * 1.8f;

        // 기존 핸들이 있으면 중지
        _moveHandle.TryCancel();

        // 걸어들어가는 연출 (Position3D만 더 앞으로 이동, 타일은 그대로)
        _moveHandle = LMotion.Create(
            currentPosition,
            finalPosition,
            WALK_IN_DURATION)
            .WithEase(Ease.Linear)
            .WithOnComplete(() =>
            {
                Debug.Log("MoveToFinalPosition Complete");
            })
            .Bind(value =>
            {
                if (owner != null)
                {
                    owner.Position3D = value;
                    owner.GetCharacterView().CachedTr.localPosition = value;
                }
            });
    }

    public void JumpEnd()
    {
        // 포털 VFX 제거
        if (_jumpForwardPortalVfx != null)
        {
            _jumpForwardPortalVfx.Remove();
            _jumpForwardPortalVfx = null;
        }

        _moveHandle.TryCancel();

        // 원래 타일로 즉시 이동
        owner.ChangeOccupiedTile(_originalTile);
        owner.Position3D = _originalTile.View.Position - _jumpDirection;
        owner.GetCharacterView().CachedTr.localPosition = owner.Position3D;

        // 걸어들어가는 연출 (Position3D만 더 앞으로 이동, 타일은 그대로)
        _moveHandle = LMotion.Create(
            owner.Position3D,
            owner.Position3D + _jumpDirection,
            LAND_DURATION)
            .WithEase(Ease.InExpo)
            .Bind(value =>
            {
                if (owner != null)
                {
                    owner.Position3D = value;
                    owner.GetCharacterView().CachedTr.localPosition = value;
                }
            });


        // 점프 완료 및 쿨타임 초기화
        _isJumping = false;
    }


    public override void OnSkillAnimationEnd()
    {
        if (_jumpBackPortalVfx != null)
        {
            _jumpBackPortalVfx.Remove();
            _jumpBackPortalVfx = null;
        }
        _jumpBackPortalVfx = null;
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }



    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }

    public override void OnPreRemoved()
    {
        _colliderVfx.gameObject.SetActive(false);
        _colliderVfx.Remove();
        if (_jumpForwardPortalVfx != null)
        {
            _jumpForwardPortalVfx.Remove();
            _jumpForwardPortalVfx = null;
        }
        if (_jumpBackPortalVfx != null)
        {
            _jumpBackPortalVfx.Remove();
            _jumpBackPortalVfx = null;
        }
        // 기존 핸들이 있으면 중지
        _moveHandle.TryCancel();

        base.OnPreRemoved();
    }

    // [InGame_New: removed] private void OnCollision2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    // [InGame_New: removed] {
        // [InGame_New: removed] // 점프 중일 때는 리턴
        // [InGame_New: removed] if (!_isJumping)
            // [InGame_New: removed] return;

        // [InGame_New: removed] if (tile == null || owner == null || !owner.IsAlive)
            // [InGame_New: removed] return;
        // [InGame_New: removed] InGameVfxManager.Instance.AddInGameTileFx(SynergyType.LIGHTNING, tile);

        // [InGame_New: removed] // 타일에 적 캐릭터가 있는지 확인
        // [InGame_New: removed] if (tile.CheckValidTile(owner.AllianceType, false))
        // [InGame_New: removed] {
            // [InGame_New: removed] var target = tile.OccupiedCharacter;

            // [InGame_New: removed] // 캐릭터가 살아있는지 확인
            // [InGame_New: removed] if (!target.IsAlive)
                // [InGame_New: removed] return;

            // [InGame_New: removed] // 이미 스턴을 받은 캐릭터인지 확인 (반복 스턴 방지)
            // [InGame_New: removed] if (_stunnedCharacters.Contains(target))
                // [InGame_New: removed] return;

            // [InGame_New: removed] // 스턴 적용
            // [InGame_New: removed] Span<double> eccStats = stackalloc double[1];
            // [InGame_New: removed] eccStats.Clear();
            // [InGame_New: removed] eccStats[0] = _debuffTime;
            // [InGame_New: removed] EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, target, eccStats, source);

            // [InGame_New: removed] // 스턴을 받은 캐릭터를 리스트에 추가
            // [InGame_New: removed] _stunnedCharacters.Add(target);
        // [InGame_New: removed] }
    // [InGame_New: removed] }
}
