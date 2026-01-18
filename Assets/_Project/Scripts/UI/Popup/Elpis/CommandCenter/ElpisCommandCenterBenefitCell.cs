using CookApps.TeamBattle;
using Cysharp.Text;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 커멘드 센터 혜택 아이템 셀
    /// </summary>
    public class ElpisCommandCenterBenefitCell : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private SpriteLoader iconImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI benefitCountText;
        [SerializeField] private Button shortcutButton;

        private ElpisCommandCenterBenefit currentData;
        private int currentIndex;

        private void Awake()
        {
            shortcutButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnClick())
                .AddTo(this);
        }

        /// <summary>
        /// 셀 데이터 설정
        /// </summary>
        public void SetData(ElpisCommandCenterBenefit data, int index)
        {
            currentData = data;
            currentIndex = index;

            UpdateUI();
        }

        #region Set UI

        /// <summary>
        /// UI 업데이트
        /// </summary>
        private void UpdateUI()
        {
            if (currentData == null)
                return;
            
            SetBenefitCountActive(false);
            SetTitleText();
            SetIconImage();
            
            switch (currentData.benefit_type)
            {
                case BenefitType.BUILDING:
                    SetUIToBuildingBenefit();
                    break;
                case BenefitType.MULTI_BUILDING:
                    SetUIToMultiBuildingBenefit();
                    break;
                case BenefitType.MAX_LEVEL_UP:
                    SetUIToMaxLevelUpBenefit();
                    break;
                case BenefitType.AREA_EXPAND:
                    SetUIToAreaExpandBenefit();
                    break;
            }
        }

        private void SetIconImage()
        {
            var targetBuildInfo = SpecDataManager.Instance.GetBuildInfo(currentData.build_id);
            
            if (iconImage == null)
                return;

            if (targetBuildInfo == null)
            {
                iconImage.SetSprite("Building_Active").Forget();
            }
            else
            {
                iconImage.SetSprite(targetBuildInfo.sprite_name).Forget();
            }

            iconImage.gameObject.SetActive(true);
        }

        private void SetTitleText()
        {
            titleText.text = LanguageManager.Instance.GetDefaultText(currentData.benefit_title_token);
        }

        private void SetBuildingDescriptionText()
        {
            var buildInfo = SpecDataManager.Instance.GetBuildInfo(currentData.build_id);

            var descriptionString = LanguageManager.Instance.GetDefaultText(currentData.benefit_desc_token);
            descriptionText.SetTextFormat(descriptionString,LanguageManager.Instance.GetDefaultText(buildInfo.buld_name_token));
        }

        private void SetBenefitCountText()
        {
            if(!benefitCountText)
                return;
            
            benefitCountText.SetTextFormat("{0} > {1}", currentData.before_key, currentData.benefit_key);
            SetBenefitCountActive(true);
        }

        private void SetUIToBuildingBenefit()
        {
            SetBuildingDescriptionText();
        }

        private void SetUIToMultiBuildingBenefit()
        {
            SetBuildingDescriptionText(); 
            SetBenefitCountText();
        }

        private void SetUIToMaxLevelUpBenefit()
        {
            if (currentData.build_id == 0) //보상이 빌딩 맥스레벨이 아닐 때 처리하면 될듯?
                return;

            SetBuildingDescriptionText();
            SetBenefitCountText();
        }

        private void SetUIToAreaExpandBenefit()
        {
            descriptionText.text = LanguageManager.Instance.GetDefaultText(currentData.benefit_desc_token);
        }

        private void SetBenefitCountActive(bool active)
        {
            benefitCountText?.gameObject.SetActive(active);
        }

        #endregion

        private System.Action callback;
        /// <summary>
        /// 바로가기 버튼 콜백 설정
        /// </summary>
        public void SetShortcutCallback(System.Action callback)
        {
            this.callback = callback;
        }

        /// <summary>
        /// 셀 상태 초기화
        /// </summary>
        public void ResetState()
        {
            currentData = null;
            currentIndex = -1;

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }

            if (titleText != null)
            {
                titleText.text = string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text = string.Empty;
            }
        }

        private void OnClick()
        {
            callback?.Invoke();
        }
    }
}
