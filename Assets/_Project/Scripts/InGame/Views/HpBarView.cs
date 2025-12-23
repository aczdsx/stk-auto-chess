using System;
using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle;
using CookApps.BattleSystem;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
        [SerializeField] private SpriteRenderer _shieldFiilLeft;
        [SerializeField] private Color _playerSmoothColor;
        [SerializeField] private Color _enermySmoothColor;
        [SerializeField] private SpriteRenderer _coolTimeGuage;
        [SerializeField] private SpriteRenderer _hpMarkGuage; // 눈금선용 SpriteRenderer

        [Space]
        [SerializeField] private GameObject _buffObj;
        [SerializeField] private List<InGameBuffDebuff> _buffDebuffs;

        [Space]
        [SerializeField] private GameObject _synergyObj;
        [SerializeField] private SpriteRenderer _elementSynergySprite;
        [SerializeField] private SpriteRenderer _positionSynergySprite;
        [SerializeField] private SpriteLoader _elementSynergySpriteLoader;
        [SerializeField] private SpriteLoader _positionSynergySpriteLoader;

        private SpriteRenderer _selectedFillLeft;
        private const float AnimationDuration = 0.4f; // 애니메이션 지속 시간
        private Vector2 _defalutSize;
        private Vector3 _defaultScale;

        public void Initialize(CharacterStatData statData, AllianceType allianceType)
        {
            bool isPlayer = allianceType == AllianceType.Player;

            _hpPlayerFillLeft.gameObject.SetActive(isPlayer);
            _hpEnemyFillLeft.gameObject.SetActive(!isPlayer);

            _selectedFillLeft = isPlayer ? _hpPlayerFillLeft : _hpEnemyFillLeft;
            _hpFillSmoothGuage.color = isPlayer ? _playerSmoothColor : _enermySmoothColor;

            _defalutSize = _selectedFillLeft.size;
            _defaultScale = _selectedFillLeft.transform.localScale;

            // 눈금선 초기화
            if (_hpMarkGuage != null)
            {
                _hpMarkGuage.size = _defalutSize;
                if (_hpMarkGuage.material != null && statData != null)
                {
                    _hpMarkGuage.material.SetFloat("_MaxHP", (float)statData.HP);
                }
            }

            if (statData != null)
            {
                _elementSynergySpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(statData.Spec.character_element_type)).Forget();
                _positionSynergySpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(statData.Spec.character_stella_type)).Forget();
            }
        }

        public void SetHpBarType(HpBarType type = HpBarType.None)
        {
            _hpBarObj.SetActive(type.HasFlag(HpBarType.HpBar));
            _buffObj.SetActive(type.HasFlag(HpBarType.Buff));
            _synergyObj.SetActive(type.HasFlag(HpBarType.Synergy));
        }

        public async void SetValue(double currHP, double maxHP, double currShield)
        {
            if (!CachedGo.activeSelf)
            {
                ShowHpBar();
            }

            float startRatio = _selectedFillLeft.size.x / _defalutSize.x;
            float targetRatio = (currHP + currShield < maxHP) ? Mathf.Clamp01((float)(currHP / maxHP)) : Mathf.Clamp01((float)(currHP / (maxHP + currShield)));
            float shieldRatio = (currHP + currShield < maxHP) ? Mathf.Clamp01((float)(currShield / maxHP)) : Mathf.Clamp01((float)(currShield / (maxHP + currShield)));

            float defaultX = _defalutSize.x * targetRatio;
            if (!float.IsNaN(defaultX))
            {
                _selectedFillLeft.size = new Vector2(_defalutSize.x * targetRatio, _selectedFillLeft.size.y);
                _shieldFiilLeft.size = new Vector2(_defalutSize.x * shieldRatio, _shieldFiilLeft.size.y);

                // 쉴드바 위치를 HP바 끝에 놓일 수 있도록 계산
                _shieldFiilLeft.transform.localPosition = _selectedFillLeft.transform.localPosition
                                                          + Vector3.right * (_defaultScale.x * (_selectedFillLeft.size.x + _shieldFiilLeft.size.x));
            }

            // 눈금선 shader에 체력 값 전달
            if (_hpMarkGuage != null && _hpMarkGuage.material != null)
            {
                _hpMarkGuage.material.SetFloat("_CurrentHP", (float)currHP);
                _hpMarkGuage.material.SetFloat("_MaxHP", (float)maxHP);
            }

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

                float defaultX = _defalutSize.x * ratio;
                if (!float.IsNaN(defaultX))
                    _hpFillSmoothGuage.size = new Vector2(defaultX, _hpFillSmoothGuage.size.y);

                // _hpFillSmoothGuage.material.SetFloat("_HitEffectBlend", hitEffectBlend);

                await UniTask.Yield();
            }

            if (_hpFillSmoothGuage != null)
            {
                _hpFillSmoothGuage.size = new Vector2(0, _hpFillSmoothGuage.size.y);
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
            if (max == 0)
            {
                Debug.LogError("Max cannot be zero");
                return;
            }

            float targetRatio = Mathf.Clamp01((float)(current / max));
            _coolTimeGuage.size = new Vector2(_defalutSize.x * targetRatio, _coolTimeGuage.size.y);
        }

        public void RestructBuffIcon(IReadOnlyList<(int, BuffStackData)> buffDebuffs)
        {
            // 최상위 3개 버프 아이콘만 보여준다.
            for (int i = 0; i < 3; i++)
            {
                var inGameBuffDebuff = _buffDebuffs[i];
                inGameBuffDebuff.gameObject.SetActive(false);

                if (i < buffDebuffs.Count)
                {
                    int codeID = buffDebuffs[i].Item1;
                    if (codeID == 0)
                        continue;

                    inGameBuffDebuff.gameObject.SetActive(true);
                    inGameBuffDebuff.Set(buffDebuffs[i]);
                }
            }
        }

        public void RefreshCoolTimeBuffIcon(out bool isExpired)
        {
            isExpired = false;

            foreach (var buffDebuff in _buffDebuffs)
            {
                if (buffDebuff.IsWorking == false)
                    continue;

                bool isBuffExpired = buffDebuff.RefreshCoolTime();

                if (!isExpired && isBuffExpired)
                    isExpired = true;
            }
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
