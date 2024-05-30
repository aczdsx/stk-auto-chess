using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookApps.AutoBattler
{
    public class HpBarView : CachedMonoBehaviour
    {
        [SerializeField] private SpriteRenderer _hpBaseGauge;
        [SerializeField] private SpriteRenderer _hpFillSmoothGuage;
        [SerializeField] private SpriteRenderer _hpFillLeft;
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
                _hpFillLeft.color = _playerColor;
            else if (allianceType == AllianceType.Enemy)
                _hpFillLeft.color = _enemyColor;

            _defaultSize = _hpFillSmoothGuage.size;
        }

        public async void SetHpValue(double current, double max)
        {
            if (!CachedGo.activeSelf)
            {
                ShowHpBar();
            }

            float targetRatio = Mathf.Clamp01((float)(current / max));
            float startRatio = 1 - _hpFillSmoothGuage.material.GetFloat("_ClipUvRight");
            _hpFillLeft.material.SetFloat("_ClipUvRight", 1 - targetRatio);
            await AnimateHpBar(startRatio, targetRatio, AnimationDuration);
        }

        private async UniTask AnimateHpBar(float startRatio, float targetRatio, float duration)
        {
            if (_hpFillSmoothGuage == null)
            {
                return;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_hpFillSmoothGuage == null)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                float ratio = Mathf.Lerp(startRatio, targetRatio, elapsed / duration);
                float hitEffectBlend = Mathf.Clamp01(1 - Mathf.Abs(2 * ratio - 1));

                _hpFillSmoothGuage.material.SetFloat("_ClipUvRight", 1 - ratio);
                _hpFillSmoothGuage.material.SetFloat("_HitEffectBlend", hitEffectBlend);

                await UniTask.Yield();
            }

            if (_hpFillSmoothGuage != null)
            {
                _hpFillSmoothGuage.material.SetFloat("_ClipUvRight", 1 - targetRatio);
                _hpFillSmoothGuage.material.SetFloat("_HitEffectBlend", 0);
            }
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
            if (_hpBarViewPool != null)
                _hpBarViewPool.Return(hpBarView);
        }
    }
}
