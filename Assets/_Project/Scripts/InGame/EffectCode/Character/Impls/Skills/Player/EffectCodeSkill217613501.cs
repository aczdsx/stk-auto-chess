using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 오데트
// 범위 : 
// 1. 본인기준  2*3 
// 2. 본인기준 3*3 범위
// 재사용 시간: {0}초
// 낫을 크게 휘둘러, 적에게 공격력 {1}%의 대미지를 주고, 상대의 뒤편으로 이동하여 넓은 범위의 공격을 진행합니다.
// 특수 효과 : 피격된 적은 {2}동안 공격 속도가 {3}% 감소하고 한기를 중첩 시킵니다.
/// </summary>
[UseEffectCodeIds(217613501)]
public partial class EffectCodeSkill217613501 : EffectCodeCharacterBase
{
    private ObfuscatorFloat _powerRate;
    private ObfuscatorFloat _debuffTime;
    private ObfuscatorFloat _atkSpeedDownRate;

    private bool _isReadyToActivate;

    private SkillActive _specSkill;
    private static readonly Vector3 _vfxFlipScale = new Vector3(1, 1, -1);

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _atkSpeedDownRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
        _isReadyToActivate = false;
        IsSkillActivated = false;

        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _powerRate = codeInfo.GetCodeStatToFloat(1) * 0.01f;
        _debuffTime = codeInfo.GetCodeStatToFloat(2);
        _atkSpeedDownRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
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
        // base.Activate();

        var isInRange = InGameObjectManager.Instance.IsInRange(owner, owner.Target);
        if (!isInRange)
        {
            if (owner.Target != null)
            {
                InGameTile bestTile = InGameObjectManager.Instance.GetNextMovableTile(owner.CurrentTile,
                    owner.Target.CurrentTile);
                owner.MoveTile(bestTile);
            }
            return;
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

        if (executeIndex == 0)
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_a_3501_01);
            ExecuteFirstStep().Forget();
        }
        else if (executeIndex == 1)
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_skill_a_3501_02);
            ExecuteSecondStep();
        }

        if (executeIndex == 1)
        {
            IsSkillActivated = false;
        }
    }

    public override float AddSkillCooltime(float cooltime)
    {
        CoolTimeElapsedTime += cooltime;
        return cooltime;
    }

    public override void OnSkillAnimationEnd()
    {
        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
        base.OnSkillAnimationEnd();
    }

    private async UniTaskVoid ExecuteFirstStep()
    {
        List<InGameTile> targetTiles = null;

        // 본인기준 두칸 
        targetTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 0, 1);
        // 앞 세칸
        var frontTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 1, 1);
        targetTiles.AddRange(frontTiles);

        foreach (var tile in targetTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);

            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                var target = tile.OccupiedCharacter;
                var damageValue = target.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AD;
                damageValue *= _powerRate;
                var damage = owner.CalculateDamageAmount(damageValue, 0, target, codeId, true);
                target.GetDamaged(damage, owner);

                double[] eccStats = new double[3];
                eccStats[0] = codeId;
                eccStats[1] = _debuffTime;
                eccStats[2] = _atkSpeedDownRate;
                
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN, target, eccStats, source);
            }
        }

        var frontTile = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner);
        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillMiddleFXTransformFollowable.GetPosition());

        var targetTileIdx = frontTile[0].Int2Index;
        var odetteTileIdx = owner.CurrentTile.Int2Index;
        var directionidx = targetTileIdx - odetteTileIdx;
        if (directionidx.x > 0 || directionidx.y < 0)
        {
            vfx.CachedTr.localScale = _vfxFlipScale;
        }
        else
        {
            vfx.CachedTr.localScale = Vector3.one;
        }

        Vector3 direction = (frontTile[0].View.CachedTr.position - owner.CurrentTile.View.CachedTr.position).normalized;
        vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90f, 0);

        // 0.2초 후 캐릭터가 보고 있는 방향 앞 두칸으로 이동
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

        if (owner == null || owner.CurrentTile == null)
            return;

        // 캐릭터가 보고 있는 방향 앞 두칸 타일 가져오기
        var forwardTiles = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 2);
        
        // 두 칸 앞 타일이 있고 비어있으면 이동
        if (forwardTiles != null && forwardTiles.Count > 0)
        {
            var targetTile = forwardTiles[forwardTiles.Count - 1]; // 마지막 타일 (두 칸 앞)
            if (targetTile != null && targetTile.OccupiedCharacter == null)
            {
                MoveToTile(targetTile);
            }
        }
    }

    private void MoveToTile(InGameTile targetTile)
    {
        owner.ChangeOccupiedTile(targetTile);
        owner.Position3D = targetTile.View.Position;

        var characterView = owner.GetCharacterView();
        if (characterView?.CachedTr != null)
        {
            characterView.CachedTr.localPosition = targetTile.View.Position;
        }

    }

    private void ExecuteSecondStep()
    {
        // 플레이어 기준 Square 3x3 타일 가져오기
        var areaTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(owner.CurrentTile, 1);
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[2], owner.CurrentTile.View.CachedTr.position);

        foreach (var tile in areaTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(owner.SpecCharacter.character_element_type, tile);

            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                var target = tile.OccupiedCharacter;
                if (target == null || !target.IsAlive)
                    continue;

                // 데미지 적용
                var damageValue = target.SpecCharacter.atk_type == AtkType.AD ? owner.AD : owner.AD;
                damageValue *= _powerRate;
                var damage = owner.CalculateDamageAmount(damageValue, 0, target, codeId, true);
                target.GetDamaged(damage, owner);

                // 디버프 적용
                double[] eccStats = new double[3];
                eccStats[0] = codeId;
                eccStats[1] = _debuffTime;
                eccStats[2] = _atkSpeedDownRate;

                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN, target, eccStats, source);
            }
        }

        CoolTimeElapsedTime = 0;
        IsSkillActivated = false;
    }



}
