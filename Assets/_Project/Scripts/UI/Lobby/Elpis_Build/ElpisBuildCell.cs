using System;
using CookApps.TeamBattle;
using CookApps.TeamBattle.UIManagements;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

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
        [SerializeField] private TextMeshProUGUI upgradeButtonText;

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

            nameText.text = LanguageManager.Instance.GetDefaultText(facilityInfo.buildInfo.buld_name_token);
            var buttonTextKey = facilityInfo.isInstalled ? "ELPIS_UPGRADE" : "ELPIS_BUILD";
            upgradeButtonText.text = LanguageManager.Instance.GetDefaultText(buttonTextKey);

            iconSpriteLoader.SetSprite(buildInfo.sprite_name).Forget();

            costText.text = buildInfo.item_INT.ToString();

            lockedOverlay.SetActive(!facilityInfo.isCanInstall);

            var stateKey = "";

            if (facilityInfo.isInstalled)
            {
                installButton.gameObject.SetActive(false);
                stateKey = "ELPIS_INSTALLED";
            }
            else if (facilityInfo.isInstalling)
            {
                installButton.gameObject.SetActive(false);
                stateKey = "ELPIS_INSTALLING";
            }
            else if (facilityInfo.isPreviousLevelRequired)
            {
                installButton.gameObject.SetActive(false);
                stateKey = "ELPIS_UPGRADE_PREVIOUS_REQUIRED";
            }
            else if (facilityInfo.isAnotherBuilding)
            {
                // 다른 건물이 건설 중인 경우 (사령부 레벨 부족보다 먼저 체크)
                installButton.gameObject.SetActive(false);
                stateKey = "ELPIS_ANOTHER_BUILDING";
            }
            else if (!facilityInfo.isCanInstall)
            {
                // 위 조건들이 모두 아닌데 설치 불가능하면 사령부 레벨 부족
                installButton.gameObject.SetActive(false);
                stateKey = "ELPIS_LEVEL_NOT_ENOUGH";
            }
            else
            {
                installButton.gameObject.SetActive(true);
                installButton.SetClickableState(facilityInfo.isCanInstall);
                conditionText.text = "";
            }

            conditionText.text = LanguageManager.Instance.GetDefaultText(stateKey);

            CachedGo.SetActive(true);
        }

        private void OnInstallClick()
        {
            onInstallClicked?.Invoke(buildInfo);
        }
    }
}
