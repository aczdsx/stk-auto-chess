using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor.Localization.Plugins.XLIFF.V12;
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
        SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM),
            $"snd_bgm_chapter{_specStage.chapter_id - 1}"));
        InGameMain.GetInGameMain().SetVignette(_specStage.chapter_id);

        PrepareSpecTileEffectCodeDic();
    }

    public override async void StateInit(object target)
    {
        await TutorialManager.Instance.CheckAndInitTutorial(_specStage.stage_id);

        TutorialManager.Instance.HandleTutorialAction(TutorialTriggerType.GAME_START, 0);

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
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(7.5f, new Vector3(0, 2.0f, -10), 1.0f).Forget();
        else
            ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera).SetCameraSize(8.5f, new Vector3(0, 1.5f, -10), 1.0f).Forget();

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

        var battleDeckList = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.STAGE);

        // LINQ 제거: 직접 루프로 유효하지 않은 캐릭터 제거

        for (int i = battleDeckList.Count - 1; i >= 0; i--)
        {
            if (SpecDataManager.Instance.GetCharacterData(battleDeckList[i].CharacterId) == null)
            {
                battleDeckList.RemoveAt(i);
            }
        }

        CheckOverlapCharacter(battleDeckList);

        // LINQ 제거: 직접 루프로 특정 캐릭터 제거
        if (_specStage.chapter_id == 2 && _specStage.stage_number == 6)
        {
            for (int i = battleDeckList.Count - 1; i >= 0; i--)
            {
                if (battleDeckList[i].CharacterId == 130601)
                {
                    battleDeckList.RemoveAt(i);
                }
            }
        }

        foreach (var character in battleDeckList)
        {
            var characterData = UserDataManager.Instance.GetUserCharacter(character.CharacterId);
            Debug.LogColor($"기존 배치 캐릭터 추가 : {character.CharacterId}");
            var characterStat = new CharacterStatData(characterData.CharacterId, characterData.Level,
                GlobalEffectCodeManager.Instance.GetAllGlobalEffectCodes());

            int x = character.PositionTileX;
            int y = character.PositionTileY;

            int2 coordinate = new int2(x, y);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(characterStat, coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady), true, HpBarType.Synergy | HpBarType.HpBar));
        }

        await UniTask.WhenAll(addCharacterTasks);
        InGameMain.GetInGameMain().InitReadyStateUI(battleDeckList);

        StartDrawingLinesAsync(2.0f).Forget();

        SpawnRuleTiles();

        if (_specStage.chapter_id == 2 && _specStage.stage_number == 5)
        {
            var startTile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(2, 2));
            var endTile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(3, 2));
            if (startTile.OccupiedCharacter != null && endTile.OccupiedCharacter == null)
                InGameMain.GetInGameMain().SetObjectMover(startTile, endTile);
        }

        if (_specStage.chapter_id == 2)
        {
            if (_specStage.stage_number == 1)
            {
                InGameMain.GetInGameMain().SetAlertBottomCharacter(113422301);//멘샤
            }
            else if (_specStage.stage_number == 6)
            {
                // LINQ 제거: 직접 루프로 특정 캐릭터 제거
                for (int i = battleDeckList.Count - 1; i >= 0; i--)
                {
                    if (battleDeckList[i].CharacterId == 113252102)
                    {
                        battleDeckList.RemoveAt(i);
                    }
                }
                InGameMain.GetInGameMain().SetAlertBottomCharacter(113642501);//엘리스
            }
            else if (_specStage.stage_number == 11)
            {
                InGameMain.GetInGameMain().SetAlertBottomCharacter(114653505);//엔키
            }
        }
        else if (_specStage.chapter_id == 3)
        {
            if (_specStage.stage_number == 6)
            {
                InGameMain.GetInGameMain().SetAlertBottomCharacter(113362202);//시이나
            }
        }
    }

    private void CheckOverlapCharacter(List<Cookapps.Stkauto.V1.UserCharacterBattleDeck> battleDeckList)
    {
        // HashSet 사용으로 Contains 성능 최적화 (O(1))
        var obstacleTileIDs = new HashSet<int>(_specStage.obstacle_grid_id);
        var neutralTileIDs = new HashSet<int>(_specStage.neutral_grid_id);
        var reservedPlayerPositions = new HashSet<int2>();

        // 플레이어 캐릭터 위치 중복 해결
        var grid = InGameObjectManager.Instance.InGameGrid;
        for (int i = battleDeckList.Count - 1; i >= 0; i--)
        {
            var character = battleDeckList[i];
            var currentPosition = new int2(character.PositionTileX, character.PositionTileY);
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
                    character.PositionTileX = emptyTile.X;
                    character.PositionTileY = emptyTile.Y;
                    reservedPlayerPositions.Add(new int2(emptyTile.X, emptyTile.Y));
                    Debug.LogColor($"캐릭터 {character.CharacterId} 위치 변경: ({currentPosition.x}, {currentPosition.y}) -> ({emptyTile.X}, {emptyTile.Y})");
                }
                else
                {
                    Debug.LogColor($"캐릭터 {character.CharacterId} 배치 불가능하여 제거");
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

}