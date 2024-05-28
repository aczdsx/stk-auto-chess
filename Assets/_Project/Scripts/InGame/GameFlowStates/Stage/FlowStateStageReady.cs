using System.Collections.Generic;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

public class FlowStateStageReady : StateBase
{
    //[TODO] 캐릭터 배치 관련 로직 추가
    public override async void StateInit(object target)
    {
        List<CharacterStatData> characterStats = new List<CharacterStatData>();
        characterStats.Add(new CharacterStatData(40101, 10));
        characterStats.Add(new CharacterStatData(30601, 10));
        characterStats.Add(new CharacterStatData(40402, 10));
        await UniTask.WhenAll(new[]
        {
            InGameObjectManager.Instance.AddCharacterToField(characterStats[0], new int2(3, 3), AllianceType.Enemy,
                typeof(CharacterStateIdle)),
            InGameObjectManager.Instance.AddCharacterToField(characterStats[1], new int2(5, 4), AllianceType.Enemy,
                typeof(CharacterStateIdle))
        });

        InGameMain.GetInGameMain().SetReadyUI(characterStats);
    }

    public override void StateStart()
    {
        // 캐릭터 배치
        // UI에서 Start 버튼을 눌렀을 때 Start로 이동
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
