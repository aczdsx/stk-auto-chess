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
        [SerializeField] private GameObject _buffObjParent;
        
        [System.Serializable]
        public class BuffIconLayout
        {
            [Header("위치 설정")]
            [Range(-1f, 1f)]
            [Tooltip("Y 위치 (상하 위치)")]
            public float yPosition = 0.1f;
            
            [Header("간격 설정")]
            [Range(-1f, 1f)]
            [Tooltip("시작 X 위치 (첫 번째 아이콘의 위치)")]
            public float startX = -0.15f;
            
            [Range(0f, 0.5f)]
            [Tooltip("아이콘 사이의 좌우 간격")]
            public float horizontalSpacing = 0.1f;
            
            [Header("커스텀 위치 (간격 대신 직접 지정)")]
            [Tooltip("각 아이콘의 X 위치 (순서대로, 비어있으면 간격 기반으로 계산)")]
            public float[] xOffsets = new float[] { -0.15f, -0.05f, 0.15f, 0.05f };
        }

        [Header("Bottom Layout (아래쪽 버프 아이콘)")]
        [SerializeField] private BuffIconLayout _bottomLayout = new BuffIconLayout
        {
            yPosition = 0.1f,
            startX = -0.15f,
            horizontalSpacing = 0.1f,
            xOffsets = new float[] { -0.15f, -0.05f, 0.15f, 0.05f }
        };

        [Header("Top Layout (위쪽 버프 아이콘)")]
        [SerializeField] private BuffIconLayout _topLayout = new BuffIconLayout
        {
            yPosition = 0.19f,
            startX = -0.15f,
            horizontalSpacing = 0.1f,
            xOffsets = new float[] { -0.15f, -0.05f, 0.15f, 0.05f }
        };

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

        private List<InGameBuffDebuff> _buffDebuffList = new List<InGameBuffDebuff>();

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

        /// <summary>
        /// 버프 아이콘 위치를 가져옵니다. (0~3: bottom, 4~7: top)
        /// </summary>
        private Vector2 GetBuffIconPosition(int index)
        {
            BuffIconLayout layout;
            int layoutIndex;
            
            if (index < 4)
            {
                // Bottom layout
                layout = _bottomLayout;
                layoutIndex = index;
            }
            else
            {
                // Top layout
                layout = _topLayout;
                layoutIndex = index - 4;
            }

            float x;
            float y = layout.yPosition;

            // xOffsets가 설정되어 있고 해당 인덱스가 있으면 사용, 없으면 간격 기반으로 계산
            if (layout.xOffsets != null && layout.xOffsets.Length > layoutIndex)
            {
                x = layout.xOffsets[layoutIndex];
            }
            else
            {
                // 간격 기반 자동 계산
                x = layout.startX + (layoutIndex * layout.horizontalSpacing);
            }
            
            return new Vector2(x, y);
        }

        public void SetHpBarType(HpBarType type = HpBarType.None)
        {
            _hpBarObj.SetActive(type.HasFlag(HpBarType.HpBar));
            _buffObjParent.SetActive(type.HasFlag(HpBarType.Buff));
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
#region Buff Icon
        public void RestructBuffIcon(IReadOnlyList<(int, BuffStackData)> buffDebuffs)
        {
            // 유효한 버프 개수 계산 (codeID가 0이 아닌 것만)
            int validBuffCount = 0;
            for (int i = 0; i < buffDebuffs.Count; i++)
            {
                if (buffDebuffs[i].Item1 != 0)
                {
                    validBuffCount++;
                }
            }
            
            int requiredCount = Mathf.Min(validBuffCount, 8); // 최대 8개 (bottom 4개 + top 4개)
            
            // 사용하지 않는 아이콘 반환
            while (_buffDebuffList.Count > requiredCount)
            {
                var buffIcon = _buffDebuffList[_buffDebuffList.Count - 1];
                _buffDebuffList.RemoveAt(_buffDebuffList.Count - 1);
                InGameBuffDebuffPool.Instance.Return(buffIcon);
            }
            
            // 부족한 아이콘 생성
            while (_buffDebuffList.Count < requiredCount)
            {
                var buffIcon = InGameBuffDebuffPool.Instance.Get();
                if (buffIcon == null)
                    break;
                    
                buffIcon.CachedTr.SetParent(_buffObjParent.transform, false);
                _buffDebuffList.Add(buffIcon);
            }

            // 모든 아이콘 위치 설정 및 데이터 업데이트
            int buffIndex = 0;
            for (int i = 0; i < buffDebuffs.Count && buffIndex < _buffDebuffList.Count; i++)
            {
                int codeID = buffDebuffs[i].Item1;
                if (codeID == 0)
                {
                    continue; // codeID가 0이면 건너뛰기
                }

                var inGameBuffDebuff = _buffDebuffList[buffIndex];
                
                // 위치 설정
                Vector2 position = GetBuffIconPosition(buffIndex);
                inGameBuffDebuff.CachedTr.localPosition = new Vector3(position.x, position.y, inGameBuffDebuff.CachedTr.localPosition.z);
                
                inGameBuffDebuff.gameObject.SetActive(true);
                inGameBuffDebuff.Set(buffDebuffs[i]);
                
                buffIndex++;
            }
        }

        public void RefreshCoolTimeBuffIcon(out bool isExpired)
        {
            isExpired = false;

            foreach (var buffDebuff in _buffDebuffList)
            {
                if (buffDebuff.IsWorking == false)
                    continue;

                bool isBuffExpired = buffDebuff.RefreshCoolTime();

                if (!isExpired && isBuffExpired)
                    isExpired = true;
            }
        }

        public void OnPreReturn()
        {
            foreach (var buffDebuff in _buffDebuffList)
            {
                InGameBuffDebuffPool.Instance.Return(buffDebuff);
            }
            _buffDebuffList.Clear();
        }
        
#endregion
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
