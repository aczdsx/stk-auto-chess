using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class ElpisCoreResearchLayer : UILayer
    {
        public enum CoreResearchType
        {
            KnightAttack,
            KnightDefense,
            KnightHealth,
            Fire,
            Wind,
            Earth,
            Lightning,
            Water,
            Noblesse,
            Supernova,
            Troubleshooter,
        }
        
        public SerializableDictionary<CoreResearchType, Color> iconColors;
        public SerializableDictionary<CoreResearchType, Gradient> IconGradients;
    }
}
