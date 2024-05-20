using CookApps.TeamBattle;
using CookApps.BattleSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookApps.AutoBattler
{
    public class InGameTileView : CachedMonoBehaviour
    {
        [SerializeField] private GameObject _activeObj;
        [SerializeField] private AllianceType _allianceType;
        public int ID { get; set; }
        public Vector3 Position => CachedTr.position;
        public AllianceType AllianceType => _allianceType;

        public void SetActiveObj(bool isActive)
        {
            _activeObj.SetActive(isActive);
        }
    }
}
