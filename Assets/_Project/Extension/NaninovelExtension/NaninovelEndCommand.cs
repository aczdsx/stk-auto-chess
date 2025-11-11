using Naninovel;
using Spine.Unity;
using System;
using UnityEngine;

public class NaninovelEndCommand : Naninovel.Command
{
    public override UniTask Execute(AsyncToken token = default(AsyncToken))
    {
        // var runScriptName = DataManager.TestDialogueScriptName; // ToDo:나중에는 스크립트 인덱스로 처리하도록 변경 예정 choiJE.230523

        // UnityEngine.Debug.LogWarning("End Script");

        // if (!string.IsNullOrEmpty(runScriptName))
        // {
        //     DataManager.Instance.EndNaninovel(runScriptName);
        // }

        // AppEventManager.Instance.SendDialoguePass(runScriptName, true,SceneDialog.isAuto,SceneDialog.isSkip);

        return UniTask.CompletedTask;
    }
}