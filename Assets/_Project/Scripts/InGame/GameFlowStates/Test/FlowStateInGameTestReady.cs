using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;
using System;

public class FlowStateInGameTestReady : StateReadyBase
{
    private InGameTestConfig _testConfig;
    private StageInfo _specStage;
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
        _testConfig = data as InGameTestConfig;

        if (_testConfig == null)
        {
            Debug.LogError("FlowStateInGameTestReady: TestConfig is null!");
            return;
        }

        // Stage 모드이고 StageId가 설정되어 있으면 스테이지 데이터 로드
        if (_testConfig.Mode == TestMode.Stage && _testConfig.StageId > 0)
        {
            _specStage = SpecDataManager.Instance.GetStageData(_testConfig.StageId);
            if (_specStage != null)
            {
                // InGameManager에 SpecStage 설정 (다른 시스템에서 참조할 수 있도록)
                InGameManager.Instance.SetSpecStageForTest(_specStage);
                Debug.LogColor($"[Test] Stage 모드: {_specStage.chapter_id}-{_specStage.stage_number} (맵: {_specStage.map_size})", "yellow");
                if (_specStage.chapter_id == 1)
                {
                    SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM), $"snd_bgm_chapter{_specStage.chapter_id - 1}"));

                }
                else if (_specStage.chapter_id == 2 || _specStage.chapter_id == 3)
                {
                    SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM), $"snd_bgm_chapter{_specStage.chapter_id}"));
                }
            }
            else
            {
                Debug.LogError($"[Test] 스테이지 데이터를 찾을 수 없음: {_testConfig.StageId}");
            }
        }
        else
        {
            Debug.LogColor($"[Test] Custom 모드: 그리드 {_testConfig.GridWidth}x{_testConfig.GridHeight}", "cyan");
        }
        PrepareSpecTileEffectCodeDic();
    }

    public override async void StateInit(object target)
    {
        // 디버그 UI 생성
        InGameTestDebugUI.Create();

        var addCharacterTasks = new List<UniTask<CharacterController>>();

        // 카메라 설정
        var inGameCamera = ObjectRegistry.GetObject<InGameCamera>(RegistryKey.InGameCamera);

        // 스테이지 모드인 경우
        if (_specStage != null)
        {
            await InitStageMode(addCharacterTasks, inGameCamera);
        }
        else
        {
            // 수동 모드
            await InitManualMode(addCharacterTasks, inGameCamera);
        }

        await UniTask.WhenAll(addCharacterTasks);

        // UI가 있으면 초기화
        var inGameMain = InGameMain.GetInGameMain();
        if (inGameMain != null)
        {
            inGameMain.InitReadyStateUI(new List<Tech.Hive.V1.DeckCharacterPlacement>());
        }

        MainCameraHolder.MainCamera.transform.rotation = Quaternion.Euler(30f, 45f, 0f);

        StartDrawingLinesAsync(2.0f).Forget();
        SpawnRuleTiles();

    }

    /// <summary>
    /// 스테이지 모드: SpecData에서 몬스터/장애물 정보를 가져와서 배치
    /// </summary>
    private async UniTask InitStageMode(List<UniTask<CharacterController>> addCharacterTasks, InGameCamera inGameCamera)
    {
        // 카메라 설정 (챕터에 따라)
        bool isSize75 = _specStage.chapter_id == 1 || _specStage.chapter_id == 2;
        if (inGameCamera != null)
        {
            if(InGameObjectManager.Instance.InGameGrid.Width > 5)
            {
                inGameCamera.SetCameraPositionMode(InGameCamera.CameraPositionMode.LargeSize);
            }
            else
            {
                inGameCamera.SetCameraPositionMode(InGameCamera.CameraPositionMode.Default);
            }
        }

        // 스테이지 몬스터 배치
        List<StageMonster> monsters = SpecDataManager.Instance.GetStageMonsterList(
            _specStage.chapter_id,
            _specStage.stage_number,
            _specStage.difficulty_type);

        float monsterMultipleHp = 1.0f;
        foreach (var monster in monsters)
        {
            monsterMultipleHp = monster.multiple_hp;
            Debug.LogColor($"[Test] 스테이지 몬스터 추가: {monster.monster_id}", "yellow");

            var statData = new MonsterStatData(
                monster.monster_id,
                monster.monster_lv,
                monster.multiple_atk,
                monster.multiple_hp);

            string[] coordinates = monster.coordinate.Split(',');
            int x = int.Parse(coordinates[0]);
            int y = int.Parse(coordinates[1]);
            int2 coordinate = new int2(x, y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady),
                true,
                HpBarType.Synergy | HpBarType.HpBar));
        }

        // 장애물 설치
        foreach (var gridID in _specStage.obstacle_grid_id)
        {
            addCharacterTasks.Add(
                InGameObjectManager.Instance.AddNonStatObstacleToField(
                    gridID,
                    _specStage.obstacle_id,
                    AllianceType.Wall));
        }

        // 체력이 있는 장애물 설치
        foreach (var gridID in _specStage.neutral_grid_id)
        {
            Debug.LogColor($"[Test] neutral 추가: {_specStage.neutral_wall_id}", "yellow");
            var statData = new CharacterStatData(_specStage.neutral_wall_id, 1, 1, monsterMultipleHp);

            var tile = InGameObjectManager.Instance.GetInGameTile(gridID);
            int2 coordinate = new int2(tile.X, tile.Y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady),
                false,
                HpBarType.None));
        }

        // 테스트용 플레이어 캐릭터 배치 (수동 설정된 것 사용)
        foreach (var player in _testConfig.PlayerCharacters)
        {
            if (player.CharacterId <= 0) continue;

            Debug.LogColor($"[Test] 플레이어 추가: {player.CharacterId} at ({player.GridX}, {player.GridY})", "cyan");
            var statData = new CharacterStatData(
                player.CharacterId,
                player.Level,
                player.MultipleAtk,
                player.MultipleHp
            );

            int2 coordinate = new int2(player.GridX, player.GridY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady),
                true,
                HpBarType.Synergy | HpBarType.HpBar
            ));
        }
    }

    /// <summary>
    /// 수동 모드: TestConfig에서 설정한 캐릭터 정보로 배치
    /// </summary>
    private UniTask InitManualMode(List<UniTask<CharacterController>> addCharacterTasks, InGameCamera inGameCamera)
    {
        // 카메라 설정
        if (inGameCamera != null)
        {
            inGameCamera.SetCameraSize(_testConfig.CameraSize, _testConfig.CameraPosition, 1.0f).Forget();
        }

        // 적 캐릭터 배치
        foreach (var enemy in _testConfig.EnemyCharacters)
        {
            if (enemy.CharacterId <= 0) continue;

            Debug.LogColor($"[Test] 적 추가: {enemy.CharacterId} at ({enemy.GridX}, {enemy.GridY})", "yellow");
            var statData = new MonsterStatData(
                enemy.CharacterId,
                enemy.Level,
                enemy.MultipleAtk,
                enemy.MultipleHp
            );

            int2 coordinate = new int2(enemy.GridX, enemy.GridY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Enemy,
                typeof(CharacterStateReady),
                true,
                HpBarType.Synergy | HpBarType.HpBar
            ));
        }

        // 내 캐릭터 배치
        foreach (var player in _testConfig.PlayerCharacters)
        {
            if (player.CharacterId <= 0) continue;

            Debug.LogColor($"[Test] 플레이어 추가: {player.CharacterId} at ({player.GridX}, {player.GridY})", "cyan");
            var statData = new CharacterStatData(
                player.CharacterId,
                player.Level,
                player.MultipleAtk,
                player.MultipleHp
            );

            int2 coordinate = new int2(player.GridX, player.GridY);
            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(
                statData,
                coordinate,
                AllianceType.Player,
                typeof(CharacterStateReady),
                true,
                HpBarType.Synergy | HpBarType.HpBar
            ));
        }

        return UniTask.CompletedTask;
    }

    private async UniTaskVoid AutoStartCombatAsync()
    {
        Debug.LogColor("[Test] 1초 후 전투 시작...", "cyan");
        // 1초 대기 후 자동 전투 시작
        await UniTask.Delay(1000);
        InGameMainFlowManager.Instance.AddNextState<FlowStateInGameTestCombat>((_testConfig));
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
        if(_specStage is null)
        {
            return;
        }

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
