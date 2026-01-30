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

            var naninovelMain = NaninovelMain.GetNaninovelMain();
            if (naninovelMain == null)
            {
                UnityEngine.Debug.LogError("NaninovelEndCommand: NaninovelMain을 찾을 수 없습니다!");
                return;
            }

            UnityEngine.Debug.Log("NaninovelEndCommand: ExecuteEndAction 호출");

            // @end 커맨드 완료 전에 다음 스크립트 재생을 await
            // 이렇게 해야 Naninovel 엔진이 종료 처리를 하지 않음
            await naninovelMain.ExecuteEndActionAsync();
        }
    }
}