using System;
using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
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
        [SerializeField] private TextMeshPro _debugHpText;

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
        [SerializeField]
        private BuffIconLayout _bottomLayout = new BuffIconLayout
        {
            yPosition = 0.1f,
            startX = -0.15f,
            horizontalSpacing = 0.1f,
            xOffsets = new float[] { -0.15f, -0.05f, 0.15f, 0.05f }
        };

        [Header("Top Layout (위쪽 버프 아이콘)")]
        [SerializeField]
        private BuffIconLayout _topLayout = new BuffIconLayout
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

        [Space]
        [SerializeField] private GameObject _buffSideBadgeParentObj;

        private SpriteRenderer _selectedFillLeft;
        private const float AnimationDuration = 0.4f; // 애니메이션 지속 시간
        private Vector2 _defalutSize;
        private Vector3 _defaultScale;

        // 풀에서 재사용 시 원래 size를 복원하기 위해 Awake에서 저장
        private Vector2 _originalPlayerSize;
        private Vector2 _originalEnemySize;
        private bool _isOriginalSizeInitialized;

        private List<InGameBuffDebuff> _buffDebuffList = new List<InGameBuffDebuff>();
        private List<InGameBuffDebuff> _sideBuffDebuffList = new List<InGameBuffDebuff>(); // Side 위치 버프 리스트

        private void Awake()
        {
            // 프리팹의 원래 size를 저장 (풀에서 재사용 시 복원용)
            if (!_isOriginalSizeInitialized)
            {
                _originalPlayerSize = _hpPlayerFillLeft.size;
                _originalEnemySize = _hpEnemyFillLeft.size;
                _isOriginalSizeInitialized = true;
            }
        }

        public void Initialize(CharacterStatData statData, AllianceType allianceType)
        {
            bool isPlayer = allianceType == AllianceType.Player;

            _hpPlayerFillLeft.gameObject.SetActive(isPlayer);
            _hpEnemyFillLeft.gameObject.SetActive(!isPlayer);

            _selectedFillLeft = isPlayer ? _hpPlayerFillLeft : _hpEnemyFillLeft;
            _hpFillSmoothGuage.color = isPlayer ? _playerSmoothColor : _enermySmoothColor;

            // 풀에서 재사용 시 원래 size 사용 (Awake에서 저장된 값)
            _defalutSize = isPlayer ? _originalPlayerSize : _originalEnemySize;
            _defaultScale = _selectedFillLeft.transform.localScale;

            // HP 바 초기화 (풀에서 재사용 시 원래 값으로 리셋)
            _selectedFillLeft.size = _defalutSize;
            _hpFillSmoothGuage.size = new Vector2(_defalutSize.x, _hpFillSmoothGuage.size.y);
            _shieldFiilLeft.size = new Vector2(0, _shieldFiilLeft.size.y);

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
        /// 시너지 아이콘 반짝임 효과 재생 (속성 시너지)
        /// </summary>
        public void PlayElementSynergyEffect()
        {
            if (_elementSynergySprite == null) return;
            PlaySynergyShinyEffect(_elementSynergySprite);
        }

        /// <summary>
        /// 시너지 아이콘 반짝임 효과 재생 (성좌 시너지)
        /// </summary>
        public void PlayPositionSynergyEffect()
        {
            if (_positionSynergySprite == null) return;
            PlaySynergyShinyEffect(_positionSynergySprite);
        }

        private static readonly int ShinyProgressId = Shader.PropertyToID("_ShinyProgress");

        /// <summary>
        /// 시너지 아이콘 Shiny 효과 (사선으로 빛이 흐르는 효과 + 스케일 애니메이션)
        /// </summary>
        private void PlaySynergyShinyEffect(SpriteRenderer spriteRenderer)
        {
            const float duration = 1.2f;

            var material = spriteRenderer.material;
            if (material == null) return;

            var targetTransform = spriteRenderer.transform;

            // 기존 트윈 중지
            DOTween.Kill(material);
            DOTween.Kill(targetTransform);

            // Shiny Progress: -0.5 → 1.5 (화면 밖에서 시작해서 밖으로 나감)
            material.SetFloat(ShinyProgressId, -0.5f);
            material.DOFloat(1.5f, ShinyProgressId, duration).SetEase(Ease.Linear);

            // 스케일 애니메이션: 1.0 → 0.9 → 1.3 → 0.9 → 1.0 (1.5초 동안)
            targetTransform.localScale = new Vector3(0.17f, 0.17f, 0.17f);

            // 0.17f
            var sequence = DOTween.Sequence();
            sequence.Append(targetTransform.DOScale(0.155f, 0.14f).SetEase(Ease.InQuad));
            sequence.Append(targetTransform.DOScale(0.21f, 0.41f).SetEase(Ease.OutQuad));
            sequence.Append(targetTransform.DOScale(0.155f, 0.41f).SetEase(Ease.InOutQuad));
            sequence.Append(targetTransform.DOScale(0.17f, 0.14f).SetEase(Ease.OutQuad));
            sequence.SetTarget(targetTransform);
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
            _synergyObj.SetActive(type.HasFlag(HpBarType.Synergy));
            _buffObjParent.SetActive(type.HasFlag(HpBarType.Buff));
            _buffSideBadgeParentObj.SetActive(type.HasFlag(HpBarType.Buff));
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

            UpdateDebugHpText(currHP, maxHP);

            await AnimateHpBar(startRatio, targetRatio, AnimationDuration);
        }

        private void UpdateDebugHpText(double currHP, double maxHP)
        {
            if(_debugHpText == null) return;
            _debugHpText.text = $"{Math.Round(currHP)}";
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
            // Side 버프와 일반 버프 분리
            var (sideBuffs, normalBuffs) = SeparateBuffsByPosition(buffDebuffs);

            // Side 버프 처리
            RestructSideBuffs(sideBuffs);

            // 일반 버프 처리
            RestructNormalBuffs(normalBuffs);
        }

        /// <summary>
        /// 버프를 Side와 일반으로 분리합니다.
        /// </summary>
        private (List<(int, BuffStackData)> sideBuffs, List<(int, BuffStackData)> normalBuffs) SeparateBuffsByPosition(IReadOnlyList<(int, BuffStackData)> buffDebuffs)
        {
            List<(int, BuffStackData)> sideBuffs = new List<(int, BuffStackData)>();
            List<(int, BuffStackData)> normalBuffs = new List<(int, BuffStackData)>();

            for (int i = 0; i < buffDebuffs.Count; i++)
            {
                int codeID = buffDebuffs[i].Item1;
                if (codeID == 0)
                {
                    continue;
                }

                var buffData = buffDebuffs[i];
                if (buffData.Item2.showPosition == BuffStackData.BuffShowPosition.SIDE)
                {
                    sideBuffs.Add(buffData);
                }
                else
                {
                    normalBuffs.Add(buffData);
                }
            }

            return (sideBuffs, normalBuffs);
        }

        /// <summary>
        /// Side 버프 아이콘을 재구성합니다.
        /// </summary>
        private void RestructSideBuffs(List<(int, BuffStackData)> sideBuffs)
        {
            int sideBuffCount = sideBuffs.Count;

            // 아이콘 개수 조정
            AdjustIconCount(_sideBuffDebuffList, sideBuffCount, _buffSideBadgeParentObj.transform);

            // 아이콘 위치 설정 및 데이터 업데이트
            float sideSpacing = _bottomLayout.horizontalSpacing;
            for (int i = 0; i < sideBuffs.Count && i < _sideBuffDebuffList.Count; i++)
            {
                var inGameBuffDebuff = _sideBuffDebuffList[i];

                // Side 버프는 세로로 배치 (horizontalSpacing을 Y 간격으로 사용)
                inGameBuffDebuff.CachedTr.localPosition = new Vector3(0, i * sideSpacing, 0);
                inGameBuffDebuff.gameObject.SetActive(true);
                inGameBuffDebuff.Set(sideBuffs[i]);
            }
        }

        /// <summary>
        /// 일반 버프 아이콘을 재구성합니다.
        /// </summary>
        private void RestructNormalBuffs(List<(int, BuffStackData)> normalBuffs)
        {
            int validBuffCount = normalBuffs.Count;
            int requiredCount = Mathf.Min(validBuffCount, 8); // 최대 8개 (bottom 4개 + top 4개)

            // 아이콘 개수 조정
            AdjustIconCount(_buffDebuffList, requiredCount, _buffObjParent.transform);

            // 아이콘 위치 설정 및 데이터 업데이트
            for (int i = 0; i < normalBuffs.Count && i < _buffDebuffList.Count; i++)
            {
                var inGameBuffDebuff = _buffDebuffList[i];

                // 위치 설정
                Vector2 position = GetBuffIconPosition(i);
                inGameBuffDebuff.CachedTr.localPosition = new Vector3(position.x, position.y, inGameBuffDebuff.CachedTr.localPosition.z);

                inGameBuffDebuff.gameObject.SetActive(true);
                inGameBuffDebuff.Set(normalBuffs[i]);
            }
        }

        /// <summary>
        /// 버프 아이콘 리스트의 개수를 조정합니다 (필요시 생성/반환).
        /// </summary>
        private void AdjustIconCount(List<InGameBuffDebuff> iconList, int requiredCount, Transform parent)
        {
            // 사용하지 않는 아이콘 반환
            while (iconList.Count > requiredCount)
            {
                var buffIcon = iconList[iconList.Count - 1];
                iconList.RemoveAt(iconList.Count - 1);

                InGameBuffDebuffPool.Instance.Return(buffIcon);
            }

            // 부족한 아이콘 생성
            while (iconList.Count < requiredCount)
            {
                var buffIcon = InGameBuffDebuffPool.Instance.Get();
                if (buffIcon == null)
                    break;

                buffIcon.CachedTr.SetParent(parent, false);
                iconList.Add(buffIcon);
            }
        }

        public void RefreshCoolTimeBuffIcon(out bool isExpired)
        {
            isExpired = false;

            // 일반 버프 쿨타임 갱신
            foreach (var buffDebuff in _buffDebuffList)
            {
                if (buffDebuff.IsWorking == false)
                    continue;

                bool isBuffExpired = buffDebuff.RefreshCoolTime();

                if (!isExpired && isBuffExpired)
                    isExpired = true;
            }

            // Side 버프 쿨타임 갱신
            foreach (var buffDebuff in _sideBuffDebuffList)
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
            // 일반 버프 반환
            foreach (var buffDebuff in _buffDebuffList)
            {
                InGameBuffDebuffPool.Instance.Return(buffDebuff);
            }
            _buffDebuffList.Clear();

            // Side 버프 반환
            foreach (var buffDebuff in _sideBuffDebuffList)
            {
                InGameBuffDebuffPool.Instance.Return(buffDebuff);
            }
            _sideBuffDebuffList.Clear();
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
