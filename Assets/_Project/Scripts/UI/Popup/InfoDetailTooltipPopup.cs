using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class InfoDetailTooltipPopup : UILayer
    {
        [SerializeField] private CAButton _closeButton;
        [SerializeField] private CAButton _dimLayerButton;

        [Header("Stat Layer")]
        [SerializeField] private TextMeshProUGUI _battlePointText;
        [SerializeField] private TextMeshProUGUI _atkText;
        [SerializeField] private TextMeshProUGUI _atkSpdText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _criRateText;
        [SerializeField] private TextMeshProUGUI _criDamageText;
        [SerializeField] private TextMeshProUGUI _defText;
        [SerializeField] private TextMeshProUGUI _resText;
        [SerializeField] private TextMeshProUGUI _defPenText;
        [SerializeField] private TextMeshProUGUI _resPenText;

        private CharacterStatData _statData;

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

            _statData = param as CharacterStatData;

            SetStatInfo();
        }

        private void SetStatInfo()
        {
            if (_statData == null) return;

            _battlePointText.text = _statData.GetAttrValue().ToString("N0");
            _atkText.text = _statData.AD.ToString("N0");
            //_atkSpdText.text = ((float)_statData.AttackSpeed * 100).ToString("0.##");
            _atkSpdText.text = _statData.AttackSpeed.ToString("0.##");
            _hpText.text = _statData.HP.ToString("N0");
            _criRateText.text = $"{((float)_statData.CriticalProb * 100).ToString("0.##")}%";
            _criDamageText.text = $"{((float)_statData.CriticalDamageRate * 100).ToString("0.##")}%";
            _defText.text = _statData.ADReduce.ToString("N0");
            _resText.text = _statData.APReduce.ToString("N0");
            _defPenText.text = $"{((float)_statData.ADPierce  * 100).ToString("0.##")}%";
            _resPenText.text = $"{((float)_statData.APPierce * 100).ToString("0.##")}%";
        }

        private void OnClickCloseButton()
        {
            SoundManager.Instance.PlaySFX(SoundFX.snd_sfx_ui_btn_touch);

            SceneUILayerManager.Instance.PopUILayer(this);
        }
    }
}
