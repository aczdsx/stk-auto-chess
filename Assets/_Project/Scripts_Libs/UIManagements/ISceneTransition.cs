using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.UIManagements
{
    public interface ISceneTransition
    {
        UniTask FadeInAsync();
        UniTask FadeOutAsync(bool withDelete);
    }
}
