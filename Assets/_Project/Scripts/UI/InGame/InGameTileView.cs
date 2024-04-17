using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class InGameTileView : CachedMonoBehaviour, IInGameTileView
    {
        public Vector2 Position => CachedTr.position;
    }
}
