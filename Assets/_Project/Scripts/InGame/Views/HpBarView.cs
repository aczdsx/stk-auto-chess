using System;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace CookApps.AutoBattler
{
    [Flags]
    public enum HpBarType
    {
        None = 0,
        HpBar = 1 << 0,
        Buff = 1 << 1,
        Synergy = 1 << 2,
    }

    public class HpBarView : CachedMonoBehaviour
    {
        [Space]
        [SerializeField] private GameObject _hpBarObj;
        [SerializeField] private SpriteRenderer _hpFillSmoothGuage;
        [SerializeField] private SpriteRenderer _hpPlayerFillLeft;
        [SerializeField] private SpriteRenderer _hpEnemyFillLeft;
        [SerializeField] private Color _playerSmoothColor;
        [SerializeField] private Color _enermySmoothColor;
        [SerializeField] private SpriteRenderer _coolTimeGuage;

        [Space]
        [SerializeField] private GameObject _buffObj;
        [SerializeField] private List<InGameBuffDebuff> _buffDebuffList;

        [Space]
        [SerializeField] private GameObject _synergyObj;
        [SerializeField] private SpriteRenderer _elementSynergySprite;
        [SerializeField] private SpriteRenderer _positionSynergySprite;

        private SpriteRenderer _selectedFillLeft;
        private const float AnimationDuration = 0.4f; // 애니메이션 지속 시간
        private Vector2 _defalutSize;

        public void Initialize(CharacterStatData statData, AllianceType allianceType)
        {
            if (statData == null)
            {
                return;
            }

            bool isPlayer = allianceType == AllianceType.Player;

            _hpPlayerFillLeft.gameObject.SetActive(isPlayer);
            _hpEnemyFillLeft.gameObject.SetActive(!isPlayer);

            _selectedFillLeft = isPlayer ? _hpPlayerFillLeft : _hpEnemyFillLeft;
            _hpFillSmoothGuage.color = isPlayer ? _playerSmoothColor : _enermySmoothColor;

            _defalutSize = _selectedFillLeft.size;

            _elementSynergySprite.sprite = ImageManager.Instance.GetSynergySprite(statData.Spec.element_type);
            _positionSynergySprite.sprite = ImageManager.Instance.GetPositionSprite(statData.Spec.character_position_type);
        }

        public void SetHpBarType(HpBarType type = HpBarType.None)
        {
            _hpBarObj.SetActive(type.HasFlag(HpBarType.HpBar));
            _buffObj.SetActive(type.HasFlag(HpBarType.Buff));
            _synergyObj.SetActive(type.HasFlag(HpBarType.Synergy));
        }

        public async void SetHpValue(double current, double max)
        {
            if (!CachedGo.activeSelf)
            {
                ShowHpBar();
            }

            float targetRatio = Mathf.Clamp01((float)(current / max));
            float startRatio = _selectedFillLeft.size.x / _defalutSize.x;


            float defaultX = _defalutSize.x * targetRatio;
            if (!float.IsNaN(defaultX))
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
                // float hitEffectBlend = Mathf.Clamp01(1 - Mathf.Abs(2 * ratio - 1));

                float defaultX = _defalutSize.x* ratio;
                if (!float.IsNaN(defaultX))
                    _hpFillSmoothGuage.size = new Vector2(defaultX, _defalutSize.y);

                // _hpFillSmoothGuage.material.SetFloat("_HitEffectBlend", hitEffectBlend);

                await UniTask.Yield();
            }

            if (_hpFillSmoothGuage != null)
            {
                _hpFillSmoothGuage.size = new Vector2(0, _defalutSize.y);
                // _hpFillSmoothGuage.material.SetFloat("_HitEffectBlend", 0);
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

        public void OnCoolTimeUpdated(int index, float current, float max)
        {
            float targetRatio = Mathf.Clamp01((float)(current / max));
            _coolTimeGuage.size = new Vector2(_defalutSize.x * targetRatio, _defalutSize.y);
        }

        public void AddBuffIcon(long codeID)
        {
            // [TODO] buff, debuff icon 고민 좀만 더...
            // EffectCodeType type = (EffectCodeType)codeID;
            // _buffDebuffList.ForEach(renderer =>
            // {
            //     if (renderer.sprite == null)
            //     {
            //         renderer.sprite = ImageManager.Instance.GetBuffDebuffSprite(type.ToString());
            //         renderer.gameObject.SetActive(true);
            //         return;
            //     }
            // });
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
