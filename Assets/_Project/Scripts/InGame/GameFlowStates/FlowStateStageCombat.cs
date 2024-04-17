using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.BattleSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using CharacterController = CookApps.TeamBattle.BattleSystem.CharacterController;

public class FlowStateStageCombat : StateBase
{
    public override void StateInit(object target)
    {
    }

    public override void StateStart()
    {
        StartAsync().Forget();
    }

    private async UniTask StartAsync()
    {
        await UniTask.Delay(1000);
        List<CharacterController> characters = ListPool<CharacterController>.Get();
        InGameObjectManager.Instance.GetAllAliveCharacters(AllianceType.Player, characters);
        foreach (CharacterController charac in characters)
        {
            charac.IsForceIdle = false;
        }

        ListPool<CharacterController>.Release(characters);
    }

    public override void StateRunning(float dt)
    {
    }

    public override void StateEnd(bool isForced)
    {
    }
}
