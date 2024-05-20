using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

public class FlowStateStageReady : StateBase
{
    //[TODO] 캐릭터 배치 관련 로직 추가
    public override async void StateInit(object target)
    {
        CharacterStatData statData1 = new CharacterStatData(40101, 10);
        CharacterStatData statData2 = new CharacterStatData(30601, 10);
        CharacterStatData statData3 = new CharacterStatData(40402, 10);
        await UniTask.WhenAll(new[]
        {
            InGameObjectManager.Instance.AddCharacterToField(statData1, new int2(1, 1), AllianceType.Player,
                typeof(CharacterStateIdle)),
            InGameObjectManager.Instance.AddCharacterToField(statData2, new int2(2, 1), AllianceType.Player,
                typeof(CharacterStateIdle)),
            InGameObjectManager.Instance.AddCharacterToField(statData3, new int2(3, 1), AllianceType.Player,
                typeof(CharacterStateIdle)),
            InGameObjectManager.Instance.AddCharacterToField(statData2, new int2(3, 3), AllianceType.Enemy,
                typeof(CharacterStateIdle)),
            InGameObjectManager.Instance.AddCharacterToField(statData3, new int2(5, 3), AllianceType.Enemy,
                typeof(CharacterStateIdle))
        });
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
