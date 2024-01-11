using UnityEngine;

namespace CookApps.TeamBattle.BattleSystem
{
    public interface IFollowable
    {
        Vector3 GetPosition();
        int GetSortingLayerOrder();
    }
}
