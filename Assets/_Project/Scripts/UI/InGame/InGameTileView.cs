using CookApps.BattleSystem;
using CookApps.TeamBattle;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class InGameTileView : CachedMonoBehaviour
    {
        [SerializeField] private GameObject _activeObj;
        [SerializeField] private GameObject _attackActiveObj;
        [SerializeField] private GameObject _commanderSkillNavigateObj;
        [SerializeField] private AllianceType _allianceType;
        [SerializeField] private SpriteRenderer _boardSprite;
        public int ID { get; set; }
        public Vector3 Position => CachedTr.position;
        public AllianceType AllianceType => _allianceType;
        public bool IsAlphaBoard => _boardSprite.color.a == 0;

        public void SetActiveObj(bool isActive)
        {
            _activeObj.SetActive(isActive);
        }

        public void SetAttackActiveObj(bool isActive)
        {
            _attackActiveObj.SetActive(isActive);
        }

        public void SetNavigateObj(bool isActive)
        {
            _commanderSkillNavigateObj.SetActive(isActive);
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
