using CookApps.Obfuscator;
using CookApps.TeamBattle.Utility;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameEffectHitBasic : InGameEffectHitBase
    {
        protected override ObfuscatorFloat Duration => 1f;

        protected override void ReturnToPool()
        {
            UnityPool<InGameEffectHitBasic>.Instance.Return(this);
        }
    }
}
