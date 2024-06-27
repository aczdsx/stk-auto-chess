using System.Collections;
using System.Collections.Generic;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    public class InGameSynergyUI : CachedMonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _countText;
        private ElementType _elementType;
        private CharacterPositionType _positionType;
        private bool _isElementType;
        private int _step;

        // 캐릭터 속성 시너지 세팅
        public void SetSynergy(ElementType type, int count, int step, bool isActive = true)
        {
            _elementType = type;
            _step = step;
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(type, isActive);
            _countText.text = count.ToString();

            _iconImage.color = (step > 0) ? Color.white : Color.gray;
            _countText.color = (step > 0) ? Color.white : Color.gray;
            _isElementType = true;
        }

        // 캐릭터 직업 속성 시너지 세팅
        public void SetPositionSynergy(CharacterPositionType type, int count, int step, bool isActive = true)
        {
            _positionType = type;
            _iconImage.sprite = ImageManager.Instance.GetPositionSprite(type, isActive);
            _countText.text = count.ToString();

            _iconImage.color = (step > 0) ? Color.white : Color.gray;
            _countText.color = (step > 0) ? Color.white : Color.gray;
            _isElementType = false;
        }

        public void OnClickSynergy()
        {
            if(_isElementType)
            {
                var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_elementType);
                if (specSynergyDataList != null && specSynergyDataList.Count > 0)
                {
                    SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipInGamePopup>((specSynergyDataList, _step)).Forget();
                }
            }
            else
            {
                var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_positionType);
                if (specSynergyDataList != null && specSynergyDataList.Count > 0)
                {
                    SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipInGamePopup>((specSynergyDataList, _step)).Forget();
                }
            }
        }
    }
}
