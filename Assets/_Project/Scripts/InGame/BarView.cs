using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using Cysharp.Threading.Tasks;

namespace CookApps.SampleTeamBattle
{
    public class BarView : CachedMonoBehaviour, IHpBarView
    {
        public void SetHpValue(double currHp, double maxHp)
        {
            throw new System.NotImplementedException();
        }
    }

    public class InGameHpBarViewPool : IHpBarViewPool
    {
        public UniTask<IHpBarView> GetHpBar()
        {
            throw new System.NotImplementedException();
        }

        public void ReturnHpBar(IHpBarView hpBarView)
        {
            throw new System.NotImplementedException();
        }
    }
}
