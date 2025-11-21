using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private List<Image> _stepImageList;


        private Color _step0Color = new Color32(139, 139, 139, 50); // 그레이 (Gray)
        private Color _step1Color = new Color32(205, 127, 50, 255); // 동 (Bronze)
        private Color _step2Color = new Color32(230, 230, 230, 255); // 은 (Silver)
        private Color _step3Color = new Color32(255, 215, 0, 255); // 금 (Gold)
        private Color _step4Color = new Color32(229, 228, 226, 255); // 플래티넘 (Platinum) 

        private SynergyType _synergyType;
        private int _count;
        private SpecSynergy _synergyData;
        private SpecSynergy _nextSynergyData;

        // 캐릭터 속성 시너지 세팅
        public void SetSynergy(SynergyType synergyType, int count, SpecSynergy data, SpecSynergy nextData, bool isActive = true)
        {
            _synergyType = synergyType;

            _synergyData = data;
            _nextSynergyData = nextData;

            Color color = Color.white;
            switch (data.grade)
            {
                case 0:
                    color = _step0Color;
                    break;
                case 1:
                    color = _step1Color;
                    break;
                case 2:
                    color = _step2Color;
                    break;
                case 3:
                    color = _step3Color;
                    break;
                case 4:
                    color = _step4Color;
                    break;
                default:
                    color = Color.white;
                    break;
            }

            _count = count;
            _iconImage.sprite = ImageManager.Instance.GetSynergySprite(synergyType, isActive);
            _iconImage.color = (data.grade == 0) ? color : Color.white;
            _countText.text = $"{count}/{nextData.min_count}";
            _countText.color = color;



            for (int i = 0; i < _stepImageList.Count; i++)
            {
                bool isActiveObject = i <= _synergyData.grade - 1;
                _stepImageList[i].gameObject.SetActive(isActiveObject);
                if (isActiveObject)
                {
                    _stepImageList[i].color = color;
                }
            }
        }

        public void OnClickSynergy()
        {
            var specSynergyDataList = SpecDataManager.Instance.GetSpecSynergyList(_synergyType);
            if (specSynergyDataList != null && specSynergyDataList.Count > 0)
            {
                var filteredSynergyDataList = specSynergyDataList.Where(l => l.grade != 0).ToList();
                SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipInGamePopup>((filteredSynergyDataList, _count, _synergyData, _nextSynergyData)).Forget();
            }
        }
    }
}
