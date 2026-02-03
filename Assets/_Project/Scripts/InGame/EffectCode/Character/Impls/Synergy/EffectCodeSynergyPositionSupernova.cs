using System;
using System.Collections.Generic;
using System.Threading;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

/// <summary>
/// 슈퍼노바 클래스 시너지 타입 아이템만 만들고 
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionSupernova : EffectCodeSynergyBase, IEffectCodeInGameObjectDragDropItemInfo
{
    public const int CodeId = 200301;
    private int _synergyGrade;
    private CharacterController _targetCharacter = null;
    private IEffectCodeSource _source;

    private const string NOT_SUPERNOVA_TYPE_TOKEN = "NOT_SUPERNOVA_TYPE";
    private const string NOT_SUPERNOVA_ITEM_APPLY = "NOT_SUPERNOVA_ITEM_APPLY";
    private const float _dragonballEffectDelayTime = 0.5f;

    // 배틀 아이템 등록 전에 ApplySupernovaItemBySavedCharacterId가 호출된 경우 예약
    private bool _isApplyReserved = false;

    private InGameVfx _supernovaItemVfx;
    private AllianceType _allianceType;
    private InGameVfx _supernovaApplyVfx;


    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);
        _allianceType = (AllianceType)codeInfo.GetCodeStatToInt(4);
        if (_allianceType == AllianceType.Enemy)
        {
            AddSupernovaForEnemy();
        }
        else
        {
            AddGameObjectSuperNovaItem(source);
        }
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);

        _synergyGrade = codeInfo.GetCodeStatToInt(3);
        _allianceType = (AllianceType)codeInfo.GetCodeStatToInt(4);

        if (_supernovaApplyVfx != null && _targetCharacter != null)
        {
            SafeRemoveVfx(ref _supernovaApplyVfx);
            _supernovaApplyVfx = InGameVfxManager.Instance.AddInGameVfx(GetSupernovaVfxName(_synergyGrade), _targetCharacter.SkillRootTransformFollowable);
        }
    }

    public override void OnFlowStateStageReadyStart()
    {
        base.OnFlowStateStageReadyStart();
        ApplySupernovaItemBySavedCharacterId();
    }

    /// <summary>
    /// 저장된 슈퍼노바 캐릭터 ID로 슈퍼노바 아이템을 자동 적용
    /// 배틀 아이템이 아직 등록되지 않았다면 예약하고, 등록 완료 시 자동 적용
    /// </summary>
    public void ApplySupernovaItemBySavedCharacterId()
    {
        var deckData = ServerDataManager.Instance.Deck.GetDeck(InGameType.STAGE);
        if (deckData == null)
            return;

        var additionalData = deckData.GetAdditionalData();
        if (additionalData == null || additionalData.supernovaCharacterId == 0)
            return;

        // 저장된 캐릭터 ID로 캐릭터 찾기
        var characterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
        CharacterController targetCharacter = null;

        foreach (var charCtrl in characterList)
        {
            if (charCtrl != null && charCtrl.CharacterId == additionalData.supernovaCharacterId)
            {
                // 슈퍼노바 타입인지 확인
                if (charCtrl.SpecCharacter.character_stella_type == SynergyType.SUPERNOVA)
                {
                    targetCharacter = charCtrl;
                    break;
                }
            }
        }

        if (targetCharacter == null)
            return;


        // 슈퍼노바 배틀 아이템 찾기 (모든 등급 확인)
        CharacterController battleItem = null;

        int battleItemId = (int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA;
        var battleItemList = InGameSynergyManager.Instance.GetBattleItemList(battleItemId);
        if (battleItemList != null && battleItemList.Count > 0)
        {
            // ITEM_DRAG_DROP 상태인 아이템 찾기
            var itemInfoList = InGameSynergyManager.Instance.GetBattleItemInfoList(battleItemId);
            if (itemInfoList != null)
            {
                foreach (var itemInfo in itemInfoList)
                {
                    if (itemInfo.itemState == InGameBattleItemDragDropComponent.ItemState.ITEM_DRAG_DROP)
                    {
                        battleItem = itemInfo.targetObj;
                        break;
                    }
                }
            }
        }

        // 배틀 아이템이 없으면 예약만 하고 나중에 처리
        if (battleItem == null)
        {
            _isApplyReserved = true;
            return;
        }

        battleItem.CurrentTile.SetUnoccupied();
        // 배틀 아이템이 있으면 즉시 적용
        InGameSynergyManager.Instance.ApplyBattleItem(battleItem, targetCharacter);
        _isApplyReserved = false;
    }

    /// <summary>
    /// 예약된 아이템 적용 작업 실행
    /// </summary>
    private void ExecuteReservedApply()
    {
        if (!_isApplyReserved || _allianceType != AllianceType.Player)
            return;

        _isApplyReserved = false;
        ApplySupernovaItemBySavedCharacterId();
    }

    private async void AddGameObjectSuperNovaItem(IEffectCodeSource source)
    {
        if (InGameSynergyManager.Instance.IsRegisteredBattleItem((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA))
            return;

        var battleItemId = (int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA + _synergyGrade - 1;
        var specCharacter = SpecDataManager.Instance.GetSpecCharacter(battleItemId);
        InGameTile inGameTile = null;

        if (InGameTouchManager.Instance.SelectedFirstTileID != -1)
        {
            inGameTile = InGameObjectManager.Instance.InGameGrid.GetTile(InGameTouchManager.Instance.SelectedFirstTileID);
            // 선택된 타일이 이미 점유되어 있으면 다른 빈 타일 찾기
            if (inGameTile != null && inGameTile.OccupiedCharacter != null)
            {
                inGameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(_allianceType);
            }
        }
        else
        {
            inGameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(_allianceType);
        }

        if (TutorialManager.Instance.IsTutorial)
        {
            var tutorialTile = InGameObjectManager.Instance.InGameGrid.GetTile(11);
            // 튜토리얼 타일이 이미 점유되어 있으면 다른 빈 타일 찾기
            if (tutorialTile != null && tutorialTile.OccupiedCharacter != null)
            {
                inGameTile = InGameObjectManager.Instance.InGameGrid.GetRandomEmptyTile(_allianceType);
            }
            else
            {
                inGameTile = tutorialTile;
            }
        }


        int2 pos = new int2(inGameTile.X, inGameTile.Y);

        var statData = new CharacterStatData(battleItemId, 1, 1, 1);
        var character = await InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.BattleItem,
            typeof(CharacterStateReady), false, HpBarType.None);

        character.GetCharacterView().SpriteRendererSetActive(false);
        _supernovaItemVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_sn_item_01, character.SkillMiddleFXTransformFollowable);
        var ecc = character.GetEffectCodeContainer();
        ecc.AddOrMergeEffectCode(new EffectCodeInfo((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA_DRAGGING_EFFECT, 0, Span<double>.Empty), source);

        var itemInfo = InGameBattleItemDragDropComponent.InGameBattleItemInfo.Create(
            character: character,
            source: source,
            itemInfoHandler: this);

        if (TutorialManager.Instance.IsTutorial)
        {
            TutorialTarget tutorialTarget = inGameTile.View.transform.GetComponent<TutorialTarget>();
            if (tutorialTarget == null)
            {
                tutorialTarget = inGameTile.View.gameObject.AddComponent<TutorialTarget>();
            }
            tutorialTarget.SetTargetId("SupernovaItemTile_11");
        }
        InGameSynergyManager.Instance.RegisterBattleItem(itemInfo);

        // 배틀 아이템 등록 완료 후 예약된 적용 작업 실행
        ExecuteReservedApply();
    }

    public override void OnCombatStart()
    {
        if (_targetCharacter == null)
            return;

        var supernovaSynergyList = SpecDataManager.Instance.GetSpecSynergyList(SynergyType.SUPERNOVA);
        if (supernovaSynergyList == null || supernovaSynergyList.Count == 0)
            return;

        base.OnCombatStart();
        for (int i = 1; i <= _synergyGrade; i++)
        {
            switch (i)
            {
                case 1:
                    AddHpPercentUp(_targetCharacter, _source, supernovaSynergyList);
                    break;
                case 2:
                    AddAdPercentUp(_targetCharacter, _source, supernovaSynergyList);
                    break;
                case 3:
                    AddAttackSpeedCriticalRateAtkPierce(_targetCharacter, _source, supernovaSynergyList);
                    break;
            }
        }
    }

    private void AddSupernovaForEnemy()
    {
        var enemyList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Enemy);
        foreach (var enemy in enemyList)
        {
            if (enemy.SpecCharacter.character_stella_type != SynergyType.SUPERNOVA)
            {
                continue;
            }
            _targetCharacter = enemy;
            OnItemApplyDragAndDrop(_targetCharacter, _source);
        }
        return;

    }


    private void AddHpPercentUp(CharacterController targetCharacter, IEffectCodeSource source, List<ISpecSynergyData> supernovaSynergyList)
    {
        float increaseValue = supernovaSynergyList[0].effect_stat_value_1 * 0.01f;
        double accumulatedValue = 0d;


        foreach (var character in GetOtherSupernovaCharacters(targetCharacter))
            {
                var value = character.HP * -increaseValue;
                accumulatedValue += value;
                ApplyEffectCode(EffectCodeNameType.HP_UP, character, value, source);
                character.UpdateHpBar();
            }

        ApplyEffectCode(EffectCodeNameType.HP_UP, targetCharacter, -accumulatedValue, source);
        targetCharacter.SetMaxHealth();
    }

    private void AddAdPercentUp(CharacterController targetCharacter, IEffectCodeSource source, List<ISpecSynergyData> supernovaSynergyList)
    {
        float increaseValue = supernovaSynergyList[1].effect_stat_value_1 * 0.01f;
        double accumulatedValue = 0d;

        foreach (var character in GetOtherSupernovaCharacters(targetCharacter))
        {
            var effectCodeType = character.SpecCharacter.atk_type == AtkType.AD 
                ? EffectCodeNameType.AD_UP 
                : EffectCodeNameType.AP_UP;
            var statValue = character.SpecCharacter.atk_type == AtkType.AD ? character.AD : character.AP;
            
            var value = statValue * -increaseValue;
            accumulatedValue += value;
            ApplyEffectCode(effectCodeType, character, value, source);
        }

        var targetEffectCodeType = targetCharacter.SpecCharacter.atk_type == AtkType.AD 
            ? EffectCodeNameType.AD_UP 
            : EffectCodeNameType.AP_UP;
        ApplyEffectCode(targetEffectCodeType, targetCharacter, -accumulatedValue, source);
    }

    private void AddAttackSpeedCriticalRateAtkPierce(CharacterController targetCharacter, IEffectCodeSource source, List<ISpecSynergyData> supernovaSynergyList)
    {
        var targetData = supernovaSynergyList[2];

        float atkSpeedRate = targetData.effect_stat_value_1 * 0.01f;
        float criticalRateRate = targetData.effect_stat_value_2 * 0.01f;
        float atkPierceRate = targetData.effect_stat_value_3 * 0.01f;

        double accAtkSpeed = 0d;
        double accCriticalRate = 0d;
        double accAtkPierce = 0d;

        foreach (var character in GetOtherSupernovaCharacters(targetCharacter))
        {
            var atkSpeedValue = character.AttackSpeed * -atkSpeedRate;
            accAtkSpeed += atkSpeedValue;
            ApplyEffectCode(EffectCodeNameType.ATK_SPEED_UP, character, atkSpeedValue, source);

            var critValue = character.CriticalProb * -criticalRateRate;
            accCriticalRate += critValue;
            ApplyEffectCode(EffectCodeNameType.CRIT_RATE_UP, character, critValue, source);

            var pierceValue = character.ADPierce * -atkPierceRate;
            accAtkPierce += pierceValue;
            ApplyEffectCode(EffectCodeNameType.AD_PIERCE_UP, character, pierceValue, source);
        }

        ApplyEffectCode(EffectCodeNameType.ATK_SPEED_UP, targetCharacter, -accAtkSpeed, source);
        ApplyEffectCode(EffectCodeNameType.CRIT_RATE_UP, targetCharacter, -accCriticalRate, source);
        ApplyEffectCode(EffectCodeNameType.AD_PIERCE_UP, targetCharacter, -accAtkPierce, source);
    }

    /// <summary>
    /// 타겟을 제외한 같은 팀의 슈퍼노바 캐릭터 목록 반환
    /// </summary>
    private IEnumerable<CharacterController> GetOtherSupernovaCharacters(CharacterController targetCharacter)
    {
        var characterList = InGameObjectManager.Instance.GetCharacterList(targetCharacter.AllianceType);
        foreach (var character in characterList)
        {
            if (character != targetCharacter && 
                character.SpecCharacter.character_stella_type == SynergyType.SUPERNOVA)
            {
                yield return character;
            }
        }
    }

    /// <summary>
    /// EffectCode를 적용하고 시너지 ID를 등록
    /// </summary>
    private void ApplyEffectCode(EffectCodeNameType effectCodeType, CharacterController character, double value, IEffectCodeSource source)
    {
        Span<double> stats = stackalloc double[1];
        stats[0] = value;
        EffectCodeHelper.AddOrMergeEffectCode(effectCodeType, character, stats, source);
        base.AddSynergyAddEffectCodeIds(effectCodeType);
    }

    public override void OnPreRemoved()
    {
        InGameSynergyManager.Instance.TryRemoveBattleItemFromTarget((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA);

        Debug.LogColor($"Supernova Removed", "red");
        base.OnPreRemoved();
        
        SafeRemoveVfx(ref _supernovaApplyVfx);
        SafeRemoveVfx(ref _supernovaItemVfx);
        
        _targetCharacter = null;
    }
    
    /// <summary>
    /// VFX를 안전하게 제거하고 참조를 null로 설정
    /// </summary>
    private void SafeRemoveVfx(ref InGameVfx vfx)
    {
        if (vfx == null) return;
        
        if (vfx.CachedGo != null)
        {
            InGameVfxManager.Instance.RemoveInGameVfx(vfx);
        }
        vfx = null;
    }

    public void OnItemApplyDragAndDrop(CharacterController targetCharacter, IEffectCodeSource source)
    {
        _targetCharacter = targetCharacter;
        _source = source;

        SafeRemoveVfx(ref _supernovaItemVfx);
        
        InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_asterism_sn_get_01, _targetCharacter.SkillRootTransformFollowable.GetPosition());
        GetSupernovaDragonBall().Forget();
    }

    private async UniTask GetSupernovaDragonBall()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(_dragonballEffectDelayTime));

        if (_targetCharacter == null)
            return;

        var vfxName = GetSupernovaVfxName(_synergyGrade);
        SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_synergy_nova_spirit);
        _supernovaApplyVfx = InGameVfxManager.Instance.AddInGameVfx(vfxName, _targetCharacter.SkillRootTransformFollowable);
    }

    public bool OnItemCanApplyDragAndDrop(CharacterController targetCharacter)
    {
        if (targetCharacter == null)
            return false;
        if (targetCharacter.SpecCharacter.character_stella_type != SynergyType.SUPERNOVA)
        {
            ToastManager.Instance.ShowToastByTokenKey(NOT_SUPERNOVA_TYPE_TOKEN);
            return false;
        }
        return true;
    }
    public bool OnItemCheckCharacterAffected(CharacterController targetCharacter)
    {
        if (targetCharacter == null || _targetCharacter == null)
        {
            return false;
        }
        if (targetCharacter != _targetCharacter)
        {
            return false;
        }
        return true;
    }

    public void OnItemTargetObjectRelease(CharacterController targetCharacter, InGameBattleItemDragDropComponent.ItemState itemState)
    {
        InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetCharacter.SpecCharacter.character_stella_type);
        InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetCharacter.SpecCharacter.character_element_type);
    }

    public void OnItemNotAppliedBeforeCombat(CharacterController targetItemController, IEffectCodeSource source)
    {
        if (_allianceType == AllianceType.Player)
        {
            ToastManager.Instance.ShowToastByTokenKey(NOT_SUPERNOVA_ITEM_APPLY);
        }

        var characterList = InGameObjectManager.Instance.GetCharacterList(_allianceType);
        foreach (var character in characterList)
        {
            if (character == targetItemController ||
            (character.SpecCharacter.character_stella_type != SynergyType.SUPERNOVA))
            {
                continue;
            }
            InGameSynergyManager.Instance.ApplyBattleItem(targetItemController, character);
            // OnCombatStart();
            break;
        }
    }

    private InGameVfxNameType GetSupernovaVfxName(int synergyGrade)
    {
        return synergyGrade == 1 ? InGameVfxNameType.fx_common_asterism_sn_aura_01 :
        synergyGrade == 2 ? InGameVfxNameType.fx_common_asterism_sn_aura_02 :
        InGameVfxNameType.fx_common_asterism_sn_aura_03;
    }
}
