using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using CookApps.TeamBattle.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CookApps.TeamBattle.Utility;
using Cysharp.Threading.Tasks;
using R3;

namespace CookApps.AutoBattler
{
    public class ElpisBuildCell : CachedMonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private SpriteLoader iconSpriteLoader;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI conditionText;
        [SerializeField] private CAButton installButton;
        [SerializeField] private GameObject lockedOverlay;

        private ElpisBuildInfo buildInfo;
        private Action<ElpisBuildInfo> onInstallClicked;

        private void Awake()
        {
            // Bind Button
            installButton
                .OnClickAsObservable()
                .Subscribe(this, (_, self) => self.OnInstallClick())
                .AddTo(this);
        }

        public void SetData(LobbyBuildingInteractionUI.FacilityInfo facilityInfo, Action<ElpisBuildInfo> onInstallClicked)
        {
            buildInfo = facilityInfo.buildInfo;
            this.onInstallClicked = onInstallClicked;
            
            // Basic Info
            if (!string.IsNullOrEmpty(buildInfo.build_prefab))
            {
                // TODO: 실제 아이콘 경로에 맞게 수정 필요. 현재는 prefab 이름이나 ID를 사용한다고 가정.
                iconSpriteLoader.SetSprite(buildInfo.build_prefab).Forget(); 
            }

            nameText.text = buildInfo.buld_name_token;
            // _descText.text = LanguageManager.Instance.GetDefaultText(buildInfo.buld_desc_token);
            costText.text = buildInfo.item_INT.ToString();

            // State Handling
            lockedOverlay.SetActive(!facilityInfo.isCanInstall);

            if (facilityInfo.isInstalled)
            {
                installButton.gameObject.SetActive(false);
                conditionText.text = "설치 완료";
            }
            else if (facilityInfo.isJustCompleted)
            {
                installButton.gameObject.SetActive(false);
                conditionText.text = "설치 완료 대기";
            }
            else if (facilityInfo.isInstalling)
            {
                installButton.gameObject.SetActive(false);
                conditionText.text = "설치 중";
            }
            else if (facilityInfo.isPreviousLevelRequired)
            {
                installButton.gameObject.SetActive(false);
                conditionText.text = "이전 레벨 설치 필요";
            }
            else if (facilityInfo.isAnotherBuilding)
            {
                // 다른 건물이 건설 중인 경우 (사령부 레벨 부족보다 먼저 체크)
                installButton.gameObject.SetActive(false);
                conditionText.text = "다른 건물 건설 중";
            }
            else if (!facilityInfo.isCanInstall)
            {
                // 위 조건들이 모두 아닌데 설치 불가능하면 사령부 레벨 부족
                installButton.gameObject.SetActive(false);
                conditionText.text = "사령부 레벨 부족";
            }
            else
            {
                installButton.gameObject.SetActive(true);
                installButton.SetClickableState(facilityInfo.isCanInstall);
                conditionText.text = "";
            }
            
            CachedGo.SetActive(true);
        }

        private void OnInstallClick()
        {
            onInstallClicked?.Invoke(buildInfo);
        }
    }
}
