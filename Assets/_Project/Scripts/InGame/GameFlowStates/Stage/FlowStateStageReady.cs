using System;
using System.Collections.Generic;
using System.Resources;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

public class FlowStateStageReady : StateBase
{
    private SpecStage specStage;

    public override void SetStateData(object data)
    {
        base.SetStateData(data);
        specStage = data as SpecStage;
    }

    public override async void StateInit(object target)
    {;
        var addCharacterTasks = new List<UniTask<CharacterController>>();
        List<SpecStageMonster> monsters =
            SpecDataManager.Instance.GetStageMonsterList(specStage.chapter_id, specStage.stage_number,
                specStage.difficulty_type);

        foreach (var monster in monsters)
        {
            Debug.LogColor($"monster 추가 : {monster.monster_id}");
            var statData = new CharacterStatData(monster.monster_id, monster.monster_lv);

            string[] coordinates = monster.coordinate.Split(',');
            int x = int.Parse(coordinates[0]);
            int y = int.Parse(coordinates[1]);
            int2 coordinate = new int2(x, y);

            addCharacterTasks.Add(InGameObjectManager.Instance.AddCharacterToField(statData, coordinate, AllianceType.Enemy,
                typeof(CharacterStateReady)));
        }

        await UniTask.WhenAll(addCharacterTasks);

        InGameMain.GetInGameMain().SetReadyUI();
    }

    public override void StateStart()
    {
        //[TODO] 나중에 데이터로 뺄 부분
        Span<double> debuffStats = stackalloc double[1];
        debuffStats.Clear();
        debuffStats[0] = 5;
        var effectCodeID = new EffectCodeInfo((long)EffectCodeNameType.CHAPTER_FIRE, 0, debuffStats);
        InGameManager.Instance.EffectCodeContainer.AddOrMergeEffectCode(effectCodeID, null);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
