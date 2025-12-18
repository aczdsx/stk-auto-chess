using Naninovel;
using Naninovel.Commands;
using Spine.Unity;
using System;
using UnityEngine;
using CookApps.TeamBattle.UIManagements;

namespace CookApps.AutoBattler
{
    [CommandAlias("end")]
    public class NaninovelEndCommand : Command
    {
        public override UniTask Execute(AsyncToken token = default)
        {
            SceneLoading.GoToNextScene("InGame",
                    (InGameType.PROLOGUE, (IGameStateUICore)new InGameMainStatePrologue(), 0));

            return UniTask.CompletedTask;
        }
    }
}