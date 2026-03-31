using System.Collections.Generic;
using System.Linq;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 인게임 시너지 미니 팝업 - 시너지 아이콘 클릭 시 표시
    /// </summary>
    public class SynergyTooltipInGamePopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimLayerButton;

        [Header("Synergy Info")]
        [SerializeField] private SynergyInGameElement element;
        [SerializeField] private TextMeshProUGUI _synergyNameText;
        [SerializeField] private TextMeshProUGUI _synergyNameTitleText;
        [SerializeField] private TextMeshProUGUI _synergyDescText;

        [Header("Synergy Effect List (Optional)")]
        [SerializeField] private List<TextMeshProUGUI> _synergyEffectList;

        private SynergyType _synergyType;
        private ISpecSynergyData _synergyData;
        private ISpecSynergyData _nextSynergyData;
        private int _count;

        /// <summary>
        /// 팝업 파라미터 데이터
        /// </summary>
        public readonly struct PopupParam
        {
            public readonly SynergyType SynergyType;
            public readonly int Count;
            public readonly ISpecSynergyData SynergyData;
            public readonly ISpecSynergyData NextSynergyData;

            public PopupParam(SynergyType synergyType, int count, ISpecSynergyData synergyData, ISpecSynergyData nextSynergyData)
            {
                SynergyType = synergyType;
                Count = count;
                SynergyData = synergyData;
                NextSynergyData = nextSynergyData;
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

            if (param is PopupParam popupParam)
            {
                _synergyType = popupParam.SynergyType;
                _count = popupParam.Count;
                _synergyData = popupParam.SynergyData;
                _nextSynergyData = popupParam.NextSynergyData;
            }

            SetSynergyInfo();
        }

        private void SetSynergyInfo()
        {
            if (_synergyData == null) return;

            // 시너지 UI 아이콘 설정
            if (element != null)
            {
                element.SetSynergy(_synergyType, _count, _synergyData, _nextSynergyData, _synergyData.grade > 0);
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
            _synergyDescText.text = LanguageManager.Instance.GetDefaultText(_synergyData.desc_token_1);

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
