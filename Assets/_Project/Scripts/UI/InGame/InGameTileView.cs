using CookApps.TeamBattle;
using CookApps.BattleSystem;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class InGameTileView : CachedMonoBehaviour, IInGameTileView
    {
        public Vector3 Position => CachedTr.position;
    }
}
