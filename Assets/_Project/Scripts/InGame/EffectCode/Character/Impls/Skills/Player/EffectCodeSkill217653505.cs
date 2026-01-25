using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Unity.Mathematics;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 엔키
/// 대상 : 아군 전체
/// 효과 : 
/// 물결을 일으켜 아군을 엔키의 치유력 {1}% 만큼 치유하고, 
/// {2}초간 지속되는 {3}%위력의 지속 회복을 부여합니다.
/// </summary>
[UseEffectCodeIds(217653505)]
public partial class EffectCodeSkill217653505 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _healRate;
    private ObfuscatorFloat _buffTime;
    private ObfuscatorFloat _healBuffRate;
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private static readonly InGameVfxNameType _waveVfxName = InGameVfxNameType.Skill_406011_wave;
    private int _waveSize = 5;
    private List<CharacterController> _hitCharacters = new List<CharacterController>();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _healBuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        SkillIndex = 0;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _healRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _buffTime = codeInfo.GetCodeStatToFloat(2);
        _healBuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(owner.SpecCharacter.character_element_type,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        var ownerTile = owner.CurrentTile;
        var waveStartY = Math.Clamp(ownerTile.Y - 2, 0, InGameObjectManager.Instance.InGameGrid.Height - 1);//엔키의 두칸뒤에서 시작한다.

        var inGameTile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(ownerTile.X, waveStartY));
        if (inGameTile == null)
            return;

        var waveStartTileList = InGameObjectManager.Instance.InGameGrid.GetTileListByRow(inGameTile, _waveSize / 2);
        var grid = InGameObjectManager.Instance.InGameGrid;

        foreach (var startTile in waveStartTileList)
        {
            // 시작 타일에서 +y 방향으로 맵 끝까지 이동할 목표 타일 찾기
            InGameTile endTile = null;
            for (int y = startTile.Y; y < grid.Height; y++)
            {
                var tile = grid.GetTile(new int2(startTile.X, y));
                if (tile != null)
                {
                    endTile = tile;
                }
            }

            if (endTile == null || endTile == startTile)
                continue;

            var vfx = InGameVfxManager.Instance.AddInGameVfx(_waveVfxName, startTile.View.CachedTr.position);
            var movement = InGameVfxMovementPool.Get<InGameVfxMovementLinear>();
            movement.SetData(vfx.CachedTr.position, endTile.View.CachedTr.position, 5);
            vfx.Initialize(false, movement);
            vfx.OnCollisionWithTile += OnTrigger2DEnter;

            // 목표 지점에 도달하면 VFX 제거
            movement.OnReachedTarget += () =>
            {
                vfx.Remove();
            };

        }
        IsSkillActivated = false;
    }

    private void OnTrigger2DEnter(InGameVfx.CollisionType type, InGameTile tile, InGameVfx vfx)
    {
        if (owner is null)
            return;

        var tileFx = InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
        if (tileFx != null)
        {
            tileFx.CachedTr.position = tile.View.CachedTr.position;

            if (tile.CheckValidTile(owner.AllianceType, true))
            {
                if (!_hitCharacters.Exists(l => l == tile.OccupiedCharacter))
                {
                    InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.OccupiedCharacter.SkillRootTransformFollowable);

                    // 즉시 힐량 계산 (PostCalculateHealAmount에서 오라클 처리)
                    double healAmount = EffectCodeJobPassiveRecovery.CalculateOracleDefaultSkillRecoveryAmount(owner, _healRate);
                    healAmount = owner.PostCalculateHealAmount(healAmount, tile.OccupiedCharacter, isSkill: true);
                    tile.OccupiedCharacter.GetHealed(healAmount, owner, codeId, true);

                    // 지속 회복 버프 힐량 계산 (PostCalculateHealAmount에서 오라클 처리)
                    healAmount = EffectCodeJobPassiveRecovery.CalculateOracleDefaultSkillRecoveryAmount(owner, _healBuffRate);
                    healAmount = owner.PostCalculateHealAmount(healAmount, tile.OccupiedCharacter, isSkill: true);
                    Span<double> eccStats = stackalloc double[3];

                    eccStats.Clear();
                    eccStats[0] = CodeId;//sourceCodeId
                    eccStats[1] = _buffTime;//duration
                    eccStats[2] = healAmount;//value

                    EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_SPECIAL_MEDITATION, tile.OccupiedCharacter, eccStats, source);

                    _hitCharacters.Add(tile.OccupiedCharacter);
                }
            }
        }
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

}
