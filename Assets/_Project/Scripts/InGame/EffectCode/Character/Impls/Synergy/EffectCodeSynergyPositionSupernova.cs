using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;

/// <summary>
/// 슈퍼노바 클래스 시너지 타입 아이템만 만들고 
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionSupernova : EffectCodeSynergyBase, IEffectCodeInGameObjectDragDropItemInfo
{
    private enum SupernovaGrade
    {
        NONE = 0,
        ADD_HP_PERCENT = 1,
        ADD_AD_PERCENT = 2,
        ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE = 3,
    }
    public const int CodeId = 200301;
    private int _synergyGrade;
    private CharacterController _targetCharacter = null;
    private IEffectCodeSource _source;
    private InGameVfx _supernovaVfx;
    private const string NOT_SUPERNOVA_TYPE_TOKEN = "NOT_SUPERNOVA_TYPE";
    private const string NOT_SUPERNOVA_ITEM_APPLY = "NOT_SUPERNOVA_ITEM_APPLY";

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);
        AddGameObjectSuperNovaItem(source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);

        if (_supernovaVfx != null && _targetCharacter != null)
        {
            _supernovaVfx.Remove();
            _supernovaVfx = InGameVfxManager.Instance.AddInGameVfx(GetSupernovaVfxName(_synergyGrade), _targetCharacter.SkillRootTransformFollowable);
        }
    }

    private async void AddGameObjectSuperNovaItem(IEffectCodeSource source)
    {
        if (InGameSynergyManager.Instance.IsRegisteredBattleItem((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA))
            return;

        var specCharacter = SpecDataManager.Instance.GetBattleItemData((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA);
        InGameTile inGameTile = null;

        if (InGameTouchManager.Instance.SelectedFirstTileID != -1)
        {
            inGameTile = InGameObjectManager.Instance.InGameGrid.GetTile(InGameTouchManager.Instance.SelectedFirstTileID);
        }
        else
        {
            inGameTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(specCharacter);
        }
        int2 pos = new int2(inGameTile.X, inGameTile.Y);

        var statData = new CharacterStatData((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA, 1, 1, 1);
        var character = await InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Neutral,
            typeof(CharacterStateReady), false, HpBarType.None);

        var itemInfo = InGameBattleItemDragDropComponent.InGameBattleItemInfo.Create(
            character: character,
            source: source,
            itemInfoHandler: this);
        InGameSynergyManager.Instance.RegisterBattleItem(itemInfo);
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
    private void AddHpPercentUp(CharacterController targetCharacter, IEffectCodeSource source, List<ISpecSynergyData> supernovaSynergyList)
    {

        float increaseValue = supernovaSynergyList[0].effect_stat_value_1 * 0.01f;

        TakeToSupernovas(increaseValue, targetCharacter, source, EffectCodeNameType.HP_PERCENT_UP);

        Span<double> stats = stackalloc double[1];
        stats.Clear();
        stats[0] = increaseValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.HP_PERCENT_UP, targetCharacter, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.HP_PERCENT_UP);

        targetCharacter.ForceSetHp(targetCharacter.HP);

    }

    private void AddAdPercentUp(CharacterController targetCharacter, IEffectCodeSource source, List<ISpecSynergyData> supernovaSynergyList)
    {
        Span<double> stats = stackalloc double[1];
        stats.Clear();
        float increaseValue = supernovaSynergyList[1].effect_stat_value_1 * 0.01f;
        var takeCharactersList = InGameObjectManager.Instance.GetCharacterList(targetCharacter.AllianceType);
        foreach (var character in takeCharactersList)
        {
            if (character == targetCharacter ||
            (character.SpecCharacter.character_stella_type != SynergyType.SUPERNOVA))
            {
                continue;
            }
            stats[0] = increaseValue * -1f;
            EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PERCENT_UP, character, stats, source);
            base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AD_PERCENT_UP);
        }


        stats.Clear();
        stats[0] = supernovaSynergyList[(int)SupernovaGrade.ADD_AD_PERCENT].effect_stat_value_1;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PERCENT_UP, targetCharacter, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AD_PERCENT_UP);
    }
    private void AddAttackSpeedCriticalRateAtkPierce(CharacterController targetCharacter, IEffectCodeSource source, List<ISpecSynergyData> supernovaSynergyList)
    {

        var targetData = supernovaSynergyList[2];

        float atkSpeedValue = targetData.effect_stat_value_1 * 0.01f;
        float criticalRateValue = targetData.effect_stat_value_2 * 0.01f;
        float atkPierceValue = targetData.effect_stat_value_3 * 0.01f;

        TakeToSupernovas(atkSpeedValue, targetCharacter, source, EffectCodeNameType.ATK_SPEED_PERCENT_UP);
        TakeToSupernovas(criticalRateValue, targetCharacter, source, EffectCodeNameType.CRIT_RATE_PERCENT_UP);
        TakeToSupernovas(atkPierceValue, targetCharacter, source, EffectCodeNameType.AD_PIERCE_PERCENT_UP);

        Span<double> stats = stackalloc double[1];

        stats.Clear();
        stats[0] = atkSpeedValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.ATK_SPEED_PERCENT_UP, targetCharacter, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.ATK_SPEED_PERCENT_UP);

        stats.Clear();
        stats[0] = criticalRateValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CRIT_RATE_PERCENT_UP, targetCharacter, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.CRIT_RATE_PERCENT_UP);

        stats.Clear();
        stats[0] = atkPierceValue;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PIERCE_PERCENT_UP, targetCharacter, stats, source);
        base.AddSynergyAddEffectCodeIds(EffectCodeNameType.AD_PIERCE_PERCENT_UP);

    }

    private void TakeToSupernovas(float value, CharacterController targetCharacter,
    IEffectCodeSource source, EffectCodeNameType effectCodeNameType)
    {
        Span<double> stats = stackalloc double[1];
        stats.Clear();
        value = value * -1;

        var takeCharactersList = InGameObjectManager.Instance.GetCharacterList(targetCharacter.AllianceType);
        foreach (var character in takeCharactersList)
        {
            if (character == targetCharacter ||
            (character.SpecCharacter.character_stella_type != SynergyType.SUPERNOVA))
            {
                continue;

            }
            stats[0] = value;
            EffectCodeHelper.AddOrMergeEffectCode(effectCodeNameType, character, stats, source);
            base.AddSynergyAddEffectCodeIds(effectCodeNameType);

        }
    }

    public override void OnPreRemoved()
    {
        InGameSynergyManager.Instance.TryRemoveBattleItemFromTarget((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA);

        Debug.LogColor($"Supernova Removed", "red");
        base.OnPreRemoved();

        if (_supernovaVfx != null)
        {
            if (_supernovaVfx.CachedGo != null)
            {
                InGameVfxManager.Instance.RemoveInGameVfx(_supernovaVfx);
            }
            else
            {
                _supernovaVfx.Remove();
                _supernovaVfx = null;
            }
        }
    }

    public void OnItemApplyDragAndDrop(CharacterController targetCharacter, IEffectCodeSource source)
    {
        _targetCharacter = targetCharacter;
        _source = source;

        // _supernovaVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_cast_supernova_02, _targetCharacter.SkillRootTransformFollowable);
        var vfxName = GetSupernovaVfxName(_synergyGrade);

        _supernovaVfx = InGameVfxManager.Instance.AddInGameVfx(vfxName, _targetCharacter.SkillRootTransformFollowable);
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
        ToastManager.Instance.ShowToastByTokenKey(NOT_SUPERNOVA_ITEM_APPLY);

        var characterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
        foreach (var character in characterList)
        {
            if (character == targetItemController ||
            (character.SpecCharacter.character_stella_type != SynergyType.SUPERNOVA))
            {
                continue;
            }
            InGameSynergyManager.Instance.ApplyBattleItem(targetItemController, character);
            OnCombatStart();
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
