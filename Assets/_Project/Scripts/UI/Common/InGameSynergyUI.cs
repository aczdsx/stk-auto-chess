using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using LitMotion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class InGameSynergyUI : CachedMonoBehaviour
    {
        /// <summary>
        /// 시너지 업데이트 중 상승한 시너지 타입 수집용
        /// </summary>
        private static HashSet<SynergyType> _upgradedSynergyTypes = new HashSet<SynergyType>();

        /// <summary>
        /// 시너지 타입별 이전 상태 저장 (count, grade)
        /// UI 슬롯이 재정렬되어도 정확한 비교를 위해 전역적으로 관리
        /// </summary>
        private static Dictionary<SynergyType, (int count, int grade)> _previousSynergyStates = new Dictionary<SynergyType, (int count, int grade)>();

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

        [SerializeField] private Image _iconImage;
        [SerializeField] private SpriteLoader _starAsterismIconSpriteLoader;
        [SerializeField] private SpriteLoader _elementalIconSpriteLoader;
        [SerializeField] private GameObject _starAsterismIconGameObject;
        [SerializeField] private GameObject _elementalIconGameObject;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _countMaxGameObject;
        [SerializeField] private GameObject _countParentGameObject;

        [SerializeField] private Image _starAsterismGradeGuageImage;
        [SerializeField] private Image _starAsterismGradeGuageColor;
        [SerializeField] private Image _elementalGradeGuageImage;
        [SerializeField] private Image _elementalGradeGuageColor;
        [SerializeField] private RectTransform _buttonRect;
        [SerializeField] private UIShiny _starAstarismShiny;
        [SerializeField] private UIShiny _elementalShiny;



        private Color _step0Color = new Color32(139, 139, 139, 50); // 그레이 (Gray)
        private Color _step1Color = new Color32(205, 127, 50, 255); // 동 (Bronze)
        private Color _step2Color = new Color32(230, 230, 230, 255); // 은 (Silver)
        private Color _step3Color = new Color32(255, 215, 0, 255); // 금 (Gold)
        private Color _step4Color = new Color32(229, 228, 226, 255); // 플래티넘 (Platinum) 

        private SynergyType _synergyType;
        private int _count;
        private ISpecSynergyData _synergyData;
        private ISpecSynergyData _nextSynergyData;
        private bool _isActive;
        private const int MAX_GRADE = 3;

        //캐릭터 속성 시너지 세팅
        public void SetSynergy(SynergyType synergyType, int count, ISpecSynergyData data, ISpecSynergyData nextData, bool isActive = true, bool isColorWhite = false)
        {
            _synergyType = synergyType;
            _synergyData = data;
            _nextSynergyData = nextData;
            _isActive = isActive;

            // 시너지 상승 시 Shiny 효과 재생 및 상승 타입 수집
            // 시너지 타입별 전역 상태와 비교 (UI 슬롯 재정렬에 영향받지 않음)
            if (_previousSynergyStates.TryGetValue(synergyType, out var prevState))
            {
                if (count > prevState.count || data.grade > prevState.grade)
                {
                    bool isAsterism = DistinguishSynergyTypeHelper.IsAsterismSynergyType(synergyType);
                    PlayShinyEffect(isAsterism ? _starAstarismShiny : _elementalShiny).Forget();
                    _upgradedSynergyTypes.Add(synergyType);
                }
            }

            // 현재 상태 저장
            _previousSynergyStates[synergyType] = (count, data.grade);

            Color color = Color.white;
            switch (data.grade)
            {
                case 0:
                    color = _step0Color;
                    break;
                case 1:
                    color = _step1Color;
                    break;
                case 2:
                    color = _step2Color;
                    break;
                case 3:
                    color = _step3Color;
                    break;
                case 4:
                    color = _step4Color;
                    break;
                default:
                    color = Color.white;
                    break;
            }

            _count = count;


            bool isAsterismSynergyType = DistinguishSynergyTypeHelper.IsAsterismSynergyType(synergyType);
            _starAsterismIconGameObject.SetActive(isAsterismSynergyType);
            _elementalIconGameObject.SetActive(!isAsterismSynergyType);
            if (isAsterismSynergyType)
            {
                _starAsterismIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(synergyType, isActive)).Forget();
            }
            else
            {
                _elementalIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(synergyType, isActive)).Forget();
            }

            if (isAsterismSynergyType)
            {
                if (data.grade == 1 && !isActive)
                {
                    _starAsterismGradeGuageImage.fillAmount = 0f;
                }
                else if (data.grade == MAX_GRADE)
                {
                    _starAsterismGradeGuageImage.fillAmount = 1f;
                }
                else
                {
                    _starAsterismGradeGuageImage.fillAmount = (float)data.grade / (float)(MAX_GRADE);
                }
                _starAsterismGradeGuageColor.color = color;
            }
            else
            {
                if (data.grade == 1 && !isActive)
                {
                    _elementalGradeGuageImage.fillAmount = 0f;
                }
                else if (data.grade == MAX_GRADE)
                {
                    _elementalGradeGuageImage.fillAmount = 1f;
                }
                else
                {
                    _elementalGradeGuageImage.fillAmount = (float)data.grade / (float)(MAX_GRADE);
                }
                _elementalGradeGuageColor.color = color;
            }

            if (data.grade == MAX_GRADE)
            {
                _countMaxGameObject.SetActive(true);
                _countText.gameObject.SetActive(false);
            }
            else
            {
                _countMaxGameObject.SetActive(false);
                _countText.gameObject.SetActive(true);
            }
            _countText.text = $"{count}/{nextData.min_int}";
            if (isAsterismSynergyType)
            {
                // Debug.LogColor($"SynergyUI!! [{synergyType}] {isActive}/{_starAsterismGradeGuageImage.fillAmount}", "green");
            }
            else
            {
                // Debug.LogColor($"SynergyUI!! [{synergyType}] {isActive}/{_elementalGradeGuageImage.fillAmount}", "green");
            }
        }

        /// <summary>
        /// 시너지 아이콘 클릭 시 미니 팝업 표시
        /// </summary>
        public void OnClickSynergy()
        {
            if (_synergyData == null || _synergyData.grade == 0) return;

            var param = new SynergyTooltipIngameMiniPopup.PopupParam(
                _synergyType,
                _synergyData,
                _buttonRect,
                _isActive
            );

            SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipIngameMiniPopup>(param).Forget();
        }

        private const float ShinyDuration = 2f;

        /// <summary>
        /// UIShiny 효과 재생 (켜기 → Play → duration 후 끄기) + 스케일 애니메이션
        /// </summary>
        private async UniTaskVoid PlayShinyEffect(UIShiny shiny)
        {
            if (shiny == null) return;

            var targetTransform = shiny.transform;

            // 기존 트윈 중지 (현재 활성 트윈 없음)

            shiny.effectPlayer.duration = ShinyDuration;
            shiny.Play(true);

            // 스케일 애니메이션: 1.0 → 0.9 → 1.3 → 0.9 → 1.0 (2초 동안)
            // targetTransform.localScale = Vector3.one;

            // var sequence = DOTween.Sequence();
            // sequence.Append(targetTransform.DOScale(0.9f, 0.1f).SetEase(Ease.InQuad));
            // sequence.Append(targetTransform.DOScale(1.3f, 0.4f).SetEase(Ease.OutQuad));
            // sequence.Append(targetTransform.DOScale(0.9f, 0.4f).SetEase(Ease.InOutQuad));
            // sequence.Append(targetTransform.DOScale(1.0f, 0.1f).SetEase(Ease.OutQuad));
            // sequence.SetTarget(targetTransform);

            await UniTask.Delay((int)(ShinyDuration * 1000));
        }

        public void SetCountTextVisible(bool visible)
        {
            _countParentGameObject?.SetActive(visible);
        }
    }
}
