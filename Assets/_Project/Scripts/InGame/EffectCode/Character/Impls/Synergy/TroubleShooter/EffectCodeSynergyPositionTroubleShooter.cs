using System;
using CookApps.AutoBattler;
using CookApps.Obfuscator;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using System.Collections.Generic;
using GooglePlayGames.BasicApi;

/// <summary>
///  트러블슈터
/// </summary>
[UseEffectCodeIds(CodeId)]
public partial class EffectCodeSynergyPositionTroubleShooter : EffectCodeSynergyBase, IEffectCodeInGameObjectDragDropItemInfo
{
    public const int CodeId = 200201;
    private enum TroubleShooterGrade
    {
        NONE = 0,
        INTALL_MINE = 1,
        ADD_SUPPLY = 2,
        SUPPORT_CANNON = 3,
    }

    private enum TroubleShooterItemType
    {
        DYNAMITE = 6102,
        CHOCOBAR = 6103,
        ENERGY_DRINK = 6104,
        BATTLE_VITAMIN = 6105,
        EMP_BOMB = 6106,
        CANNON = 6107,
    }
    private int _synergyGrade;
    private float _elapsedTime;
    private float _supplyDuration;
    private float _cannonDuration;
    private List<CharacterController> _dynamiteList = new();

    public override void Initialize(EffectCodeInfo codeInfo, EffectCodeContainer container, IEffectCodeSource source)
    {
        base.Initialize(codeInfo, container, source);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);
        _dynamiteList.Clear();
        AddGameObjectDynamite(source, codeInfo.GetCodeStatToInt(0));
    }

    public override void Merge(EffectCodeInfo codeInfo, IEffectCodeSource source)
    {
        base.Merge(codeInfo, source);
        _synergyGrade = codeInfo.GetCodeStatToInt(3);
    }

    private async void AddGameObjectDynamite(IEffectCodeSource source, int itemCount)
    {
        if (InGameSynergyManager.Instance.IsRegisteredBattleItem((int)TroubleShooterItemType.DYNAMITE))
            return;

        var specCharacter = SpecDataManager.Instance.GetBattleItemData((int)TroubleShooterItemType.DYNAMITE);
        InGameTile inGameTile = null;

        for (int i = 0; i < itemCount; i++)
        {
            if (InGameTouchManager.Instance.SelectedFirstTileID != -1)
            {
                inGameTile = InGameObjectManager.Instance.InGameGrid.GetTile(InGameTouchManager.Instance.SelectedFirstTileID);
            }
            else
            {
                inGameTile = InGameObjectManager.Instance.InGameGrid.GetRecommandedTile(specCharacter);
            }
            int2 pos = new int2(inGameTile.X, inGameTile.Y);

            var statData = new CharacterStatData((int)TroubleShooterItemType.DYNAMITE, 1, 1, 1);
            var character = await InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.BattleItem,
                typeof(CharacterStateReady), false, HpBarType.None);
            _dynamiteList.Add(character);
            var itemInfo = InGameBattleItemDragDropComponent.InGameBattleItemInfo.Create( character: character, source: source, itemInfoHandler: this);
            InGameSynergyManager.Instance.RegisterBattleItem(itemInfo);
        }
    }

    public void OnUpdate()
    {
        if (_synergyGrade < 2)
            return;
        

    }

    public override void OnCombatStart()
    {
        var troubleShooterSynergyList = SpecDataManager.Instance.GetSpecSynergyList(SynergyType.TROUBLESHOOTER);
        if (troubleShooterSynergyList == null || troubleShooterSynergyList.Count == 0)
            return;

        base.OnCombatStart();
        for (int i = 1; i <= _synergyGrade; i++)
        {
            switch (i)
            {
                //전투 준비 시 중립 지역에 대전차지뢰를 최대 {0}개 설치 가능합니다.(위력은 트러블 슈터 성군원들의 공격력 {1}%로 결정됩니다.)
                case 1:
                    ApplyDynamiteToTile();
                    break;
                case 2:
                    _supplyDuration = troubleShooterSynergyList[i].effect_stat_value_1;
                    _elapsedTime = 0;
                    break;
                case 3:
                    _cannonDuration = troubleShooterSynergyList[i].effect_stat_value_1;
                    _elapsedTime = 0;
                    break;
            }
        }
        _elapsedTime = 0;
    }

    public void ApplyDynamiteToTile()
    {
        var troubleShooterSynergyList = SpecDataManager.Instance.GetSpecSynergyList(SynergyType.TROUBLESHOOTER);
        if (troubleShooterSynergyList == null || troubleShooterSynergyList.Count == 0)
            return;
        var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(allianceType: AllianceType.Player);
        double FinalDamageValue = 0;
        foreach (var character in playerCharacterList)
        {
            FinalDamageValue += character.AD;
        }

        Span<double> stats = stackalloc double[5];
        stats.Clear();
        stats[1] = FinalDamageValue * (double)troubleShooterSynergyList[0].effect_stat_value_2 * 0.01d;

        foreach (var dynamite in _dynamiteList)
        {
            var curDynamiteTile = dynamite.CurrentTile;
            var effectCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.CHAPTER_TRAP,
            0, stats);

            curDynamiteTile.EffectCodeContainer.AddOrMergeEffectCode(effectCodeInfo, null);
        }
        
     }

    public override void OnPreRemoved()
    {
        InGameSynergyManager.Instance.TryRemoveBattleItemFromTarget((int)TroubleShooterItemType.DYNAMITE);

        Debug.LogColor($"trouble shooter Removed", "red");
        base.OnPreRemoved();
    }

    #region Dynamite Call
    /// <summary>
    /// 아이템이 부여되는 로직은 없지만 시너지 특성 상 지뢰또한 같이 사라져야하기때문에 상속받는다.
    /// </summary>
    /// <param name="targetCharacter"></param>
    /// <param name="source"></param>

    public void OnItemApplyDragAndDrop(CharacterController targetCharacter, IEffectCodeSource source)
    {
    }

    public bool OnItemCanApplyDragAndDrop(CharacterController targetCharacter)
    {
        return false;
    }

    public bool OnItemCheckCharacterAffected(CharacterController targetCharacter)
    {
        return false;
    }

    public void OnItemTargetObjectRelease(CharacterController targetCharacter, InGameBattleItemDragDropComponent.ItemState itemState)
    {
        InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetCharacter.SpecCharacter.character_stella_type);
        InGameManager.Instance.RemoveSynergyTeamOnce(AllianceType.Player, targetCharacter.SpecCharacter.character_element_type);
    }

    public void OnItemNotAppliedBeforeCombat(CharacterController targetItemController, IEffectCodeSource source)
    {

    }

    #endregion Dynamite Call
}
