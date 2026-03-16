using CookApps.AutoBattler;

namespace CookApps.AutoChess
{
    /// <summary>
    /// SynergyType + Tier → SynergyBehaviorBase 인스턴스 생성.
    /// Asterism 시너지 구현 시 여기에 케이스 추가.
    /// </summary>
    public static class SynergyBehaviorFactory
    {
        public static SynergyBehaviorBase Create(SynergyType type, byte tier, int traitId, byte teamIndex)
        {
            SynergyBehaviorBase behavior = type switch
            {
                // === asterism 시너지 구현 시 여기에 추가 ===
                // SynergyType.NOBLESSE => new SynergyBehaviorNoblesse(),
                // SynergyType.TROUBLESHOOTER => new SynergyBehaviorTroubleShooter(),
                // SynergyType.SUPERNOVA => new SynergyBehaviorSupernova(),
                _ => null,
            };

            if (behavior != null)
            {
                behavior.TraitId = traitId;
                behavior.Tier = tier;
                behavior.TeamIndex = teamIndex;
            }

            return behavior;
        }

        /// <summary>해당 SynergyType이 행동 클래스를 필요로 하는지</summary>
        public static bool NeedsBehavior(SynergyType type)
        {
            return type switch
            {
                // 원소(1-6): 스탯만, 행동 없음
                SynergyType.NORMAL or
                SynergyType.FIRE or
                SynergyType.WIND or
                SynergyType.LIGHTNING or
                SynergyType.EARTH or
                SynergyType.WATER => false,
                // asterism(7+): 행동 필요
                _ => true,
            };
        }
    }
}
