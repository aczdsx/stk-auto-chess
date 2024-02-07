using CookApps.TeamBattle;
using CookApps.TeamBattle.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookApps.SampleTeamBattle
{
    public class HpBarView : CachedMonoBehaviour, IHpBarView
    {
        [SerializeField] private SpriteRenderer hpGauge;

        private Vector2 defaultSize;

        private void Awake()
        {
            defaultSize = hpGauge.size;
        }

        public void Initialize(ICharacterStatData target)
        {
            var statData = target as CharacterStatData;
            if (statData == null)
            {
                return;
            }

            hpGauge.size = defaultSize;
        }

        public void SetHpValue(double current, double max)
        {
            if (CachedGo.activeSelf == false)
            {
                ShowHpBar();
            }

            float ratio = Mathf.Max(0f, (float) (current / max));
            Vector2 size = defaultSize;
            size.x = defaultSize.x * ratio;
            hpGauge.size = size;

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

    public class InGameHpBarViewPool : IHpBarViewPool
    {
        private UnityPool<HpBarView> hpBarViewPool;

        public async UniTask InitializePool()
        {
            // TODO: load hp bar prefab from addressable
            // hpBarViewPool.Initialize(prefab);
        }

        public void ReleasePool()
        {
            hpBarViewPool.ClearPool();
            hpBarViewPool = null;
        }

        public IHpBarView GetHpBar()
        {
            return hpBarViewPool.Get(null);
        }

        public void ReturnHpBar(IHpBarView hpBarView)
        {
            hpBarViewPool.Return(hpBarView as HpBarView);
        }
    }
}
