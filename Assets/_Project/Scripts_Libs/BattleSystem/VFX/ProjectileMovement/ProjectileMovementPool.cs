using UnityEngine.Pool;

namespace CookApps.TeamBattle.BattleSystem
{
    public static class ProjectileMovementPool
    {
        private static LinkedPool<ProjectileMovementLinear> linearPool;
        private static LinkedPool<ProjectileMovementBezier> bezierPool;

        public static ProjectileMovementBase Create(ProjectileMovementType type)
        {
            linearPool ??= new LinkedPool<ProjectileMovementLinear>(
                ProjectileMovementLinear.Create,
                collectionCheck: false);

            bezierPool ??= new LinkedPool<ProjectileMovementBezier>(
                ProjectileMovementBezier.Create,
                collectionCheck: false);

            switch (type)
            {
                case ProjectileMovementType.Linear:
                    return linearPool.Get();
                case ProjectileMovementType.Bezier:
                    return bezierPool.Get();
                default:
                    return null;
            }
        }

        public static void Release(ProjectileMovementBase movement)
        {
            switch (movement)
            {
                case ProjectileMovementLinear linear:
                    linearPool.Release(linear);
                    break;
                case ProjectileMovementBezier bezier:
                    bezierPool.Release(bezier);
                    break;
            }
        }
    }
}
