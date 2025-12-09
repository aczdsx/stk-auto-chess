using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// 슈퍼노바 클래스 시너지 타입 아이템만 만들고 
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionSupernova : EffectCodeCharacterBase, IEffectCodeInGameObjectItemInfo
{
    private enum SupernovaGrade
    {
        NONE = 0,
        ADD_HP_PERCENT = 1,
        ADD_AD_PERCENT = 2,
        ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE = 3,
    }
    public const int CodeId = 210701;
    public readonly static int SUPERNOVA_ITEM_VIEW_ID = 300001;
    // private float _statValue_1;
    // private float _statValue_2;
    // private float _statValue_3;
    private int _synergyGrade;
    private CharacterController _targetCharacter = null;
    private IEffectCodeSource _source;
    private InGameVfx _supernovaVfx;
    private const string NOT_SUPERNOVA_TYPE_TOKEN = "NOT_SUPERNOVA_TYPE";

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        // _statValue_1 = codeInfo.GetCodeStatToFloat(0);
        // _statValue_2 = codeInfo.GetCodeStatToFloat(1);
        // _statValue_3 = codeInfo.GetCodeStatToFloat(2);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);
        AddGameObjectSuperNovaItem(source);
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        // _statValue_1 = codeInfo.GetCodeStatToFloat(0);
        // _statValue_2 = codeInfo.GetCodeStatToFloat(1);
        // _statValue_3 = codeInfo.GetCodeStatToFloat(2);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);
    }

    private async void AddGameObjectSuperNovaItem(IEffectCodeSource source)
    {
        if (InGameObjectManager.Instance.IsRegisteredItem(SUPERNOVA_ITEM_VIEW_ID))
            return;

        var specCharacter = SpecDataManager.Instance.GetCharacterData(SUPERNOVA_ITEM_VIEW_ID);
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
        
        var statData = new CharacterStatData(SUPERNOVA_ITEM_VIEW_ID, 1, 1, 1);
        var character = await InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.Neutral,
            typeof(CharacterStateReady), false, HpBarType.None);

        var itemInfo = InGameObjectManagerItemComponent.InGameObjectItemInfo.Create(
            character: character,
            OnItemApplyDragAndDrop: OnItemApplyDragAndDrop,
            source: source,
            OnItemCanApplyDragAndDrop: OnItemCanApplyDragAndDrop,
            OnItemCheckCharacterAffected: OnItemCheckCharacterAffected,
            OnItemTargetObjectRelease: OnItemTargetObjectRelease
            );
        InGameObjectManager.Instance.RegisterItem(itemInfo);
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
    private void AddHpPercentUp(CharacterController targetCharacter, IEffectCodeSource source, List<SpecSynergy> supernovaSynergyList)
    {
        Span<double> stats = stackalloc double[1];
        stats.Clear();
        stats[0] = supernovaSynergyList[(int)SupernovaGrade.ADD_HP_PERCENT].stat_value;
        Debug.LogColor($"Supernova HP % Up: {supernovaSynergyList[1].stat_value}", "green");
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.HP_PERCENT_UP, targetCharacter, stats, source);
    }

    private void AddAdPercentUp(CharacterController targetCharacter, IEffectCodeSource source, List<SpecSynergy> supernovaSynergyList)
    {
        Span<double> stats = stackalloc double[1];
        stats.Clear();
        stats[0] = supernovaSynergyList[(int)SupernovaGrade.ADD_AD_PERCENT].stat_value;
        Debug.LogColor($"Supernova AD % Up: {supernovaSynergyList[(int)SupernovaGrade.ADD_AD_PERCENT].stat_value}", "green");
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.AD_PERCENT_UP, targetCharacter, stats, source);
    }
    private void AddAttackSpeedCriticalRateAtkPierce(CharacterController targetCharacter, IEffectCodeSource source, List<SpecSynergy> supernovaSynergyList)
    {
        Span<double> stats = stackalloc double[1];

        stats.Clear();
        stats[0] = supernovaSynergyList[(int)SupernovaGrade.ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE].stat_value;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.ATK_SPEED_PERCENT_UP, targetCharacter, stats, source);

        stats.Clear();
        stats[0] = supernovaSynergyList[(int)SupernovaGrade.ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE].stat_value_2;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.CRIT_RATE_PERCENT_UP, targetCharacter, stats, source);

        stats.Clear();
        stats[0] = supernovaSynergyList[(int)SupernovaGrade.ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE].stat_value_3;
        EffectCodeHelper.AddOrMergeEffectCode(EffectCodeNameType.DEF_PENETRATION_PERCENT_UP, targetCharacter, stats, source);
        Debug.LogColor($"Supernova ATK Speed % Up: {supernovaSynergyList[(int)SupernovaGrade.ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE].stat_value}", "green");
        Debug.LogColor($"Supernova Crit Rate % Up: {supernovaSynergyList[(int)SupernovaGrade.ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE].stat_value_2}", "green");
        Debug.LogColor($"Supernova DEF Penetration % Up: {supernovaSynergyList[(int)SupernovaGrade.ADD_ATTACK_SPEED_CRITICAL_RATE_ATK_PIERCE].stat_value_3}", "green");

    }

    public override void OnPreRemoved()
    {
        InGameObjectManager.Instance.TryRemoveItemFromTarget(SUPERNOVA_ITEM_VIEW_ID);

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

        _supernovaVfx = InGameVfxManager.Instance.AddInGameVfx(InGameVfxNameType.fx_common_cast_supernova_02, _targetCharacter.SkillRootTransformFollowable);
    }

    public bool OnItemCanApplyDragAndDrop(CharacterController targetCharacter)
    {
        if (targetCharacter == null)
            return false;
        if (targetCharacter.SpecCharacter.asterism_type != SynergyType.SUPERNOVA)
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

    public void OnItemTargetObjectRelease(CharacterController targetCharacter, InGameObjectManagerItemComponent.ItemState itemState)
    {
        InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetCharacter.SpecCharacter.element_type);
        InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetCharacter.SpecCharacter.asterism_type);
    }
}
