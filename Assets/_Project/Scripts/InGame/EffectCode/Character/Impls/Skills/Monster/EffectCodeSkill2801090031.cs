using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 시련 보스 스킬
/// </summary>
/// 
[UseEffectCodeIds(280109003)]
public partial class EffectCodeSkill280109003 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate1;
    private ObfuscatorFloat _stunTime;

    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private int _count = 0;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate1 = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _stunTime = codeInfo.GetCodeStatToFloat(2);
        _isReadyToActivate = false;
        IsSkillActivated = false;
        _count = 0;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate1 = codeInfo.GetCodeStatToFloat(1) * 0.01f;
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

        _isReadyToActivate = false;
        IsSkillActivated = true;
        owner.AddNextState<CharacterStateSkill>(this);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);
        if (owner.Target == null)
            return;

        if (_count == 0)
        {
            var inGameTiles =
                InGameObjectManager.Instance.InGameGrid.GetTileListByColumn(owner.Target.CurrentTile);
            inGameTiles = inGameTiles.OrderByDescending(t => t.Y).ToList();
            ProcessTiles(inGameTiles, owner, _powerRate1).Forget();
        }
        else if (_count == 1)
        {
            var inGameTiles =
                InGameObjectManager.Instance.InGameGrid.GetTileListByColumn(owner.Target.CurrentTile);
            inGameTiles = inGameTiles.OrderByDescending(t => t.Y).ToList();
            inGameTiles.AddRange(InGameObjectManager.Instance.InGameGrid.GetTileListByRow(owner.Target.CurrentTile));
            ProcessTiles(inGameTiles, owner, _powerRate1).Forget();
        }
        else if (_count == 2)
        {
            var inGameTiles =
                InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.Target.CurrentTile, 3).FindAll(l => l.View.ID % 2 == 0);
            ProcessTiles(inGameTiles, owner, _powerRate1).Forget();
        }
        else if (_count == 3)
        {
            var inGameTiles =
                InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.Target.CurrentTile, 3).FindAll(l => l.View.ID % 2 == 1);
            ProcessTiles(inGameTiles, owner, _powerRate1).Forget();
        }

        _count += 1;

        IsSkillActivated = false;


    }

    private void OnSkillEnd()
    {
        IsSkillActivated = false;
        owner.AddNextState<CharacterStateIdle>();
        CoolTimeElapsedTime = CoolTimeDurationTime;
        base.OnSkillAnimationEnd();
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private async UniTask ProcessTiles(List<InGameTile> tiles, CharacterController owner, float powerRate)
    {
        foreach (var tile in tiles)
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);

        foreach (var tile in tiles.FindAll(l => l.View.AllianceType == AllianceType.Player))
        {
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).ShakeCamera(0.18f, 0.1f);
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);
            InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], tile.View.CachedTr.position);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_skill_hit_01,
                    tile.OccupiedCharacter.SkillRootTransformFollowable);

                var damage = owner.CalculateDamageAmount(owner.AD * powerRate, 0, tile.OccupiedCharacter, codeId, true);
                // var damage = owner.PrecalculateDamageAmount(owner.AD * powerRate, 0, tile.OccupiedCharacter, codeId, true);
                // owner.PostCalculateDamageAmount(ref damage, tile.OccupiedCharacter);
                tile.OccupiedCharacter.GetDamaged(damage, owner);

                StunCharacter(tile.OccupiedCharacter);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.2));
        }
    }

    private void StunCharacter(CharacterController character)
    {
        Span<double> eccStats = stackalloc double[1];
        eccStats.Clear();
        eccStats[0] = _stunTime;

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CC_STUN, character, eccStats, source);
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }
}
