using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace CookApps.AutoBattler
{
    public class HpBarView : CachedMonoBehaviour
    {
        [SerializeField] private SpriteRenderer _hpBaseGauge;
        [SerializeField] private SpriteRenderer _hpFillGuage;
        [SerializeField] private Color _playerColor;
        [SerializeField] private Color _enemyColor;

        private Vector2 _defaultSize;

        private void Awake()
        {
            _defaultSize = _hpBaseGauge.size;
        }

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

            _hpBaseGauge.size = _defaultSize;
        }

        public void SetHpValue(double current, double max)
        {
            if (CachedGo.activeSelf == false)
            {
                ShowHpBar();
            }

            float ratio = Mathf.Max(0f, (float) (current / max));
            Vector2 size = _defaultSize;
            size.x = _defaultSize.x * ratio;
            _hpBaseGauge.size = size;

            /*
            if (null != _text)
            {
                _text.text = ((int)current).ToString();
            }*/
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
