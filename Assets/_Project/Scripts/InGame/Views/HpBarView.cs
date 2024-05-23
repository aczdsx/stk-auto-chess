using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
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
        private const float AnimationDuration = 0.3f; // 애니메이션 지속 시간

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

        public async void SetHpValue(double current, double max)
        {
            if (!CachedGo.activeSelf)
            {
                ShowHpBar();
            }

            float targetRatio = Mathf.Clamp01((float)(current / max));
            float startRatio = 1 - _hpFillGuage.material.GetFloat("_ClipUvRight");
            await AnimateHpBar(startRatio, targetRatio, AnimationDuration);
        }

        private async UniTask AnimateHpBar(float startRatio, float targetRatio, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ratio = Mathf.Lerp(startRatio, targetRatio, elapsed / duration);
                float hitEffectBlend = Mathf.Clamp01(1 - Mathf.Abs(2 * ratio - 1)); // 슬라이스 효과를 만듭니다.

                _hpFillGuage.material.SetFloat("_ClipUvRight", 1 - ratio);
                _hpFillGuage.material.SetFloat("_HitEffectBlend", hitEffectBlend);

                await UniTask.Yield();
            }

            _hpFillGuage.material.SetFloat("_ClipUvRight", 1 - targetRatio);
            _hpFillGuage.material.SetFloat("_HitEffectBlend", 0); // 애니메이션이 끝난 후 히트 효과를 해제합니다.
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
