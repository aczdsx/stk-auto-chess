using System;
using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle.UIManagements;
using UnityEngine;

namespace CookApps.AutoBattler
{
    public class GachaPickUpCharacterLayer : GachaBaseLayer
    {
        [Space(10)]
        [SerializeField] private CAButton _gacha1Button;
        [SerializeField] private CAButton _gacha10Button;

        private void OnEnable()
        {
            _gacha1Button?.onClick.AddListener(OnClickGacha1Button);
            _gacha10Button?.onClick.AddListener(OnClickGacha10Button);
        }

        private void OnDisable()
        {
            _gacha1Button?.onClick.RemoveListener(OnClickGacha1Button);
            _gacha10Button?.onClick.RemoveListener(OnClickGacha10Button);
        }

        public void SetGachaLayer(GachaPopup parentPopup)
        {
            _parentGachaPopup = parentPopup;
            
            _specGachaDataOneTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_1_TIME_COUNT);
            _specGachaDataTenTime = SpecDataManager.Instance.GetGachaData(CurrentGachaType, Defines.GACHA_10_TIME_COUNT);
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