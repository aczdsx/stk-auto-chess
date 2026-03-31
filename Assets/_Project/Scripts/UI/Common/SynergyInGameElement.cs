using System.Collections.Generic;
using Coffee.UIEffects;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyInGameElement : CachedMonoBehaviour
    {
        [SerializeField] private Image _iconImage;              // RF_ImageSynergyIcon
        [SerializeField] private SpriteLoader _IconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _synergyNameText;

        [SerializeField] private GameObject _countMaxGameObject;

        [SerializeField] private GameObject _countParentGameObject;
        [SerializeField] private TextMeshProUGUI _countText;    // RF_TextSynergyCount

        [SerializeField] private Image _gradeGuageImage;        // RF_Image_SliderFill

        [SerializeField] private RectTransform _buttonRect;
        [SerializeField] private UIShiny _shiny;
        [SerializeField] private Slider _fillSlider;

        [Header("Synergy Visual")]
        [SerializeField] private Image _imageBgGradient;        // ImageBgGradient
        [SerializeField] private SimpleImageSwapper _bgGradientSwapper; // ImageBgGradient

        [SerializeField] private SimpleImageSwapper _sliderBGImgaeSwapper;
        [SerializeField] private SimpleImageSwapper _sliderFillImgaeSwapper;
        /// <summary>
        /// 시너지 업데이트 중 상승한 시너지 타입 수집용
        /// </summary>
        private static HashSet<SynergyType> _upgradedSynergyTypes = new();

        /// <summary>
        /// 시너지 타입별 이전 상태 저장 (count, grade)
        /// UI 슬롯이 재정렬되어도 정확한 비교를 위해 전역적으로 관리
        /// </summary>
        private static Dictionary<SynergyType, (int count, int grade)> _previousSynergyStates = new();


        private SynergyType _synergyType;
        private ISpecSynergyData _synergyData;
        private bool _isActive;
        private HashSet<int> _inBattleChampionIds;
        private const int MAX_GRADE = 2;
        private const float ShinyDuration = 2f;

        // 비활성 공통 색상
        private static readonly Color InactiveBgColor = HexColor("#464B53");
        private static readonly Color InactiveFillColor = HexColor("#464B53");
        private static readonly Color InactiveIconColor = HexColor("#484E56");
        private static readonly Color InactiveTextColor = HexColor("#484E56");
        
        private static readonly Color ActiveIconColor = HexColor("#EEF5FC");
        private static readonly Color ActiveTextColor = HexColor("#EEF5FC");

        /// <summary>
        /// 시너지 타입별 활성 색상 (BgGradient + SliderFill 공통)
        /// </summary>
        private static readonly Dictionary<SynergyType, Color> ActiveColors = new()
        {
            // Element
            { SynergyType.FIRE,      HexColor("#BF4A4A") },
            { SynergyType.WATER,     HexColor("#377EC1") },
            { SynergyType.EARTH,     HexColor("#8C6021") },
            { SynergyType.WIND,      HexColor("#4E9A53") },
            { SynergyType.LIGHTNING, HexColor("#9756BC") },
            // Constellation
            { SynergyType.NOBLESSE,      HexColor("#C99A3E") },
            { SynergyType.SUPERNOVA,     HexColor("#C266D3") },
            { SynergyType.TROUBLESHOOTER,HexColor("#CE3555") },
        };

        public void SetInBattleChampionIds(HashSet<int> ids) => _inBattleChampionIds = ids;

        //캐릭터 속성 시너지 세팅
        public void SetSynergy(SynergyType synergyType, int count, ISpecSynergyData data, ISpecSynergyData nextData,
            bool isActive = true)
        {
            _synergyType = synergyType;
            _synergyData = data;
            _isActive = isActive;
            _synergyNameText.text = LanguageManager.Instance.GetDefaultText(_synergyData.name_token);

            // 시너지 상승 시 Shiny 효과 재생 및 상승 타입 수집
            // 시너지 타입별 전역 상태와 비교 (UI 슬롯 재정렬에 영향받지 않음)
            if (_previousSynergyStates.TryGetValue(synergyType, out var prevState))
            {
                if (count > prevState.count || data.grade > prevState.grade)
                {
                    PlayShinyEffect(_shiny).Forget();
                    _upgradedSynergyTypes.Add(synergyType);
                }
            }

            // 현재 상태 저장
            _previousSynergyStates[synergyType] = (count, data.grade);

            int grade = data.grade;
            bool isMaxGrade = grade == MAX_GRADE;

            // 아이콘
            _IconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteNameInGameBadge(synergyType)).Forget();

            // 슬라이더 카테고리별 이미지 전환 (Element / Constellation)
            var categorySwapType = DistinguishSpecTypeHelper.IsElementSynergyType(synergyType)
                ? SimpleSwapType.Elemental
                : SimpleSwapType.Constellation;
            _sliderBGImgaeSwapper?.Swap(categorySwapType);
            _sliderFillImgaeSwapper?.Swap(categorySwapType);

            // 시너지 비주얼 업데이트
            ApplySynergyVisual(synergyType, isActive, isMaxGrade);

            // 달성 단계 슬라이더 (0, 1, 2)
            _fillSlider.value = isActive ? grade : 0;
            // 카운트
            _countMaxGameObject.SetActive(isMaxGrade);
            _countText.gameObject.SetActive(!isMaxGrade);
            _countText.text = $"{count}/{nextData.min_int}";
        }

        /// <summary>
        /// PDF 스펙에 따른 시너지 비주얼 적용.
        /// 비활성화 / 단계{n} / 최대 단계 3가지 상태로 분기.
        /// </summary>
        private void ApplySynergyVisual(SynergyType synergyType, bool isActive, bool isMaxGrade)
        {
            Color activeColor = ActiveColors.GetValueOrDefault(synergyType, InactiveBgColor);

            if (!isActive)
            {
                // 비활성화
                SetBgGradient(SimpleSwapType.Normal, InactiveBgColor);
                SetSliderFillColor(InactiveFillColor);
                SetIconColor(InactiveIconColor);
                SetCountTextColor(InactiveTextColor);
            }
            else if (isMaxGrade)
            {
                // 최대 단계
                SetBgGradient(SimpleSwapType.MAX, activeColor);
                SetSliderFillColor(activeColor);
                SetIconColor(ActiveIconColor);
                SetCountTextColor(ActiveTextColor);
            }
            else
            {
                // 단계 {n} (활성, 최대 아님)
                SetBgGradient(SimpleSwapType.Normal, activeColor);
                SetSliderFillColor(activeColor);
                SetIconColor(ActiveIconColor);
                SetCountTextColor(ActiveTextColor);
            }
        }

        private void SetBgGradient(SimpleSwapType swapType, Color color)
        {
            if (_bgGradientSwapper != null)
                _bgGradientSwapper.Swap(swapType);
            if (_imageBgGradient != null)
                _imageBgGradient.color = color;
        }

        private void SetSliderFillColor(Color color)
        {
            if (_gradeGuageImage != null)
                _gradeGuageImage.color = color;
        }

        private void SetIconColor(Color color)
        {
            if (_iconImage != null)
                _iconImage.color = color;
        }

        private void SetCountTextColor(Color color)
        {
            if (_countText != null)
                _countText.color = color;
        }

        /// <summary>
        /// 시너지 아이콘 클릭 시 미니 팝업 표시
        /// </summary>
        public void OnClickSynergy()
        {
            if (_synergyData == null || _synergyData.grade == 0) return;

            var param = new SynergyTooltipIngameMiniPopup.PopupParam(
                _synergyType, _synergyData, _buttonRect, _isActive, _inBattleChampionIds);
            SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipIngameMiniPopup>(param).Forget();
        }


        /// <summary>
        /// UIShiny 효과 재생 (켜기 → Play → duration 후 끄기) + 스케일 애니메이션
        /// </summary>
        private async UniTaskVoid PlayShinyEffect(UIShiny shiny)
        {
            if (shiny == null) return;

            shiny.effectPlayer.duration = ShinyDuration;
            shiny.Play(true);

            await UniTask.Delay((int)(ShinyDuration * 1000));
        }

        public void SetCountTextVisible(bool visible)
        {
            _countParentGameObject?.SetActive(visible);
        }


        /// <summary>
        /// 상승한 시너지 타입 수집 시작 (업데이트 전 호출)
        /// </summary>
        public static void BeginCollectUpgradedSynergies()
        {
            _upgradedSynergyTypes.Clear();
        }

        /// <summary>
        /// 상승한 시너지 타입 목록 반환 및 수집 종료
        /// </summary>
        public static HashSet<SynergyType> EndCollectUpgradedSynergies()
        {
            var result = new HashSet<SynergyType>(_upgradedSynergyTypes);
            _upgradedSynergyTypes.Clear();
            return result;
        }

        /// <summary>
        /// 시너지 상태 초기화 (인게임 시작 시 호출)
        /// </summary>
        public static void ClearPreviousSynergyStates()
        {
            _previousSynergyStates.Clear();
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }
    }
}
