using Naninovel;

namespace CookApps.AutoBattler
{
    [CommandAlias("end")]
    public class NaninovelEndCommand : Command
    {
        public override UniTask Execute(AsyncToken token = default)
        {
            var naninovelMain = NaninovelMain.GetNaninovelMain();
            naninovelMain.ExecuteEndAction();

            return UniTask.CompletedTask;
        }
    }
}