using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

/// <summary>
/// 공허의 토마
/// 효과: 대상: 3x3 공격력이 가장 강한 적군
/// 효과: 범위내에 {1}% 피해를 입히고, {2}초간, 방어력과 공격속도가 {3}% 감소한다.
/// </summary>
[UseEffectCodeIds(280109001)]
public partial class EffectCodeSkill280109001 : EffectCodeCharacterBase
{
    private bool _isReadyToActivate;
    private SkillActive _specSkill;
    private float _damageRate;
    private float _debuffTime;
    private float _debuffRate;
    private List<InGameTile> _emptyTiles;
    private const float _rotationOffset = -90f;

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        SkillIndex = 1;
        CoolTimeElapsedTime = 0f;
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1);
        _debuffTime = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _debuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;

        _isReadyToActivate = false;
        IsSkillActivated = false;

        Span<double> buffStats = stackalloc double[3];

        buffStats.Clear();
        buffStats[0] = codeId;
        buffStats[1] = 999f;//duration
        buffStats[2] = 1;//value?

        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.BUFF_IMMUNE, owner, buffStats, source);

        _emptyTiles = new List<InGameTile>();
        _specSkill = SpecDataManager.Instance.GetSkillDataList(codeId).First();
        owner.SetStateType(typeof(CharacterStateAttack), typeof(CharacterStateAttackAnimEventDamage));
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        CoolTimeDurationTime = codeInfo.GetCodeStatToFloat(0);
        _damageRate = codeInfo.GetCodeStatToFloat(1);
        _debuffTime = codeInfo.GetCodeStatToFloat(2) * 0.01f;
        _debuffRate = codeInfo.GetCodeStatToFloat(3) * 0.01f;
    }

    public override void OnUpdate(float dt)
    {
        // InGameVfxManager.Instance.AddInGameTileFx(SynergyType.EARTH, owner.CurrentTile);
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
        InGameVfxManager.Instance.AddInGamePreSkillActionFx(SynergyType.EARTH,
            owner.GetCharacterView().CachedTr.position);
    }

    public override void OnSkillExecute(int executeIndex, int totalLength)
    {
        base.OnSkillExecute(executeIndex, totalLength);

        var targetCharacterList = InGameObjectManager.Instance.GetCharacterListSortedByADDescending(owner.AllianceType, isOwnCharacter: false);
        if (targetCharacterList.Count == 0)
            return;

        InGameTile selectedTile = null;
        CharacterController selectedTarget = null;

        // 모든 타겟을 순회하면서 빈 타일이 있는지 확인
        foreach (var character in targetCharacterList)
        {
            if (!character.IsAlive || character.CurrentTile == null)
                continue;

            var targetTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(character.CurrentTile, 1);

            // 리스트 초기화
            _emptyTiles.Clear();

            // OccupiedCharacter가 있는 타일 제거
            foreach (var tile in targetTiles)
            {
                if (tile != null && tile.OccupiedCharacter == null)
                {
                    _emptyTiles.Add(tile);
                }
            }

            // 빈 타일이 있으면 이 타겟 사용
            if (_emptyTiles.Count > 0)
            {
                selectedTarget = character;
                // 랜덤으로 타일 선택
                int randomIndex = InGameRandomManager.GetUniversalRandomValue(0, _emptyTiles.Count - 1);
                selectedTile = _emptyTiles[randomIndex];
                break;
            }
        }

        // 모든 타겟을 확인했는데도 빈 타일이 없으면 리턴
        if (selectedTile == null || selectedTarget == null)
        {
            IsSkillActivated = false;
            return;
        }

        owner.Target = selectedTarget;

        // 선택한 타일로 이동
        MoveToTile(selectedTile);

        // 이동한 위치에서 square 1 범위의 타일들 가져오기
        var attackTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByShapeSquare(selectedTile, 1);

        // 타일 이펙트 표시
        foreach (var tile in attackTiles)
        {
            if (tile != null)
            {
                InGameVfxManager.Instance.AddInGameTileFx(SynergyType.EARTH, tile);
            }
        }
        InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[2], owner.SkillMiddleFXTransformFollowable.GetPosition());

        // 데미지 및 디버프 적용
        Span<double> eccStats = stackalloc double[3];
        eccStats.Clear();
        eccStats[0] = codeId;
        eccStats[1] = _debuffTime;
        eccStats[2] = _debuffRate;

        foreach (var tile in attackTiles)
        {
            if (tile == null)
                continue;

            if (tile.CheckValidTile(owner.AllianceType, false) && tile.OccupiedCharacter != null)
            {
                var target = tile.OccupiedCharacter;
                if (!target.IsAlive)
                    continue;

                // 데미지 적용
                var damageValue = owner.SpecCharacter.atk_type is AtkType.AD ? owner.AD : owner.AP;
                var damage = owner.CalculateDamageAmount(damageValue * _damageRate, 0, target, codeId, true);
                target.GetDamaged(damage, owner);

                // 방어력 감소 디버프 적용
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_DEF_PERCENT_DOWN, target, eccStats, source);

                // 공격속도 감소 디버프 적용
                EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEBUFF_ATK_SPEED_DOWN, target, eccStats, source);
            }
        }

        IsSkillActivated = false;
    }

    private void MoveToTile(InGameTile targetTile)
    {
        owner.ChangeOccupiedTile(targetTile);
        owner.Position3D = targetTile.View.Position;

        var characterView = owner.GetCharacterView();
        if (characterView?.CachedTr != null)
        {
            characterView.CachedTr.localPosition = targetTile.View.Position;
            characterView.LookAt(owner.CurrentTile, owner.Target.CurrentTile);
        }
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_mon_skill_toma_02);

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

    public override void OnPreRemoved()
    {
        owner.RemoveStateType(typeof(CharacterStateAttack));
        base.OnPreRemoved();
    }

#region toma normal attack
    public override void OnStateNormalAttackDamageEvent(CharacterController.DamageInfo defaultDamageInfo, int executeIndex, int totalLength)
    {
        base.OnStateNormalAttackDamageEvent(defaultDamageInfo, executeIndex, totalLength);

        if (executeIndex == 0)
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_mon_skill_toma_01);
            ExecuteSkillOne(defaultDamageInfo);
        }
        else if (executeIndex == 1)
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_mon_skill_toma_01);
            ExecuteSkillTwo(defaultDamageInfo);
        }


    }

    // 3x1 범위 공격 
    private void ExecuteSkillOne(CharacterController.DamageInfo defaultDamageInfo)
    {
        var frontTiles = InGameObjectManager.Instance.InGameGrid.GetTileListByCharacterDirection(owner, 1, 1);

        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[0], owner.SkillMiddleFXTransformFollowable);

        if (frontTiles.Count > 0)
        {
            Vector3 direction = (frontTiles[0].View.CachedTr.position - owner.CurrentTile.View.CachedTr.position).normalized;
            vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, _rotationOffset, 0);
        }
        foreach (var tile in frontTiles)
        {
            InGameVfxManager.Instance.AddInGameTileFx(SynergyType.EARTH, tile);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                tile.OccupiedCharacter.GetDamaged(defaultDamageInfo, owner);
            }
        }

    }

    // 1x3 범위 공격 (찌르기)
    private void ExecuteSkillTwo(CharacterController.DamageInfo defaultDamageInfo)
    {
        var frontTiles = InGameObjectManager.Instance.InGameGrid.GetTileByCharacterDirection(owner, 3);
        var vfx = InGameVfxManager.Instance.AddInGameVfx(_specSkill.skill_vfxs[1], owner.SkillMiddleFXTransformFollowable);
        if (frontTiles.Count > 0)
        {
            Vector3 direction = (frontTiles[0].View.CachedTr.position - owner.CurrentTile.View.CachedTr.position).normalized;
            vfx.CachedTr.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, _rotationOffset, 0);
        }
        foreach (var tile in frontTiles)//GetTileByCharacterDirection
        {
            InGameVfxManager.Instance.AddInGameTileFx(SynergyType.EARTH, tile);
            if (tile.CheckValidTile(owner.AllianceType, false))
            {
                tile.OccupiedCharacter.GetDamaged(defaultDamageInfo, owner);
            }
        }
    }
#endregion
}
