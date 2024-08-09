using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using CookApps.Obfuscator;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageReady : StateReadyBase
{
    private SpecStage _specStage;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specStage = data as SpecStage;
        SoundManager.Instance.PlayBGM((SoundBGM)Enum.Parse(typeof(SoundBGM), $"snd_bgm_chapter{_specStage.chapter_id - 1}"));
        InGameMain.GetInGameMain().SetVignette(_specStage.chapter_id);
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

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Enemy,
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
            addCharacterTasks.Add(InGameObjectManager.Instance.AddNonStatObstacleToField(gridID, _specStage.obstacle_id, AllianceType.Wall));
        }

        // 체력이 있는 장애물 설치
        foreach (var gridID in _specStage.neutral_grid_id)
        {
            Debug.LogColor($"neutral 추가 : {_specStage.neutral_wall_id}");
            var statData = new CharacterStatData(_specStage.neutral_wall_id, 1, 1, monsterMultipleHp);

            var tile = InGameObjectManager.Instance.GetInGameTile(gridID);
            int2 coordinate = new int2(tile.X, tile.Y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Neutral,
                typeof(CharacterStateReady), false, HpBarType.None));
        }

        var battleDeckList = UserDataManager.Instance.GetUserCharacterBattleDeckList(InGameType.STAGE);
        List<ObfuscatorInt> tileIDList = _specStage.obstacle_grid_id.ToList();

        battleDeckList.RemoveAll(l =>
        {
            return tileIDList.Exists(t =>
                t.Value == InGameObjectManager.Instance.InGameGrid.GetTile(new int2(l.PositionTileX, l.PositionTileY))
                    .View
                    .ID);
        });

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
        
        InGameObjectManager.Instance.DrawPlayerLine(true);
        InGameObjectManager.Instance.DrawPlayerLine(false);
    }

    public override void StateStart()
    {
        if (_specStage.effect_code_rule_tile.Length > 0)
        {
            Span<double> debuffStats = stackalloc double[_specStage.effect_code_rule_tile.Length + 1];
            debuffStats.Clear();

            debuffStats[0] = _specStage.effect_code_stat;
            for (int i = 0; i < _specStage.effect_code_rule_tile.Length; i++)
            {
                debuffStats[i + 1] = _specStage.effect_code_rule_tile[i];
            }

            var effectCodeID = new EffectCodeInfo((long) _specStage.effect_code_name, 0, debuffStats);
            InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
        }

        if (_specStage.effect_code_rule_tile_2.Length > 0)
        {
            Span<double> debuffStats = stackalloc double[_specStage.effect_code_rule_tile_2.Length + 1];
            debuffStats.Clear();

            debuffStats[0] = _specStage.effect_code_stat_2;
            for (int i = 0; i < _specStage.effect_code_rule_tile_2.Length; i++)
            {
                debuffStats[i + 1] = _specStage.effect_code_rule_tile_2[i];
            }

            var effectCodeID = new EffectCodeInfo((long) _specStage.effect_code_name_2, 0, debuffStats);
            InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
        }

        if (_specStage.effect_code_rule_tile_3.Length > 0)
        {
            Span<double> debuffStats = stackalloc double[_specStage.effect_code_rule_tile_3.Length + 1];
            debuffStats.Clear();

            debuffStats[0] = _specStage.effect_code_stat_3;
            for (int i = 0; i < _specStage.effect_code_rule_tile_3.Length; i++)
            {
                debuffStats[i + 1] = _specStage.effect_code_rule_tile_3[i];
            }

            var effectCodeID = new EffectCodeInfo((long) _specStage.effect_code_name_3, 0, debuffStats);
            InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
        }
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
