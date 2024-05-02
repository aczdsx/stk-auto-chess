using UnityEngine;

namespace CookApps.BattleSystem
{
    public interface IFollowable
    {
        Vector3 GetPosition();
        int GetSortingLayerOrder();
    }
}
