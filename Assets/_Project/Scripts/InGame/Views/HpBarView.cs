using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class HpBarView : CachedMonoBehaviour
    {
        [SerializeField] private SpriteRenderer _hpBaseGauge;
        [SerializeField] private SpriteRenderer _hpFillGuage;
        [SerializeField] private Color _playerColor;
        [SerializeField] private Color _enemyColor;

        private Vector2 _defaultSize;

        public void Initialize(CharacterStatData statData, AllianceType allianceType)
        {
            if (statData == null)
            {
                return;
            }

            if (allianceType == AllianceType.Player)
                _hpFillGuage.color = _playerColor;
            else if (allianceType == AllianceType.Enemy)
                _hpFillGuage.color = _enemyColor;

            _defaultSize = _hpFillGuage.size;
        }

        public void SetHpValue(double current, double max)
        {
            if (!CachedGo.activeSelf)
            {
                ShowHpBar();
            }

            float ratio = Mathf.Clamp01((float)(current / max));
            Vector2 size = _defaultSize;
            size.x *= ratio;
            _hpFillGuage.size = size;
        }

        private void HideHpBar()
        {
            CachedGo.SetActive(false);
        }

        private void ShowHpBar()
        {
            CachedGo.SetActive(true);
        }
    }

    public class InGameHpBarViewPool : Singleton<InGameHpBarViewPool>
    {
        private UnityPool<HpBarView> _hpBarViewPool;
        private GameObject _instance;

        public void InitializePool(GameObject instance)
        {
            // TODO: load hp bar prefab from addressable
            _instance = instance;
            _hpBarViewPool = new UnityPool<HpBarView>();
            _hpBarViewPool.Initialize(_instance);
        }

        public void ReleasePool()
        {
            _hpBarViewPool.ClearPool();
            _hpBarViewPool = null;
        }

        public HpBarView GetHpBar()
        {
            return _hpBarViewPool.Get(null);
        }

        public void ReturnHpBar(HpBarView hpBarView)
        {
            _hpBarViewPool.Return(hpBarView);
        }
    }
}
