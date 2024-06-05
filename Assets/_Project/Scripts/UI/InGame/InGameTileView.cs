using CookApps.TeamBattle;
using CookApps.BattleSystem;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D;

namespace CookApps.AutoBattler
{
    public class InGameTileView : CachedMonoBehaviour
    {
        [SerializeField] private GameObject _activeObj;
        [SerializeField] private AllianceType _allianceType;
        [SerializeField] private SpriteRenderer _boardSprite;
        public int ID { get; set; }
        public Vector3 Position => CachedTr.position;
        public AllianceType AllianceType => _allianceType;

        public void SetActiveObj(bool isActive)
        {
            _activeObj.SetActive(isActive);
        }

        public void ChangeColor(Color color)
        {
            _boardSprite.color = color;
        }

        public Color GetColor()
        {
            return _boardSprite.color;
        }
    }
}
