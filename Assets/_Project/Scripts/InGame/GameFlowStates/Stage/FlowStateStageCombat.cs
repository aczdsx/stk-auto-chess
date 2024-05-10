using System.Collections.Generic;
using System.Linq;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageCombat : StateBase
{
    private List<CharacterController> characters;

    public override void StateInit(object target)
    {
        characters = ListPool<CharacterController>.Get();
        characters.Add((CharacterController)target);
    }

    public override void StateStart()
    {
        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        // 전투 시작 후 1초는 대기
        await UniTask.Delay(1000);

        // 모든 캐릭터 락 해제
        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            charac.IsForceIdle = false;
        }
        characters.Clear();
        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Enemy, characters);
        foreach (CharacterController charac in characters)
        {
            charac.IsForceIdle = false;
        }
        characters.Clear();
    }

    public override void StateRunning(float dt)
    {
        // 전투가 끝났는지 확인
        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Player, characters);
        if (characters.Count == 0)
        {
            // 패배!
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageWaveFail>();
        }

        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Enemy, characters);
        if (characters.Count == 0)
        {
            // 승리!
            InGameMainFlowManager.Instance.AddNextState<FlowStateStageWaveClear>();
        }
    }

    public override void StateEnd(bool isForced)
    {
    }
}
