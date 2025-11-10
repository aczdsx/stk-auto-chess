// Copyright 2022 ReWaffle LLC. All rights reserved.


using UnityEngine;

namespace Naninovel.Commands
{
    /// <summary>
    /// Stops the naninovel script execution.
    /// </summary>
    public class Stop : Command
    {
        public override UniTask ExecuteAsync (AsyncToken asyncToken = default)
        {
            Time.timeScale = 1f; // choiJE.231207
            Engine.GetService<IScriptPlayer>().Stop();

            return UniTask.CompletedTask;
        }
    } 
}
