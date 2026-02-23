using System.Collections.Generic;
using CookApps.BattleSystem;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 인게임 시너지 미니 팝업 - 시너지 아이콘 클릭 시 표시되는 간단한 정보 팝업
    /// </summary>
    public class SynergyTooltipIngameMiniPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimLayerButton;
        [SerializeField] private RectTransform _body;

        [Header("Synergy Info")]
        [SerializeField] private SynergyUI _synergyUI;
        [SerializeField] private TextMeshProUGUI _synergyNameText;
        [SerializeField] private List<SynergyTooltipGradeSlot> _gradeSlots;

        [Header("Character Icons")]
        [SerializeField] private SynergyTooltipImageGroup _imageGroup;

        private SynergyType _synergyType;
        private ISpecSynergyData _synergyData;
        private bool _isActive;

        private readonly List<SynergyTooltipImageGroup.CharacterSlotData> _reusableSlotDataList = new();
        private readonly HashSet<int> _reusableInBattleIds = new();

        /// <summary>
        /// 팝업 파라미터 데이터
        /// </summary>
        public readonly struct PopupParam
        {
            public readonly SynergyType SynergyType;
            public readonly ISpecSynergyData SynergyData;
            public readonly RectTransform ButtonRect;
            public readonly bool IsActive;

            public PopupParam(SynergyType synergyType, ISpecSynergyData synergyData, RectTransform buttonRect, bool isActive)
            {
                SynergyType = synergyType;
                SynergyData = synergyData;
                ButtonRect = buttonRect;
                IsActive = isActive;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _closeButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimLayerButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            RectTransform buttonRect = null;

            if (param is PopupParam popupParam)
            {
                _synergyType = popupParam.SynergyType;
                _synergyData = popupParam.SynergyData;
                buttonRect = popupParam.ButtonRect;
                _isActive = popupParam.IsActive;
            }

            SetSynergyInfo();
            SetBodyPosition(buttonRect);
        }

        private void SetBodyPosition(RectTransform buttonRect)
        {
            if (_body == null || buttonRect == null) return;

            Vector3[] corners = new Vector3[4];
            buttonRect.GetWorldCorners(corners);
            Vector3 buttonCenter = (corners[0] + corners[2]) / 2f;

            var bodyParent = _body.parent as RectTransform;
            if (bodyParent == null) return;

            Vector3 localPos = bodyParent.InverseTransformPoint(buttonCenter);
            float targetY = localPos.y;

            float parentHeight = bodyParent.rect.height;
            float bodyHeight = _body.rect.height;
            float pivotY = _body.pivot.y;

            float topOffset = bodyHeight * (1f - pivotY);
            float bottomOffset = bodyHeight * pivotY;

            float maxY = (parentHeight / 2f) - topOffset;
            float minY = -(parentHeight / 2f) + bottomOffset;

            float clampedY = Mathf.Clamp(targetY, minY, maxY);

            var anchoredPos = _body.anchoredPosition;
            anchoredPos.y = clampedY;
            _body.anchoredPosition = anchoredPos;
        }

        private void SetSynergyInfo()
        {
            if (_synergyData == null) return;

            if (_synergyUI != null)
            {
                _synergyUI.SetSynergyUI(_synergyType, _isActive);
            }

            string synergyName = LanguageManager.Instance.GetDefaultText(_synergyData.name_token);
            _synergyNameText.text = $"{synergyName} {_synergyData.grade}단계";

            SetGradeSlots();
            SetImageSlots();
        }

        /// <summary>
        /// 전 단계 효과를 슬롯에 표시하고 현재 단계를 하이라이트합니다.
        /// 속성/성군 구분 없이 동일 로직으로 동작합니다.
        /// </summary>
        private void SetGradeSlots()
        {
            if (_gradeSlots == null || _gradeSlots.Count == 0) return;

            var synergyList = SpecDataManager.Instance.GetSpecSynergyList(_synergyType);
            if (synergyList == null || synergyList.Count == 0) return;

            int nextGrade = 1;
            int slotIndex = 0;

            for (int i = 0; i < synergyList.Count && slotIndex < _gradeSlots.Count; i++)
            {
                var data = synergyList[i];
                if (data.grade <= 0 || data.grade != nextGrade) continue;

                // desc_token_2 우선, 비어있으면 desc_token_1 사용
                string token = !string.IsNullOrEmpty(data.desc_token_2)
                    ? data.desc_token_2
                    : data.desc_token_1;
                string text = LanguageManager.Instance.GetDefaultText(token);
                string formatted = FormatGradeText(text, data);
                bool isHighlighted = data.grade == _synergyData.grade;

                _gradeSlots[slotIndex].SetGrade(formatted, isHighlighted);
                _gradeSlots[slotIndex].SetActive(true);

                slotIndex++;
                nextGrade++;
            }

            for (int i = slotIndex; i < _gradeSlots.Count; i++)
            {
                _gradeSlots[i].SetActive(false);
            }

            // stretch 앵커의 rect가 유효하도록 레이아웃 강제 갱신
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < slotIndex; i++)
            {
                _gradeSlots[i].AdjustHeight();
            }

            // 변경된 슬롯 높이를 상위 레이아웃까지 반영
            LayoutRebuilder.ForceRebuildLayoutImmediate(_body);
        }

        /// <summary>
        /// 해당 시너지에 속하는 캐릭터 아이콘을 ImageGroup에 표시한다.
        /// 배치된 캐릭터가 앞에, 등급 내림차순으로 정렬된다.
        /// </summary>
        private void SetImageSlots()
        {
            if (_imageGroup == null) return;

            // 필드 위 플레이어 캐릭터 ID 수집
            var battlers = InGameObjectManager.Instance.GetCharacterList(AllianceType.Player);
            _reusableInBattleIds.Clear();
            for (int i = 0; i < battlers.Count; i++)
            {
                _reusableInBattleIds.Add(battlers[i].CharacterId);
            }

            var characters = SpecDataManager.Instance.GetCharacterListBySynergyType(_synergyType);
            _reusableSlotDataList.Clear();

            for (int i = 0; i < characters.Count; i++)
            {
                var charInfo = characters[i];
                _reusableSlotDataList.Add(new SynergyTooltipImageGroup.CharacterSlotData
                {
                    PrefabId = charInfo.prefab_id,
                    Grade = charInfo.grade_type,
                    InBattle = _reusableInBattleIds.Contains(charInfo.id)
                });
            }

            _imageGroup.SetCharacters(_reusableSlotDataList);
        }

        private static string FormatGradeText(string text, ISpecSynergyData data)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.Contains("{2}"))
                return string.Format(text, data.min_int, data.effect_stat_value_1, data.effect_stat_value_2);
            if (text.Contains("{1}"))
                return string.Format(text, data.min_int, data.effect_stat_value_1);
            if (text.Contains("{0}"))
                return string.Format(text, data.min_int);
            return text;
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
