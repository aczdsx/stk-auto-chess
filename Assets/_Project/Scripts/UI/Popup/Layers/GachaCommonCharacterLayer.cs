using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class GachaCommonCharacterLayer : GachaBaseLayer
    {
        [Header("Button")]
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private Image _gacha1ButtonCostImage;
        [SerializeField] private TextMeshProUGUI _gacha1ButtonCostText;
        
        [Space(10)]
        [SerializeField] private CAButton _gacha10Button;
        [SerializeField] private Image _gacha10ButtonCostImage;
        [SerializeField] private TextMeshProUGUI _gacha10ButtonCostText;

        private void OnEnable()
        {
            _gacha1Button.onClick.AddListener(OnClickGacha1Button);
            _gacha10Button.onClick.AddListener(OnClickGacha10Button);
        }

        private void OnDisable()
        {
            _gacha1Button.onClick.RemoveListener(OnClickGacha1Button);
            _gacha10Button.onClick.RemoveListener(OnClickGacha10Button);
        }

        public void SetGachaLayer(GachaPopup parentPopup)
        {
            _parentGachaPopup = parentPopup;
            
            _specGachaDataOneTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_1_TIME_COUNT);
            _specGachaDataTenTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_10_TIME_COUNT);

            _gacha1ButtonCostImage.sprite = ImageManager.Instance.GetItemSprite(_specGachaDataOneTime.gacha_cost_item_type);
            _gacha1ButtonCostText.text = $"x{_specGachaDataOneTime.gacha_cost}";
            
            _gacha10ButtonCostImage.sprite = ImageManager.Instance.GetItemSprite(_specGachaDataTenTime.gacha_cost_item_type);
            _gacha10ButtonCostText.text = $"x{_specGachaDataTenTime.gacha_cost}";
        }
        
        private void OnClickGacha1Button()
        {
            if (_specGachaDataOneTime == null) return;
            
            ProcessCharacterGacha(GachaCountType.ONE);
        }

        private void OnClickGacha10Button()
        {
            if (_specGachaDataTenTime == null) return;
            
            ProcessCharacterGacha(GachaCountType.TEN);
        }
    }
}