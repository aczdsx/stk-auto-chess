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
        [SerializeField] private SpriteLoader _starAsterismIconSpriteLoader;
        [SerializeField] private SpriteLoader _elementalIconSpriteLoader;
        [SerializeField] private GameObject _starAsterismIconGameObject;
        [SerializeField] private GameObject _elementalIconGameObject;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private GameObject _countMaxGameObject;

        [SerializeField] private Image _starAsterismGradeGuageImage;
        [SerializeField] private Image _starAsterismGradeGuageColor;
        [SerializeField] private Image _elementalGradeGuageImage;
        [SerializeField] private Image _elementalGradeGuageColor;
        [SerializeField] private RectTransform _buttonRect;



        private Color _step0Color = new Color32(139, 139, 139, 50); // 그레이 (Gray)
        private Color _step1Color = new Color32(205, 127, 50, 255); // 동 (Bronze)
        private Color _step2Color = new Color32(230, 230, 230, 255); // 은 (Silver)
        private Color _step3Color = new Color32(255, 215, 0, 255); // 금 (Gold)
        private Color _step4Color = new Color32(229, 228, 226, 255); // 플래티넘 (Platinum) 

        private SynergyType _synergyType;
        private int _count;
        private ISpecSynergyData _synergyData;
        private ISpecSynergyData _nextSynergyData;
        private const int MAX_GRADE = 3;

        //캐릭터 속성 시너지 세팅
        public void SetSynergy(SynergyType synergyType, int count, ISpecSynergyData data, ISpecSynergyData nextData, bool isActive = true, bool isColorWhite = false)
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


            bool isAsterismSynergyType = DistinguishSynergyTypeHelper.IsAsterismSynergyType(synergyType);
            _starAsterismIconGameObject.SetActive(isAsterismSynergyType);
            _elementalIconGameObject.SetActive(!isAsterismSynergyType);
            if (isAsterismSynergyType)
            {
                _starAsterismIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(synergyType, isActive)).Forget();
            }
            else
            {
                _elementalIconSpriteLoader.SetSprite(SpriteNameParser.GetSpriteName(synergyType, isActive)).Forget();
            }

            if (isAsterismSynergyType)
            {
                if (data.grade == 1 && !isActive)
                {
                    _starAsterismGradeGuageImage.fillAmount = 0f;
                }
                else if (data.grade == MAX_GRADE)
                {
                    _starAsterismGradeGuageImage.fillAmount = 1f;
                }
                else
                {
                    _starAsterismGradeGuageImage.fillAmount = (float)data.grade / (float)(MAX_GRADE);
                }
                _starAsterismGradeGuageColor.color = color;
            }
            else
            {
                if (data.grade == 1 && !isActive)
                {
                    _elementalGradeGuageImage.fillAmount = 0f;
                }
                else if (data.grade == MAX_GRADE)
                {
                    _elementalGradeGuageImage.fillAmount = 1f;
                }
                {
                    _elementalGradeGuageImage.fillAmount = (float)data.grade / (float)(MAX_GRADE);
                }
                _elementalGradeGuageColor.color = color;
            }

            if (data.grade == MAX_GRADE)
            {
                _countMaxGameObject.SetActive(true);
                _countText.gameObject.SetActive(false);
            }
            else
            {
                _countMaxGameObject.SetActive(false);
                _countText.gameObject.SetActive(true);
            }
            _countText.text = $"{count}/{nextData.min_int}";
            if(isAsterismSynergyType){
                Debug.LogColor($"SynergyUI!! [{synergyType}] {isActive}/{_starAsterismGradeGuageImage.fillAmount}", "green");
            }
            else
            {
                Debug.LogColor($"SynergyUI!! [{synergyType}] {isActive}/{_elementalGradeGuageImage.fillAmount}", "green");
            }
        }

        /// <summary>
        /// 시너지 아이콘 클릭 시 미니 팝업 표시
        /// </summary>
        public void OnClickSynergy()
        {
            if (_synergyData == null || _synergyData.grade == 0) return;

            var param = new SynergyTooltipIngameMiniPopup.PopupParam(
                _synergyType,
                _count,
                _synergyData,
                _nextSynergyData,
                _buttonRect
            );

            SceneUILayerManager.Instance.PushUILayerAsync<SynergyTooltipIngameMiniPopup>(param).Forget();
        }
    }
}
