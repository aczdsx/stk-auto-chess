using System;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IConstDataSource
    {
        float CrowdControlSlowRate { get; }
        float EffectCodeUpdatePendingTime { get; }
        float EffectCodeCooltimePendingTime { get; }
        float MaxCooltime { get; }
        float RegenHPPendingTime { get; }
        float MinDamageRate { get; }
    }

    public static class ConstDataSource
    {
        private static IConstDataSource instance;

        public static IConstDataSource Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new NullReferenceException("ConstDataSource is not initialized yet.");
                }

                return instance;
            }
        }

        public static void Initialize(IConstDataSource instance)
        {
            ConstDataSource.instance = instance;
        }
    }
}
