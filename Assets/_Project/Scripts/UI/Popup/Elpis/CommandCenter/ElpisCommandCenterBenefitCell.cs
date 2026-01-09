using Cysharp.Text;
using Naninovel;
using R3;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CookApps.AutoBattler
{
    /// <summary>
    /// 커멘드 센터 혜택 아이템 셀
    /// </summary>
    public class ElpisCommandCenterBenefitCell : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private Image iconImage;
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

        private void SetTitleText()
        {
            titleText.text = "임시값입니다..";
            //titleText.text = currentData.benefit_title_token; //TODO : localization
        }

        private void SetBuildingDescriptionText()
        {
            var buildInfo = SpecDataManager.Instance.GetBuildInfo(currentData.build_id);
            
            //var descriptionString = currentData.benefit_desc_token;  //TODO : localization
            var descriptionString = "임시. {0}를 새로 건설할 수 있습니다.";
            descriptionText.SetTextFormat(descriptionString, buildInfo.buld_name_token);
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
            descriptionText.text = currentData.benefit_desc_token; //TODO : localization
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
                iconImage.sprite = null;
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
