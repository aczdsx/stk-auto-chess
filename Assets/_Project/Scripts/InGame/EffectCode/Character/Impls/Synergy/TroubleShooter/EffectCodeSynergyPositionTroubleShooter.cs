using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Unity.Mathematics;

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

    private int _synergyGrade;
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
        if (InGameSynergyManager.Instance.IsRegisteredBattleItem((int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE))
            return;
        var specCharacter = SpecDataManager.Instance.GetSpecCharacter((int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE);

        // 배틀덱 데이터에서 저장된 dynamite 위치 가져오기
        List<InGameTile> savedDynamiteTileList = new List<InGameTile>();
        var deckData = ServerDataManager.Instance.Deck.GetDeck(InGameType.STAGE);
        if (deckData != null)
        {
            var additionalData = deckData.GetAdditionalData();
            if (additionalData != null)
            {
                // troubleshooter1, 2, 3 위치 모두 확인
                int2[] savedPositions = new int2[]
                {
                    new int2(additionalData.troubleshooter1GridX, additionalData.troubleshooter1GridY),
                    new int2(additionalData.troubleshooter2GridX, additionalData.troubleshooter2GridY),
                    new int2(additionalData.troubleshooter3GridX, additionalData.troubleshooter3GridY)
                };

                foreach (var pos in savedPositions)
                {
                    if (pos.x != -1 && pos.y != -1)
                    {
                        InGameTile dynamiteTile = InGameObjectManager.Instance.InGameGrid.GetTile(pos);
                        if (dynamiteTile != null && !dynamiteTile.IsOccupied())
                        {
                            savedDynamiteTileList.Add(dynamiteTile);
                        }
                    }
                }
            }
        }

        int savedTileIndex = 0;

        for (int i = 0; i < itemCount; i++)
        {
            InGameTile inGameTile = null;

            // 배틀덱 데이터에서 저장된 타일이 있으면 우선 사용
            if (savedTileIndex < savedDynamiteTileList.Count)
            {
                inGameTile = savedDynamiteTileList[savedTileIndex++];
                Debug.LogColor($"저장된 정보로 지뢰 로드!: {inGameTile.X}, {inGameTile.Y}");
            }
            else
            {
                // 저장된 타일이 없거나 부족하면 기존 로직 수행
                // 1. 선택된 타일이 있으면 우선 확인
                if (InGameTouchManager.Instance.SelectedFirstTileID != -1)
                {
                    inGameTile = InGameObjectManager.Instance.InGameGrid.GetTile(InGameTouchManager.Instance.SelectedFirstTileID);
                    if (inGameTile != null && inGameTile.IsOccupied())
                    {
                        inGameTile = null; // 점유되어 있으면 무시
                    }
                }

                // 3. 비어있는 타일을 찾지 못한 경우 AllianceType.None을 우선하는 빈 타일 사용
                if (inGameTile == null)
                {
                    inGameTile = InGameObjectManager.Instance.InGameGrid.GetEmptyTilePreferringNone();
                }
                Debug.LogColor($"랜덤위치에 지뢰 추가!: {inGameTile?.X}, {inGameTile?.Y}");
            }

            if (inGameTile == null)
            {
                Debug.LogWarning("[EffectCodeSynergyPositionTroubleShooter] Failed to find tile for dynamite");
                continue;
            }

            int2 pos = new int2(inGameTile.X, inGameTile.Y);

            var statData = new CharacterStatData((int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE, 1, 1, 1);
            var character = await InGameObjectManager.Instance.AddCharacterToField(statData, pos, AllianceType.BattleItem,
                typeof(CharacterStateReady), false, HpBarType.None);

            _dynamiteList.Add(character);
            var itemInfo = InGameBattleItemDragDropComponent.InGameBattleItemInfo.Create(character: character, source: source, itemInfoHandler: this);
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
                    ApplyDynamiteToTile(troubleShooterSynergyList.Find(synergy => synergy.grade == 1));
                    // InjectDroppingBombsEffectCode(troubleShooterSynergyList.Find(synergy => synergy.grade == 3));
                    // InjectSupplyEffectCode(troubleShooterSynergyList[1]);
                    break;
                case 2:
                    InjectSupplyEffectCode(troubleShooterSynergyList[1]);
                    // AddSupplyEffectCode(troubleShooterSynergyList[1]);
                    break;
                case 3:
                    InjectDroppingBombsEffectCode(troubleShooterSynergyList.Find(synergy => synergy.grade == 3));
                    break;
            }
        }
    }

    public void ApplyDynamiteToTile(ISpecSynergyData synergyData)
    {
        var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(allianceType: AllianceType.Player);
        double FinalDamageValue = 0;
        int troubleShooterCount = 0;

        foreach (var character in playerCharacterList)
        {
            if (character.SpecCharacter.character_stella_type != SynergyType.TROUBLESHOOTER)
                continue;
            troubleShooterCount++;
            FinalDamageValue += character.AD;
        }
        FinalDamageValue = FinalDamageValue / troubleShooterCount;

        Span<double> stats = stackalloc double[3];
        stats.Clear();
        stats[1] = FinalDamageValue * (double)synergyData.effect_stat_value_2 * 0.01d;
        stats[2] = synergyData.synergy_group_id;

        foreach (var dynamite in _dynamiteList)
        {
            var curDynamiteTile = dynamite.CurrentTile;
            stats[0] = curDynamiteTile.View.ID;

            var effectCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.BATTLE_ITEM_DYNAMITE,
            0, stats);

            curDynamiteTile.EffectCodeContainer.AddOrMergeEffectCode(effectCodeInfo, null);
        }

    }

    public void InjectSupplyEffectCode(ISpecSynergyData synergyData)
    {
        Span<double> stats = stackalloc double[1];
        stats.Clear();
        stats[0] = synergyData.effect_stat_value_1;//Time
        // stats[0] = 3f;
        var effectCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.BATTLE_ITEM_SUPPLY,
        0, stats);

        InGameManager.Instance.TeamEcc.AddOrMergeEffectCode(effectCodeInfo, null, AllianceType.Player);
    }

    public void InjectDroppingBombsEffectCode(ISpecSynergyData synergyData)
    {
        Span<double> stats = stackalloc double[4];
        var playerCharacterList = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
        double FinalDamageValue = 0;
        int troubleShooterCount = 0;
        foreach (var character in playerCharacterList)
        {
            if (character.SpecCharacter.character_stella_type != SynergyType.TROUBLESHOOTER)
                continue;
            troubleShooterCount++;
            FinalDamageValue += character.AD;
        }
        
        FinalDamageValue = FinalDamageValue / troubleShooterCount;
        

        stats.Clear();
        stats[0] = synergyData.effect_stat_value_1;//Time
        stats[1] = FinalDamageValue * (double)synergyData.effect_stat_value_3 * 0.01d;//Damage
        stats[2] = synergyData.effect_stat_value_2;//Count
        stats[3] = synergyData.synergy_group_id;// killlog synergy id

        var effectCodeInfo = new EffectCodeInfo((long)EffectCodeNameType.DROPPING_BOMBS, 0, stats);

        InGameManager.Instance.TeamEcc.AddOrMergeEffectCode(effectCodeInfo, null, AllianceType.Player);
    }
    public override void OnPreRemoved()
    {
        InGameSynergyManager.Instance.TryRemoveBattleItemFromTarget((int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE);

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
        InGameSynergyManager.Instance.ModifyBattleItemState(InGameBattleItemDragDropComponent.ItemState.ITEM_APPLIED, _dynamiteList[0], null);
    }

    #endregion Dynamite Call
}
