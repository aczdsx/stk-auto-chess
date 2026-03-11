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
    public class InGameSynergyUI : CachedMonoBehaviour
    {
        /// <summary>
        /// 시너지 업데이트 중 상승한 시너지 타입 수집용
        /// </summary>
        private static HashSet<SynergyType> _upgradedSynergyTypes = new();

        /// <summary>
        /// 시너지 타입별 이전 상태 저장 (count, grade)
        /// UI 슬롯이 재정렬되어도 정확한 비교를 위해 전역적으로 관리
        /// </summary>
        private static Dictionary<SynergyType, (int count, int grade)> _previousSynergyStates = new();

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
        [SerializeField] private SpriteLoader _IconSpriteLoader;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TextMeshProUGUI _synergyNameText;
        [SerializeField] private GameObject _countMaxGameObject;
        [SerializeField] private GameObject _countParentGameObject;

        [SerializeField] private Image _gradeGuageImage;

        [SerializeField] private RectTransform _buttonRect;
        [SerializeField] private UIShiny _shiny;

        [SerializeField] private GameObject _stepGameObject;
        [SerializeField] private TextMeshProUGUI _stepText;
        [SerializeField] private GameObject _splitLine;

        [Space(10)] 
        [SerializeField] private SimpleImageSwapper _gradeImageSwapper;

        [SerializeField] private SimpleImageSwapper _iconMaskSwapper;
        [SerializeField] private SimpleImageColorSwapper _iconColorSwapper;

        private SynergyType _synergyType;
        private ISpecSynergyData _synergyData;
        private bool _isActive;
        private HashSet<int> _inBattleChampionIds;
        private const int MAX_GRADE = 3;

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

            // 등급 배경
            _gradeImageSwapper.Swap(grade >= 1 && grade <= MAX_GRADE
                ? (SimpleSwapType)(SimpleSwapType.Grade_0 + grade - 1)
                : SimpleSwapType.Disabled);

            bool showStep = !(grade == 1 && !isActive);
            // 아이콘
            _IconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteNameInGameBadge(synergyType)).Forget();
            _iconColorSwapper.Swap(showStep
                ? DistinguishSpecTypeHelper.ToSwapType(synergyType)
                : SimpleSwapType.Disabled);
            
            _iconMaskSwapper.Swap(DistinguishSpecTypeHelper.IsAsterismSynergyType(synergyType)
                ? SimpleSwapType.Constellation
                : SimpleSwapType.Elemental);

            // 등급 게이지
            _gradeGuageImage.fillAmount = showStep ? (isMaxGrade ? 1f : grade / (float)MAX_GRADE) : 0f;
            _stepGameObject.SetActive(showStep);
            if (showStep) _stepText.text = grade.ToString();

            // 카운트
            _countMaxGameObject.SetActive(isMaxGrade);
            _countText.gameObject.SetActive(!isMaxGrade);
            _countText.text = $"{count}/{nextData.min_int}";
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

        public void SetSplitLine(bool visible)
        {
            _splitLine?.SetActive(visible);
        }

    }
}