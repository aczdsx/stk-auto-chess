using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using R3;
using TMPro;
using UnityEngine;

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
            if(DistinguishSynergyTypeHelper.IsElementSynergyType(synergyDataList[0].synergy_type))
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
            
                // 텍스트에 플레이스홀더가 있는 경우 Format 사용
                if (!string.IsNullOrEmpty(text))
                {
                    var data = synergyDataList[i];
                    // 플레이스홀더 개수에 따라 다른 값 전달
                    if (text.Contains("{2}"))
                    {
                        // {0}, {1}, {2} 모두 필요한 경우
                        _elementalSynergyEffectList[i].text = string.Format(text, data.min_int, data.effect_stat_value_1, data.effect_stat_value_2);
                    }
                    else if (text.Contains("{1}"))
                    {
                        // {0}, {1}만 필요한 경우
                        _elementalSynergyEffectList[i].text = string.Format(text, data.min_int, data.effect_stat_value_1);
                    }
                    else if (text.Contains("{0}"))
                    {
                        // {0}만 필요한 경우
                        _elementalSynergyEffectList[i].text = string.Format(text, data.min_int);
                    }
                    else
                    {
                        // 플레이스홀더가 없는 경우
                        _elementalSynergyEffectList[i].text = text;
                    }
                }
                else
                {
                    _elementalSynergyEffectList[i].text = text;
                }
            }
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
            
            for (int i = 0; i < _starAsterismSynergyEffectList.Count; i++)
            {
                bool isActive = synergyDataList.Count > i;
                _starAsterismSynergyEffectList[i].gameObject.SetActive(isActive);
            
                if (!isActive) continue;
                string text = LanguageManager.Instance.GetDefaultText(synergyDataList[i].desc_token_1);
            
                // 텍스트에 플레이스홀더가 있는 경우 Format 사용
                if (!string.IsNullOrEmpty(text))
                {
                    var data = synergyDataList[i];
                    // 플레이스홀더 개수에 따라 다른 값 전달
                    if (text.Contains("{2}"))
                    {
                        // {0}, {1}, {2} 모두 필요한 경우
                        _starAsterismSynergyEffectList[i].text = string.Format(text, data.min_int, data.effect_stat_value_1, data.effect_stat_value_2);
                    }
                    else if (text.Contains("{1}"))
                    {
                        // {0}, {1}만 필요한 경우
                        _starAsterismSynergyEffectList[i].text = string.Format(text, data.min_int, data.effect_stat_value_1);
                    }
                    else if (text.Contains("{0}"))
                    {
                        // {0}만 필요한 경우
                        _starAsterismSynergyEffectList[i].text = string.Format(text, data.min_int);
                    }
                    else
                    {
                        // 플레이스홀더가 없는 경우
                        _starAsterismSynergyEffectList[i].text = text;
                    }
                }
                else
                {
                    _starAsterismSynergyEffectList[i].text = text;
                }
            }
        }
        private void OnClickCloseButton()
        {
            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
