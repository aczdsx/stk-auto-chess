using CookApps.Obfuscator;

namespace CookApps.TeamBattle.BattleSystem
{
    public class InGameEffectHitBase : InGameEffectWithParticleBase
    {
        protected override ObfuscatorFloat Duration => 0.75f;

        protected override bool IsAutoRemove => true;
    }
}
