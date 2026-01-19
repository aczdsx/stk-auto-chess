using CookApps.TeamBattle.UIManagements;
using Naninovel;

namespace CookApps.AutoBattler
{
    [CommandAlias("end")]
    public class NaninovelEndCommand : Command
    {
        public override async UniTask Execute(AsyncToken token = default)
        {
            UnityEngine.Debug.Log("NaninovelEndCommand: @end 커맨드 실행됨");

            // 먼저 FadeIn 시작 (화면 가림)
            SceneTransition.Create<SceneTransition_SubTransition>(SubTransition_Animator.Address);
            await SceneTransition.FadeInAsync();

            var naninovelMain = NaninovelMain.GetNaninovelMain();
            if (naninovelMain == null)
            {
                UnityEngine.Debug.LogError("NaninovelEndCommand: NaninovelMain을 찾을 수 없습니다!");
                return;
            }

            UnityEngine.Debug.Log("NaninovelEndCommand: ExecuteEndAction 호출");
            naninovelMain.ExecuteEndAction();
        }
    }
}