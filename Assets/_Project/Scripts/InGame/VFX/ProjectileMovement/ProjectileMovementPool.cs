using UnityEngine.Pool;

namespace CookApps.BattleSystem
{
    public interface IProjectileMovementPool
    {
        ProjectileMovementBase Create(InGameEffectView effect);
        void Release(ProjectileMovementBase movement);
    }

    public class ProjectileMovementPool
    {
        private static IProjectileMovementPool instance;

        public static IProjectileMovementPool Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new System.NullReferenceException("ProjectileMovementPool is not initialized yet.");
                }

                return instance;
            }
        }

        public static void Initialize(IProjectileMovementPool instance)
        {
            ProjectileMovementPool.instance = instance;
        }
    }
}
