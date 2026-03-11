using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyTooltipPopup : UILayerPopupBase
    {
        [SerializeField] private CAButton _elementalCloseButton;
        [SerializeField] private CAButton _starAsterismCloseButton;
        [SerializeField] private CAButton _dimLayerButton;

        [SerializeField] private GameObject _elementalInfoLayer;
        [SerializeField] private GameObject _starAsterismInfoLayer;

        [Header("Elemental Info Layer")]
        [SerializeField] private SynergyUI _elementalSynergyUI;
        [SerializeField] private TextMeshProUGUI _elementalSynergyNameText;
        [SerializeField] private TextMeshProUGUI _elementalSynergyNameTitleText;
        [SerializeField] private TextMeshProUGUI _elementalSynergyDescText;

        [Header("Elemental Vertical Layout Group")]
        [SerializeField] private List<TextMeshProUGUI> _elementalSynergyEffectList;

        [Header("Star Asterism Info Layer")]
        [SerializeField] private SynergyUI _starAsterismSynergyUI;
        [SerializeField] private TextMeshProUGUI _starAsterismSynergyNameText;

        [SerializeField] private TextMeshProUGUI _starAsterismSynergyNameTitleText;

        [Header("Star Asterism Vertical Layout Group")]
        [SerializeField] private List<TextMeshProUGUI> _starAsterismSynergyEffectList;


        protected override void Awake()
        {
            base.Awake();

            _elementalCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _starAsterismCloseButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
            _dimLayerButton.OnClickAsObservable().Subscribe(this, (_, self) => self.OnClickCloseButton()).AddTo(this);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);
            var synergyDataList = param as List<ISpecSynergyData>;
            if (DistinguishSpecTypeHelper.IsElementSynergyType(synergyDataList[0].synergy_type))
            {
                SetSynergyInfoElemental(synergyDataList);
            }
            else
            {
                SetSynergyInfoStarAsterism(synergyDataList);
            }
        }

        private void SetSynergyInfoElemental(List<ISpecSynergyData> synergyDataList)
        {
            if (synergyDataList == null || synergyDataList.Count == 0) return;

            _elementalInfoLayer.SetActive(true);
            _starAsterismInfoLayer.SetActive(false);
            var baseSynergyData = synergyDataList[0];

            if (baseSynergyData.synergy_type != SynergyType.NONE)
            {
                _elementalSynergyUI.SetSynergyUI(baseSynergyData.synergy_type);
            }

            string synergyName = LanguageManager.Instance.GetDefaultText(baseSynergyData.name_token);
            _elementalSynergyNameText.text = synergyName;
            _elementalSynergyNameTitleText.text = string.Format(LanguageManager.Instance.GetDefaultText("SYNERGY_PLACE_EFFECT"), synergyName);
            _elementalSynergyDescText.text = LanguageManager.Instance.GetDefaultText(baseSynergyData.desc_token_1);

            for (int i = 0; i < _elementalSynergyEffectList.Count; i++)
            {
                bool isActive = synergyDataList.Count > i;
                _elementalSynergyEffectList[i].gameObject.SetActive(isActive);

                if (!isActive) continue;
                string text = LanguageManager.Instance.GetDefaultText(synergyDataList[i].desc_token_2);
                _elementalSynergyEffectList[i].text = FormatSynergyEffectText(text, synergyDataList[i]);
            }
            RebuildEffectListLayout(_elementalSynergyEffectList);
        }

        private void SetSynergyInfoStarAsterism(List<ISpecSynergyData> synergyDataList)
        {
            if (synergyDataList == null || synergyDataList.Count == 0) return;

            _elementalInfoLayer.SetActive(false);
            _starAsterismInfoLayer.SetActive(true);
            var baseSynergyData = synergyDataList[0];

            if (baseSynergyData.synergy_type != SynergyType.NONE)
            {
                _starAsterismSynergyUI.SetSynergyUI(baseSynergyData.synergy_type);
            }

            string synergyName = LanguageManager.Instance.GetDefaultText(baseSynergyData.name_token);
            _starAsterismSynergyNameText.text = synergyName;
            _starAsterismSynergyNameTitleText.text = string.Format(LanguageManager.Instance.GetDefaultText("SYNERGY_PLACE_EFFECT"), synergyName);


            if (baseSynergyData.synergy_type == SynergyType.TROUBLESHOOTER)
            {
                SetSynergyInfoTroubleShooter(baseSynergyData, synergyDataList);
            }
            else
            {
                for (int i = 0; i < _starAsterismSynergyEffectList.Count; i++)
                {
                    bool isActive = synergyDataList.Count > i;
                    _starAsterismSynergyEffectList[i].gameObject.SetActive(isActive);

                    if (!isActive) continue;
                    string text = LanguageManager.Instance.GetDefaultText(synergyDataList[i].desc_token_1);
                    _starAsterismSynergyEffectList[i].text = FormatSynergyEffectText(text, synergyDataList[i]);
                }
            }
            RebuildEffectListLayout(_starAsterismSynergyEffectList);
        }

        /// <summary>
        /// 텍스트 설정 후 VerticalLayout이 올바른 높이로 재계산되도록 레이아웃을 강제 갱신합니다.
        /// (TextMeshPro preferred height가 반영되지 않아 2·3번째 항목만 붙어 보이는 현상 방지)
        /// </summary>
        private static void RebuildEffectListLayout(List<TextMeshProUGUI> effectList)
        {
            if (effectList == null || effectList.Count == 0) return;
            var parent = effectList[0].transform.parent;
            if (parent != null && parent is RectTransform rectTr)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTr);
        }

        /// <summary>
        /// 시너지 효과 텍스트에 플레이스홀더({0}, {1}, {2})가 있으면 data로 포맷하여 반환합니다.
        /// </summary>
        private static string FormatSynergyEffectText(string text, ISpecSynergyData data)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            if (text.Contains("{2}"))
                return string.Format(text, data.min_int, data.effect_stat_value_1, data.effect_stat_value_2);
            if (text.Contains("{1}"))
                return string.Format(text, data.min_int, data.effect_stat_value_1);
            if (text.Contains("{0}"))
                return string.Format(text, data.min_int);
            return text;
        }

        private void SetSynergyInfoTroubleShooter(ISpecSynergyData baseSynergyData, List<ISpecSynergyData> synergyDataList)
        {
            int descCnt = 1;
            int troubleshooterMaxGrade = 3;

            for (int i = 0; i < _starAsterismSynergyEffectList.Count; i++)
            {
                bool isActive = i < troubleshooterMaxGrade;
                _starAsterismSynergyEffectList[i].gameObject.SetActive(isActive);
            }
            for (int i = 0; i < synergyDataList.Count; i++)
            {
                if (synergyDataList[i].grade == descCnt)
                {
                    string text = LanguageManager.Instance.GetDefaultText(synergyDataList[i].desc_token_1);
                    _starAsterismSynergyEffectList[descCnt - 1].text = FormatSynergyEffectText(text, synergyDataList[i]);
                    descCnt++;
                }
            }
        }

        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
