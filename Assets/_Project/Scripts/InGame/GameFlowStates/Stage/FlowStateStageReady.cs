using System;
using System.Collections.Generic;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

public class FlowStateStageReady : StateBase
{
    private SpecStage _specStage;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        _specStage = data as SpecStage;
    }

    public override async void StateInit(object target)
    {;
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<SpecStageMonster> monsters =
            SpecDataManager.Instance.GetStageMonsterList(_specStage.chapter_id, _specStage.stage_number,
                _specStage.difficulty_type);

        foreach (var monster in monsters)
        {
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

        // 그리드 설치
        foreach (var gridID in _specStage.obstacle_grid_id)
        {
            addCharacterTasks.Add(InGameObjectManager.Instance.AddObstacleToField(gridID, _specStage.chapter_id));
        }

        await UniTask.WhenAll(addCharacterTasks);

        // [TODO] 전판 배치 정보 저장 대응
        // foreach (var character in characters)
        // {
        //
        // }

        InGameMain.GetInGameMain().SetReadyUI();
    }

    public override void StateStart()
    {
        if (_specStage.chapter_rule_tile.Length > 0)
        {
            Span<double> debuffStats = stackalloc double[_specStage.chapter_rule_tile.Length];
            debuffStats.Clear();

            debuffStats[0] = _specStage.effect_code_stat;
            for (int i = 1; i < _specStage.chapter_rule_tile.Length; i++)
            {
                debuffStats[i] = _specStage.chapter_rule_tile[i];
            }
            var effectCodeID = new EffectCodeInfo((long)_specStage.effect_code_name, 0, debuffStats);
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
