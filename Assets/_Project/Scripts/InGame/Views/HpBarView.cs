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
        [SerializeField] private SpriteRenderer _hpFillSmoothGuage;
        [SerializeField] private SpriteRenderer _hpPlayerFillLeft;
        [SerializeField] private SpriteRenderer _hpEnemyFillLeft;
        [SerializeField] private Color _playerSmoothColor;
        [SerializeField] private Color _enermySmoothColor;

        private SpriteRenderer _selectedFillLeft;
        private const float AnimationDuration = 0.4f; // 애니메이션 지속 시간
        private Vector2 _defalutSize;

        public void Initialize(CharacterStatData statData, AllianceType allianceType)
        {
            if (statData == null)
            {
                return;
            }

            _hpPlayerFillLeft.gameObject.SetActive(allianceType == AllianceType.Player);
            _hpEnemyFillLeft.gameObject.SetActive(!_hpPlayerFillLeft.gameObject.activeSelf);
            _selectedFillLeft = (allianceType == AllianceType.Player) ? _hpPlayerFillLeft : _hpEnemyFillLeft;
            _hpFillSmoothGuage.color = (allianceType == AllianceType.Player) ? _playerSmoothColor : _enermySmoothColor;

            _defalutSize = _selectedFillLeft.size;
        }

        public async void SetHpValue(double current, double max)
        {
            if (!CachedGo.activeSelf)
            {
                ShowHpBar();
            }

            float targetRatio = Mathf.Clamp01((float)(current / max));
            float startRatio = _selectedFillLeft.size.x / _defalutSize.x;
            _selectedFillLeft.size = new Vector2(_defalutSize.x * targetRatio, _defalutSize.y);

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

                _hpFillSmoothGuage.size = new Vector2(_defalutSize.x * ratio, _defalutSize.y);
                _hpFillSmoothGuage.material.SetFloat("_HitEffectBlend", hitEffectBlend);

                await UniTask.Yield();
            }

            if (_hpFillSmoothGuage != null)
            {
                _hpFillSmoothGuage.size = new Vector2(0, _defalutSize.y);
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

        public void Initialize(GameObject instance)
        {
            // TODO: load hp bar prefab from addressable
            _instance = instance;
            _hpBarViewPool = new UnityPool<HpBarView>();
            _hpBarViewPool.Initialize(_instance);
        }

        public void Clear()
        {
            _hpBarViewPool.ClearPool();
            _hpBarViewPool = null;
        }

        public HpBarView Get()
        {
            return _hpBarViewPool.Get(null);
        }

        public void Return(HpBarView hpBarView)
        {
            _hpBarViewPool?.Return(hpBarView);
        }
    }
}
