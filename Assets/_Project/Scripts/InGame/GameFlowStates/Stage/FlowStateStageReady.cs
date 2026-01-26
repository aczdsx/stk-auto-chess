using System;
using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Tech.Hive.V1;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageReady : StateReadyBase
{
    private StageInfo _specStage;

    //Key: SpecTileEffectCode.id, Value: SpecTileEffectCode
    private Dictionary<int, TileEffectCode> _specTileEffectCodeDic = null;

    private enum TileRuleStatType
    {
        Tileidx = 0,
        EffectStat_1 = 1,
        EffectStat_2 = 2,
        EffectStat_3 = 3,
        End = 4,
    }
    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specStage = data as StageInfo;
        // SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM),
        //     $"snd_bgm_chapter{_specStage.chapter_id - 1}"));
        if (_specStage.chapter_id == 1)
        {
            SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM), $"snd_bgm_chapter{_specStage.chapter_id - 1}"));

        }
        else if (_specStage.chapter_id == 2 || _specStage.chapter_id == 3)
        {
            SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM), $"snd_bgm_chapter{_specStage.chapter_id}"));
        }
        InGameMain.GetInGameMain().SetVignette(_specStage.chapter_id);

        PrepareSpecTileEffectCodeDic();
    }

    public override async void StateInit(object target)
    {
        await TutorialManager.Instance.CheckAndInitTutorial(_specStage.stage_id);

        TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.GAME_START, "0");

        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<StageMonster> monsters =
            SpecDataManager.Instance.GetStageMonsterList(_specStage.chapter_id, _specStage.stage_number,
                _specStage.difficulty_type);

        float monsterMultipleHp = 1.0f;
        foreach (var monster in monsters)
        {
            monsterMultipleHp = monster.multiple_hp;
            Debug.LogColor($"monster 추가 : {monster.monster_id}");
            var statData = new CharacterStatData(monster.monster_id, monster.monster_lv, monster.multiple_atk,
                monster.multiple_hp);

            string[] coordinates = monster.coordinate.Split(',');
            int x = int.Parse(coordinates[0]);
            int y = int.Parse(coordinates[1]);
            int2 coordinate = new int2(x, y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady), true, HpBarType.Synergy | HpBarType.HpBar));
        }

        bool isSize75 = _specStage.chapter_id == 1 || _specStage.chapter_id == 2; // [TODO] 나중에 데이터로 
        if (isSize75)
        {
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPositionMode(InGameCamera.CameraPositionMode.Default);
        }
        else
        {
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraPositionMode(InGameCamera.CameraPositionMode.LargeSize);
        }
        ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetForceCameraRotation(new Vector3(30f, 45f, 0f));

        // 장애물 설치
        foreach (var gridID in _specStage.obstacle_grid_id)
        {
            addCharacterTasks.Add(
                InGameObjectManager.Instance.AddNonStatObstacleToField(gridID, _specStage.obstacle_id,
                    AllianceType.Wall));
        }

        // 체력이 있는 장애물 설치
        foreach (var gridID in _specStage.neutral_grid_id)
        {
            Debug.LogColor($"neutral 추가 : {_specStage.neutral_wall_id}");
            var statData = new CharacterStatData(_specStage.neutral_wall_id, 1, 1, monsterMultipleHp);

            var tile = InGameObjectManager.Instance.GetInGameTile(gridID);
            int2 coordinate = new int2(tile.X, tile.Y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady), false, HpBarType.None));
        }

        var deckData = ServerDataManager.Instance.Deck.GetDeck(InGameType.STAGE);
        var battleDeckList = deckData != null
            ? new List<DeckCharacterPlacement>(deckData.CharacterPlacements)
            : new List<DeckCharacterPlacement>();

        // LINQ 제거: 직접 루프로 유효하지 않은 캐릭터 제거
        for (int i = battleDeckList.Count - 1; i >= 0; i--)
        {
            var characterData = ServerDataManager.Instance.Character.GetCharacter(battleDeckList[i].CharacterId);
            if (characterData == null || SpecDataManager.Instance.GetCharacterData((int)characterData.CharacterId) == null)
            {
                battleDeckList.RemoveAt(i);
            }
        }

        CheckOverlapCharacter(battleDeckList);

        foreach (var placement in battleDeckList)
        {
            var characterData = ServerDataManager.Instance.Character.GetCharacter(placement.CharacterId);
            if (characterData == null) continue;

            Debug.LogColor($"기존 배치 캐릭터 추가 : {characterData.CharacterId}");
            var characterStat = new CharacterStatData((int)characterData.CharacterId, (int)characterData.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            int x = placement.GridX;
            int y = placement.GridY;

            int2 coordinate = new int2(x, y);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        var characters = await UniTask.WhenAll(addCharacterTasks);

        InGameManager.Instance.OnFlowStateStageReadyStart();

        // 튜토리얼 활성화 시 캐릭터들을 TutorialTargetRegistry에 등록
        if (TutorialManager.Instance.HasTutorialStage)
        {
            RegisterCharactersForTutorial(characters);
        }

        InGameMain.GetInGameMain().InitReadyStateUI(battleDeckList);

        StartDrawingLinesAsync(2.0f).Forget();

        SpawnRuleTiles();

        // if (_specStage.chapter_id == 2 && _specStage.stage_number == 5)
        // {
        //     var startTile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(2, 2));
        //     var endTile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(3, 2));
        //     if (startTile.OccupiedCharacter != null && endTile.OccupiedCharacter == null)
        //         InGameMain.GetInGameMain().SetObjectMover(startTile, endTile);
        // }
    }

    private void CheckOverlapCharacter(List<DeckCharacterPlacement> battleDeckList)
    {
        // HashSet 사용으로 Contains 성능 최적화 (O(1))
        var obstacleTileIDs = new HashSet<int>(_specStage.obstacle_grid_id);
        var neutralTileIDs = new HashSet<int>(_specStage.neutral_grid_id);
        var reservedPlayerPositions = new HashSet<int2>();

        // 플레이어 캐릭터 위치 중복 해결
        var grid = InGameObjectManager.Instance.InGameGrid;
        for (int i = battleDeckList.Count - 1; i >= 0; i--)
        {
            var placement = battleDeckList[i];
            var currentPosition = new int2(placement.GridX, placement.GridY);
            var currentTile = grid.GetTile(currentPosition);
            var currentTileID = currentTile.View.ID;

            // 장애물이나 중립 타일과 겹치는지 확인
            bool isOverlappingWithObstacle = obstacleTileIDs.Contains(currentTileID) || neutralTileIDs.Contains(currentTileID);

            // 다른 플레이어 캐릭터와 겹치는지 확인
            bool isOverlappingWithPlayer = reservedPlayerPositions.Contains(currentPosition);

            if (isOverlappingWithObstacle || isOverlappingWithPlayer)
            {
                // FindNearestEmptyTile은 List<int>를 받으므로 변환
                var obstacleList = new List<int>(obstacleTileIDs);
                var neutralList = new List<int>(neutralTileIDs);

                // 예약된 위치의 타일 ID를 임시로 장애물 목록에 추가하여 제외
                var tempObstacleList = new List<int>(obstacleList);
                foreach (var reservedPos in reservedPlayerPositions)
                {
                    var reservedTile = grid.GetTile(reservedPos);
                    if (reservedTile != null)
                    {
                        tempObstacleList.Add(reservedTile.View.ID);
                    }
                }

                // FindNearestEmptyTile 호출 (예약된 위치는 이미 장애물로 처리됨)
                var emptyTile = grid.FindNearestEmptyTile(currentPosition.x, currentPosition.y, tempObstacleList, neutralList);

                if (emptyTile != null)
                {
                    placement.GridX = emptyTile.X;
                    placement.GridY = emptyTile.Y;
                    reservedPlayerPositions.Add(new int2(emptyTile.X, emptyTile.Y));
                    Debug.LogColor($"캐릭터 {placement.CharacterId} 위치 변경: ({currentPosition.x}, {currentPosition.y}) -> ({emptyTile.X}, {emptyTile.Y})");
                }
                else
                {
                    Debug.LogColor($"캐릭터 {placement.CharacterId} 배치 불가능하여 제거");
                    battleDeckList.RemoveAt(i);
                }
            }
            else
            {
                reservedPlayerPositions.Add(currentPosition);
            }
        }
    }


    public override void StateStart()
    {
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
        SaveCurrentDeckAsync().Forget();
    }

    private async UniTaskVoid SaveCurrentDeckAsync()
    {
        var characterPlacements = new List<DeckCharacterPlacement>();
        var playerCharacters = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);

        foreach (var character in playerCharacters)
        {
            if (character?.CurrentTile == null) continue;

            characterPlacements.Add(new DeckCharacterPlacement
            {
                CharacterId = (uint)character.CharacterId,
                GridX = character.CurrentTile.X,
                GridY = character.CurrentTile.Y
            });
        }
        var additionalData = new DeckAdditionalData()
        {
            supernovaCharacterId = 0,

            troubleshooter1GridX = -1,
            troubleshooter1GridY = -1,

            troubleshooter2GridX = -1,
            troubleshooter2GridY = -1,

            troubleshooter3GridX = -1,
            troubleshooter3GridY = -1
        };

        var dynamiteList = InGameSynergyManager.Instance.GetBattleItemList((int)EffectCodeNameType.BATTLE_ITEM_DYNAMITE);
        if (dynamiteList is not null && dynamiteList.Count > 0)
        {
            int index = 0;
            foreach (var dynamite in dynamiteList)
            {
                switch (index)
                {
                    case 0:
                        additionalData.troubleshooter1GridX = dynamite.CurrentTile.X;
                        additionalData.troubleshooter1GridY = dynamite.CurrentTile.Y;
                        break;
                    case 1:
                        additionalData.troubleshooter2GridX = dynamite.CurrentTile.X;
                        additionalData.troubleshooter2GridY = dynamite.CurrentTile.Y;
                        break;
                    case 2:
                        additionalData.troubleshooter3GridX = dynamite.CurrentTile.X;
                        additionalData.troubleshooter3GridY = dynamite.CurrentTile.Y;
                        break;
                }
                index++;
            }
        }


        var supernovaList = InGameSynergyManager.Instance.GetBattleItemInfoList((int)EffectCodeNameType.BATTLE_ITEM_SUPERNOVA);

        if (supernovaList is not null && supernovaList.Count > 0)
        {
            var supernovaInfo = supernovaList[0];
            if (supernovaInfo.itemState == InGameBattleItemDragDropComponent.ItemState.ITEM_APPLIED)
            {
                additionalData.supernovaCharacterId = supernovaInfo.targetObj.SpecCharacter.id;
            }
        }
        // STAGE의 덱 슬롯 ID는 1
        await NetManager.Instance.Deck.SaveAsync((int)InGameType.STAGE, string.Empty, characterPlacements, clientData: additionalData.ToGrpcData());
    }

    private void SpawnRuleTiles()
    {
        if (_specStage.effect_code_id > 0)
        {
            Span<double> tileRuleStats = stackalloc double[(int)TileRuleStatType.End];
            tileRuleStats.Clear();

            var specTileEffectCode = _specTileEffectCodeDic[_specStage.effect_code_id];
            tileRuleStats[(int)TileRuleStatType.EffectStat_1] = specTileEffectCode.effect_code_stat_1;
            tileRuleStats[(int)TileRuleStatType.EffectStat_2] = specTileEffectCode.effect_code_stat_2;
            tileRuleStats[(int)TileRuleStatType.EffectStat_3] = specTileEffectCode.effect_code_stat_3;

            var effectCodeID = new EffectCodeInfo((long)specTileEffectCode.effect_code_name, 0, tileRuleStats);

            for (int i = 0; i < _specStage.effect_code_rule_tile.Length; i++)
            {
                effectCodeID.ModifyCodeStat((int)TileRuleStatType.Tileidx, _specStage.effect_code_rule_tile[i]);

                var targetRuleTile = InGameObjectManager.Instance.GetInGameTile(_specStage.effect_code_rule_tile[i]);
                targetRuleTile?.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
            }
        }

        if (_specStage.effect_code_id_2 > 0)
        {
            Span<double> tileRuleStats = stackalloc double[(int)TileRuleStatType.End];
            tileRuleStats.Clear();

            var specTileEffectCode = _specTileEffectCodeDic[_specStage.effect_code_id_2];
            tileRuleStats[(int)TileRuleStatType.EffectStat_1] = specTileEffectCode.effect_code_stat_1;
            tileRuleStats[(int)TileRuleStatType.EffectStat_2] = specTileEffectCode.effect_code_stat_2;
            tileRuleStats[(int)TileRuleStatType.EffectStat_3] = specTileEffectCode.effect_code_stat_3;

            var effectCodeID = new EffectCodeInfo((long)specTileEffectCode.effect_code_name, 0, tileRuleStats);

            for (int i = 0; i < _specStage.effect_code_rule_tile_2.Length; i++)
            {
                effectCodeID.ModifyCodeStat((int)TileRuleStatType.Tileidx, _specStage.effect_code_rule_tile_2[i]);

                var targetRuleTile = InGameObjectManager.Instance.GetInGameTile(_specStage.effect_code_rule_tile_2[i]);
                targetRuleTile?.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
            }
        }

        if (_specStage.effect_code_id_3 > 0)
        {
            Span<double> tileRuleStats = stackalloc double[(int)TileRuleStatType.End];
            tileRuleStats.Clear();

            var specTileEffectCode = _specTileEffectCodeDic[_specStage.effect_code_id_3];
            tileRuleStats[(int)TileRuleStatType.EffectStat_1] = specTileEffectCode.effect_code_stat_1;
            tileRuleStats[(int)TileRuleStatType.EffectStat_2] = specTileEffectCode.effect_code_stat_2;
            tileRuleStats[(int)TileRuleStatType.EffectStat_3] = specTileEffectCode.effect_code_stat_3;

            var effectCodeID = new EffectCodeInfo((long)specTileEffectCode.effect_code_name, 0, tileRuleStats);

            for (int i = 0; i < _specStage.effect_code_rule_tile_3.Length; i++)
            {
                effectCodeID.ModifyCodeStat((int)TileRuleStatType.Tileidx, _specStage.effect_code_rule_tile_3[i]);

                var targetRuleTile = InGameObjectManager.Instance.GetInGameTile(_specStage.effect_code_rule_tile_3[i]);
                targetRuleTile?.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
            }
        }
    }

    private void PrepareSpecTileEffectCodeDic()
    {
        if (_specTileEffectCodeDic != null)
        {
            return;
        }

        _specTileEffectCodeDic = new Dictionary<int, TileEffectCode>();
        foreach (var specTileEffectCode in SpecDataManager.Instance.GetSpecTileEffectCodeList())
        {
            _specTileEffectCodeDic[specTileEffectCode.id] = specTileEffectCode;
        }
    }

    /// <summary>
    /// 튜토리얼용으로 캐릭터들을 TutorialTargetRegistry에 등록
    /// 같은 캐릭터ID가 여러 개면 인덱스 추가 (130601_0, 130601_1)
    /// 유일하면 ID만 사용 (130601)
    /// </summary>
    private void RegisterCharactersForTutorial(CharacterController[] characters)
    {
        // null 제외한 캐릭터 리스트
        var validCharacters = new List<CharacterController>();
        foreach (var character in characters)
        {
            if (character != null)
                validCharacters.Add(character);
        }

        // 캐릭터 ID별 개수 카운트
        var idCount = new Dictionary<int, int>();
        foreach (var character in validCharacters)
        {
            if (idCount.ContainsKey(character.CharacterId))
                idCount[character.CharacterId]++;
            else
                idCount[character.CharacterId] = 1;
        }

        // 중복 캐릭터용 인덱스 추적
        var idIndex = new Dictionary<int, int>();

        foreach (var character in validCharacters)
        {
            var characterView = character.GetCharacterView();
            if (characterView == null) continue;

            var tutorialTarget = characterView.gameObject.GetComponent<TutorialTarget>();
            if (tutorialTarget == null)
            {
                tutorialTarget = characterView.gameObject.AddComponent<TutorialTarget>();
            }

            string targetId;
            if (idCount[character.CharacterId] > 1)
            {
                // 중복 캐릭터: 인덱스 추가
                if (!idIndex.ContainsKey(character.CharacterId))
                    idIndex[character.CharacterId] = 0;

                targetId = $"{character.CharacterId}_{idIndex[character.CharacterId]++}";
            }
            else
            {
                // 유일한 캐릭터: ID만 사용
                targetId = character.CharacterId.ToString();
            }

            tutorialTarget.SetTargetId(targetId);
        }
    }
}