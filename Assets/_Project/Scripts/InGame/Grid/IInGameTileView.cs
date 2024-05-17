using UnityEngine;

namespace CookApps.BattleSystem
{
    public interface IInGameTileView
    {
        public int ID { get; set; }
        Vector3 Position { get; }
    }
}
