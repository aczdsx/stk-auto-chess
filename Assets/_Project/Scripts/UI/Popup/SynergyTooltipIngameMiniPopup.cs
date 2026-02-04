using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;

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
        [SerializeField] private TextMeshProUGUI _synergyDescText;

        [Header("(Optional)")]
        [SerializeField] private TextMeshProUGUI _synergyNameTitleText;
        [SerializeField] private List<TextMeshProUGUI> _synergyEffectList;

        private SynergyType _synergyType;
        private ISpecSynergyData _synergyData;
        private ISpecSynergyData _nextSynergyData;
        private int _count;
        private bool _isActive;

        /// <summary>
        /// 팝업 파라미터 데이터
        /// </summary>
        public readonly struct PopupParam
        {
            public readonly SynergyType SynergyType;
            public readonly int Count;
            public readonly ISpecSynergyData SynergyData;
            public readonly ISpecSynergyData NextSynergyData;
            public readonly RectTransform ButtonRect;
            public readonly bool IsActive;

            public PopupParam(SynergyType synergyType, int count, ISpecSynergyData synergyData, ISpecSynergyData nextSynergyData, RectTransform buttonRect, bool isActive)
            {
                SynergyType = synergyType;
                Count = count;
                SynergyData = synergyData;
                NextSynergyData = nextSynergyData;
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
                _count = popupParam.Count;
                _synergyData = popupParam.SynergyData;
                _nextSynergyData = popupParam.NextSynergyData;
                buttonRect = popupParam.ButtonRect;
                _isActive = popupParam.IsActive;
            }

            SetSynergyInfo();
            SetBodyPosition(buttonRect);
        }

        private void SetBodyPosition(RectTransform buttonRect)
        {
            if (_body == null || buttonRect == null) return;

            // 버튼의 월드 좌표 중심점
            Vector3[] corners = new Vector3[4];
            buttonRect.GetWorldCorners(corners);
            Vector3 buttonCenter = (corners[0] + corners[2]) / 2f;

            // body의 부모 기준 로컬 좌표로 변환
            var bodyParent = _body.parent as RectTransform;
            if (bodyParent == null) return;

            Vector3 localPos = bodyParent.InverseTransformPoint(buttonCenter);
            float targetY = localPos.y;

            // body 높이 (pivot 고려)
            float parentHeight = bodyParent.rect.height;
            float bodyHeight = _body.rect.height;
            float pivotY = _body.pivot.y;

            // body가 화면 밖으로 나가지 않도록 클램핑
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
            
            // 시너지 UI 아이콘 설정
            if (_synergyUI != null)
            {
                _synergyUI.SetSynergyUI(_synergyType, _isActive);
            }

            // 시너지 이름 설정 (예: "바람 속성 2단계")
            string synergyName = LanguageManager.Instance.GetDefaultText(_synergyData.name_token);
            int grade = _synergyData.grade;
            _synergyNameText.text = $"{synergyName} {grade}단계";

            // 시너지 타이틀 설정 (옵션)
            if (_synergyNameTitleText != null)
            {
                _synergyNameTitleText.text = string.Format(
                    LanguageManager.Instance.GetDefaultText("SYNERGY_PLACE_EFFECT"),
                    synergyName);
            }

            // 시너지 설명 설정
            string text = LanguageManager.Instance.GetDefaultText(_synergyData.desc_token_1);
            if (DistinguishSynergyTypeHelper.IsElementSynergyType(_synergyType))
            {
                // 텍스트에 플레이스홀더가 있는 경우 Format 사용
                if (!string.IsNullOrEmpty(text))
                {
                    // 플레이스홀더 개수에 따라 다른 값 전달
                    if (text.Contains("{2}"))
                    {
                        // {0}, {1}, {2} 모두 필요한 경우
                        _synergyDescText.text = string.Format(text, _synergyData.effect_stat_value_1, _synergyData.effect_stat_value_2, _synergyData.effect_stat_value_3);
                    }
                    else if (text.Contains("{1}"))
                    {
                        // {0}, {1}만 필요한 경우
                        _synergyDescText.text = string.Format(text, _synergyData.effect_stat_value_1, _synergyData.effect_stat_value_2);
                    }
                    else if (text.Contains("{0}"))
                    {
                        // {0}만 필요한 경우
                        _synergyDescText.text = string.Format(text, _synergyData.effect_stat_value_1);
                    }
                    else
                    {
                        // 플레이스홀더가 없는 경우
                        _synergyDescText.text = text;
                    }
                }
                else
                {
                    _synergyDescText.text = text;
                }
            }
            else
            {// 성군 시너지 작성이라면?
                if (!string.IsNullOrEmpty(text))
                {
                    // "{0}명:" 부분 제거 후 플레이스홀더 인덱스 재정렬
                    text = System.Text.RegularExpressions.Regex.Replace(text, @"\{0\}명[:\s]*", "");
                    text = text.Replace("{1}", "{0}").Replace("{2}", "{1}").Replace("{3}", "{2}");

                    // 플레이스홀더 개수에 따라 다른 값 전달
                    if (text.Contains("{2}"))
                    {
                        _synergyDescText.text = string.Format(text, _synergyData.effect_stat_value_1, _synergyData.effect_stat_value_2, _synergyData.effect_stat_value_3);
                    }
                    else if (text.Contains("{1}"))
                    {
                        _synergyDescText.text = string.Format(text, _synergyData.effect_stat_value_1, _synergyData.effect_stat_value_2);
                    }
                    else if (text.Contains("{0}"))
                    {
                        _synergyDescText.text = string.Format(text, _synergyData.effect_stat_value_1);
                    }
                    else
                    {
                        _synergyDescText.text = text;
                    }
                }
                else
                {
                    _synergyDescText.text = text;
                }
            }
            

            // 강제로 레이아웃 및 캔버스 업데이트
            Canvas.ForceUpdateCanvases();

            // 시너지 효과 리스트 설정 (상세 팝업용, 옵션)
            SetSynergyEffectList();
        }

        private void SetSynergyEffectList()
        {
            if (_synergyEffectList == null || _synergyEffectList.Count == 0) return;

            var synergyList = SpecDataManager.Instance.GetSpecSynergyList(_synergyType);
            if (synergyList == null || synergyList.Count == 0) return;

            // grade > 0 인 시너지만 필터링
            var filteredList = synergyList.Where(s => s.grade > 0).ToList();

            for (int i = 0; i < _synergyEffectList.Count; i++)
            {
                if (_synergyEffectList[i] == null) continue;

                bool isActive = filteredList.Count > i;
                _synergyEffectList[i].gameObject.SetActive(isActive);

                if (!isActive) continue;

                var data = filteredList[i];
                string text = LanguageManager.Instance.GetDefaultText(data.desc_token_2);
                _synergyEffectList[i].text = string.Format(text, data.min_int);

                // 현재 등급 강조
                _synergyEffectList[i].fontStyle = data.grade == _synergyData.grade
                    ? FontStyles.Bold
                    : FontStyles.Normal;
            }
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }

        
        
    }
}
