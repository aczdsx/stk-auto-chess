using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageReady : StateReadyBase
{
    private SpecStage _specStage;

    //Key: SpecTileEffectCode.id, Value: SpecTileEffectCode
    private Dictionary<int, SpecTileEffectCode> _specTileEffectCodeDic = null;

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
        _specStage = data as SpecStage;
        SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM),
            $"snd_bgm_chapter{_specStage.chapter_id - 1}"));
        InGameMain.GetInGameMain().SetVignette(_specStage.chapter_id);

        PrepareSpecTileEffectCodeDic();
    }

    public override async void StateInit(object target)
    {
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<SpecStageMonster> monsters =
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
                typeof(CharacterStateReady), true, HpBarType.Synergy));
        }

        bool isSize75 = _specStage.chapter_id == 1 || _specStage.chapter_id == 2; // [TODO] 나중에 데이터로 
        if (isSize75)
            InGameCommanderManager.Instance.InGameCamera.SetCameraSize(7.5f, new Vector3(0, 2.0f, -10), 1.0f).Forget();
        else
            InGameCommanderManager.Instance.InGameCamera.SetCameraSize(8.5f, new Vector3(0, 1.5f, -10), 1.0f).Forget();

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
        List<int> obstacleTileIDs = _specStage.obstacle_grid_id.ToList();
        List<int> neutralTileIDs = _specStage.neutral_grid_id.ToList();

        // 겹치는 캐릭터들을 배치 가능한 위치로 이동
        for (int i = battleDeckList.Count - 1; i >= 0; i--)
        {
            var character = battleDeckList[i];
            var currentTile = InGameObjectManager.Instance.InGameGrid.GetTile(new int2(character.PositionTileX, character.PositionTileY));
            var currentTileID = currentTile.View.ID;

            // 장애물이나 중립 타일과 겹치는지 확인
            bool isOverlapping = obstacleTileIDs.Contains(currentTileID) || neutralTileIDs.Contains(currentTileID);

            if (isOverlapping)
            {
                // 현재 위치에서 점진적으로 범위를 넓혀가며 배치 가능한 위치 찾기
                var emptyTile = InGameObjectManager.Instance.InGameGrid.FindNearestEmptyTile(
                    character.PositionTileX, character.PositionTileY, obstacleTileIDs, neutralTileIDs);
                if (emptyTile != null)
                {
                    // 새로운 위치로 업데이트
                    character.PositionTileX = emptyTile.X;
                    character.PositionTileY = emptyTile.Y;
                    Debug.LogColor($"캐릭터 {character.CharacterId} 위치 변경: ({character.PositionTileX}, {character.PositionTileY})");
                }
                else
                {
                    // 배치 가능한 위치가 없으면 제거
                    Debug.LogColor($"캐릭터 {character.CharacterId} 배치 불가능하여 제거");
                    battleDeckList.RemoveAt(i);
                }
            }
        }

        if (_specStage.chapter_id == 2 && _specStage.stage_number == 6)
        {
            battleDeckList.RemoveAll(l => l.CharacterId == 130601);
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
                typeof(CharacterStateReady), true, HpBarType.Synergy));
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
                InGameMain.GetInGameMain().SetAlertBottomCharacter(130201);
            }
            else if (_specStage.stage_number == 6)
            {
                battleDeckList.RemoveAll(l => l.CharacterId == 130601);
                InGameMain.GetInGameMain().SetAlertBottomCharacter(130301);
            }
            else if (_specStage.stage_number == 11)
            {
                InGameMain.GetInGameMain().SetAlertBottomCharacter(140601);
            }
        }
        else if (_specStage.chapter_id == 3)
        {
            if (_specStage.stage_number == 6)
            {
                InGameMain.GetInGameMain().SetAlertBottomCharacter(130501);
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

    // private void SpawnRuleTiles()
    // {
    //     // 기존 코드였던 InGameManager.ECC에 Rule을 추가하는 방식에서 
    //     // 각 타일의 ECC 에서 룰을 틀고있게끔 수정 25.11.14

    //     // 더이상 _specStage.effect_code_name, _specStage.effect_code_name_2, _specStage.effect_code_name_3 는 사용하지 않음
    //     // SpecTileEffectCode 를 사용하여 타일의 ECC 에 룰을 추가하는 방식으로 수정 25.11.17

    //     if (_specStage.effect_code_rule_tile.Length > 0)
    //     {
    //         // 0인자는 effectcodestat, 1인자는 타일인덱스
    //         Span<double> tileRuleStats = stackalloc double[2];
    //         tileRuleStats.Clear();

    //         tileRuleStats[0] = _specStage.effect_code_stat;
    //         var effectCodeID = new EffectCodeInfo((long)_specStage.effect_code_name, 0, tileRuleStats);

    //         for (int i = 0; i < _specStage.effect_code_rule_tile.Length; i++)
    //         {
    //             effectCodeID.ModifyCodeStat(1, _specStage.effect_code_rule_tile[i]);

    //             var targetRuleTile = InGameObjectManager.Instance.GetInGameTile(_specStage.effect_code_rule_tile[i]);
    //             targetRuleTile?.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
    //         }
    //     }

    //     if (_specStage.effect_code_rule_tile_2.Length > 0)
    //     {
    //         Span<double> tileRuleStats = stackalloc double[2];
    //         tileRuleStats.Clear();

    //         tileRuleStats[0] = _specStage.effect_code_stat_2;
    //         var effectCodeID = new EffectCodeInfo((long)_specStage.effect_code_name_2, 0, tileRuleStats);

    //         for (int i = 0; i < _specStage.effect_code_rule_tile_2.Length; i++)
    //         {
    //             effectCodeID.ModifyCodeStat(1,_specStage.effect_code_rule_tile_2[i]);
    //             var targetRuleTile = InGameObjectManager.Instance.GetInGameTile(_specStage.effect_code_rule_tile_2[i]);
    //             targetRuleTile?.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
    //         }
    //     }

    //     if (_specStage.effect_code_rule_tile_3.Length > 0)
    //     {
    //         Span<double> tileRuleStats = stackalloc double[2];
    //         tileRuleStats.Clear();

    //         tileRuleStats[0] = _specStage.effect_code_stat_3;
    //         var effectCodeID = new EffectCodeInfo((long)_specStage.effect_code_name_3, 0, tileRuleStats);

    //         for (int i = 0; i < _specStage.effect_code_rule_tile_3.Length; i++)
    //         {
    //             effectCodeID.ModifyCodeStat(1,_specStage.effect_code_rule_tile_3[i]);
    //             var targetRuleTile = InGameObjectManager.Instance.GetInGameTile(_specStage.effect_code_rule_tile_3[i]);
    //             targetRuleTile?.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
    //         }
    //     }

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

        _specTileEffectCodeDic = new Dictionary<int, SpecTileEffectCode>();
        foreach (var specTileEffectCode in SpecDataManager.Instance.GetSpecTileEffectCodeList())
        {
            _specTileEffectCodeDic[specTileEffectCode.id] = specTileEffectCode;
        }

    }
}