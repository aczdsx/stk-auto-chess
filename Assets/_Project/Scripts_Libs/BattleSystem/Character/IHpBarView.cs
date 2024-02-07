using System;
using Cysharp.Threading.Tasks;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IHpBarView : ICachedGameObject, ICachedTransform
    {
        void Initialize(ICharacterStatData statData);
        void SetHpValue(double currHp, double maxHp);
    }

    public interface IHpBarViewPool
    {
        IHpBarView GetHpBar();
        void ReturnHpBar(IHpBarView hpBarView);
    }

    public static class HpBarViewPool
    {
        private static IHpBarViewPool instance;

        public static IHpBarViewPool Instance
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

        public static void Initialize(IHpBarViewPool instance)
        {
            HpBarViewPool.instance = instance;
        }
    }
}
