using System.Collections.Generic;
using System.Linq;
using CookApps.AutoBattler;
using CookApps.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.BattleSystem.CharacterController;

public class FlowStateStageLobbyCombat : StateBase
{
    private List<CharacterController> characters;

    public override void StateInit(object target)
    {
        characters = ListPool<CharacterController>.Get();
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
            charac.AddNextState<CharacterStateIdle>();
        }
    }

    public override void StateRunning(float dt)
    {
        // loop
    }

    public override void StateEnd(bool isForced)
    {
        ListPool<CharacterController>.Release(characters);
        characters = null;
    }
}
