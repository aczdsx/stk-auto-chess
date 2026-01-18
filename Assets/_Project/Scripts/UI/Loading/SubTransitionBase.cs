using CookApps.TeamBattle;
using Cysharp.Threading.Tasks;

namespace CookApps.AutoBattler
{
    public abstract class SubTransitionBase : CachedMonoBehaviour
    {
        public abstract UniTask FadeInAsync();
        public abstract UniTask FadeOutAsync();
    }
}
