using System.Collections;
using System.Collections.Generic;
using System.Threading;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class SynergyTooltipInGamePopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimLayerButton;

        [Header("Synergy Info Layer")]
        [SerializeField] private InGameSynergyUI _synergyUI;

        [SerializeField] private TextMeshProUGUI _synergyNameText;
        [SerializeField] private TextMeshProUGUI _synergyNameTitleText;
        [SerializeField] private TextMeshProUGUI _synergyDescText;

        [Header("Vertical Layout Group")]
        [SerializeField] private List<TextMeshProUGUI> _synergyEffectList;

        private List<SpecSynergy> _synergyList = new List<SpecSynergy>();

        SpecSynergy _synergyData;
        SpecSynergy _nextSynergyData;
        private int _count;

        protected override void Awake()
        {
            base.Awake();

            _closeButton.onClick.AddListener(OnClickCloseButton);
            _dimLayerButton.onClick.AddListener(OnClickCloseButton);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _closeButton.onClick.RemoveListener(OnClickCloseButton);
            _dimLayerButton.onClick.RemoveListener(OnClickCloseButton);
        }

        protected override void OnPreEnter(object param)
        {
            base.OnPreEnter(param);
            //TopCurrencyAndMenuBar.AddToUILayer(this, TopPanelType.CloseButton);

            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_popup);

            (_synergyList, _count, _synergyData, _nextSynergyData) = ((List<SpecSynergy>, int, SpecSynergy, SpecSynergy))param;

            SetSynergyInfo();
        }

        private void SetSynergyInfo()
        {
            if (_synergyList == null || _synergyList.Count == 0) return;

            var baseSynergyData = _synergyList[0];

            // // 시너지 UI 설정
            // if (baseSynergyData.character_position_type == SynergyType.NONE)
            // {
            //     _synergyUI.SetSynergy(baseSynergyData.element_type, _count, _synergyData, _nextSynergyData, _synergyData.grade > 0);
            // }
            // else if (baseSynergyData.element_type == SynergyType.NONE)
            // {
            //     _synergyUI.SetPositionSynergy(baseSynergyData.character_position_type, _count, _synergyData, _nextSynergyData, _synergyData.grade > 0);
            // }

            _synergyUI.SetSynergy(baseSynergyData.synergy_type, _count, _synergyData, _nextSynergyData, _synergyData.grade > 0);

            // 시너지 이름 및 설명 설정
            string synergyName = LanguageManager.Instance.GetLanguageText(baseSynergyData.name_token);
            _synergyNameText.text = synergyName;
            _synergyNameTitleText.text = string.Format(LanguageManager.Instance.GetLanguageText("SYNERGY_PLACE_EFFECT"),
                synergyName);
            _synergyDescText.text = LanguageManager.Instance.GetLanguageText(baseSynergyData.desc_token_1);

            // 강제로 레이아웃 및 캔버스 업데이트
            Canvas.ForceUpdateCanvases();

            // 시너지 효과 리스트 설정
            for (int i = 0; i < _synergyEffectList.Count; i++)
            {
                bool isActive = _synergyList.Count > i;
                _synergyEffectList[i].gameObject.SetActive(isActive);

                if (!isActive) continue;

                string text = LanguageManager.Instance.GetLanguageText(_synergyList[i].desc_token_2);
                float statValue = _synergyList[i].skill_value_type == SkillValueType.PERCENT
                    ? _synergyList[i].stat_value * 100f
                    : _synergyList[i].stat_value;
                _synergyEffectList[i].text = string.Format(text, _synergyList[i].min_count, statValue);

                // 등급에 따라 글꼴 스타일 설정
                if (_synergyList[i].grade == _synergyData.grade)
                {
                    _synergyEffectList[i].fontStyle = FontStyles.Bold;
                }
                else
                {
                    _synergyEffectList[i].fontStyle = FontStyles.Normal;

                    // 텍스트 메쉬 강제 업데이트
                    _synergyEffectList[i].ForceMeshUpdate();

                    // 텍스트 컬러 투명도 조정
                    TMP_TextInfo textInfo = _synergyEffectList[i].textInfo;

                    for (int index = 0; index < textInfo.meshInfo.Length; index++)
                    {
                        Color32[] vertexColors = textInfo.meshInfo[index].colors32;

                        for (int j = 0; j < vertexColors.Length; j++)
                        {
                            vertexColors[j] = new Color32(vertexColors[j].r, vertexColors[j].g, vertexColors[j].b,
                                (byte)(0.5f * 255f));
                        }
                    }

                    // 텍스트 메쉬 데이터 업데이트 (컬러만)
                    _synergyEffectList[i].UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                }
            }
        }


        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
