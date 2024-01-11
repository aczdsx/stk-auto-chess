using System;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IHpBar : ICachedGameObject, ICachedTransform
    {
        void SetHpValue(double currHp, double maxHp);
    }

    public interface IHpBarPool
    {
        UniTask<IHpBar> GetHpBar();
        void ReturnHpBar(IHpBar hpBar);
    }

    public static class HpBarPool
    {
        private static IHpBarPool instance;

        public static IHpBarPool Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new NullReferenceException("HpBar is not initialized yet.");
                }

                return instance;
            }
        }

        public static void Initialize(IHpBarPool instance)
        {
            HpBarPool.instance = instance;
        }
    }
}
